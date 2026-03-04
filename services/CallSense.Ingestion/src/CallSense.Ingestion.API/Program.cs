using CallSense.Ingestion.Application;
using CallSense.Ingestion.Application.Commands.UploadCall;
using CallSense.Ingestion.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RabbitMQ.Client;

// ── Builder Phase ────────────────────────────────────────────────────────────
// WebApplication.CreateBuilder sets up the host, logging, and configuration
// (reading appsettings.json, appsettings.Development.json, env vars, etc.)
// Nothing runs yet — we're just configuring services.
var builder = WebApplication.CreateBuilder(args);

// AddApplicationServices() is our extension method in Application/DependencyInjection.cs.
// It registers: MediatR (finds all IRequestHandler<,> in that assembly)
//               FluentValidation (finds all AbstractValidator<T> in that assembly)
builder.Services.AddApplicationServices();

// AddInfrastructureServices() is our extension method in Infrastructure/DependencyInjection.cs.
// It registers: EF Core + IngestionDbContext (Scoped, connects to Postgres)
//               ICallRecordRepository → CallRecordRepository (Scoped)
//               BlobServiceClient (Singleton, connects to Azure Blob / Azurite)
//               IBlobStorageService → AzureBlobStorageService (Scoped)
//               MassTransit + IBus (connects to RabbitMQ)
//               OutboxPublisherWorker (BackgroundService, polls outbox every 5s)
builder.Services.AddInfrastructureServices(builder.Configuration);

// OpenAPI / Swagger — auto-generates the /openapi/v1.json spec in development.
// Use Scalar, Swagger UI, or any OpenAPI client to explore the API.
builder.Services.AddOpenApi();

// Health checks — used by Kubernetes liveness/readiness probes and docker-compose
// healthcheck blocks. Two checks:
//   "postgres" — verifies the DB connection string is reachable (readiness)
//   "rabbitmq" — verifies the RabbitMQ AMQP port is reachable (readiness)
// Both are tagged "ready" so we can split liveness (just "alive?") from
// readiness ("ready to serve traffic?") on separate endpoints below.
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("Postgres")!,
        name: "postgres",
        tags: ["ready"])
    .AddRabbitMQ(
        async _ =>
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(builder.Configuration.GetConnectionString("RabbitMQ")!)
            };
            return await factory.CreateConnectionAsync();
        },
        name: "rabbitmq",
        tags: ["ready"]);

// ── App Phase ────────────────────────────────────────────────────────────────
// builder.Build() composes everything registered above into a runnable app.
// From this point on you can't add services — only configure the HTTP pipeline.
var app = builder.Build();

// OpenAPI spec endpoint — only in development so we don't expose API internals
// in production. Access at: GET /openapi/v1.json
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Redirect HTTP → HTTPS. In dev this is often disabled via launchSettings.json.
app.UseHttpsRedirection();

// ── Health Check Endpoints ───────────────────────────────────────────────────
// /healthz — liveness probe. Kubernetes calls this to decide if the pod should
// be restarted. We only check that the process is alive, not dependencies.
// Predicate = false means no health checks run — always returns Healthy.
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => false  // liveness = "process is alive", no dependency checks
});

// /readyz — readiness probe. Kubernetes calls this to decide if the pod should
// receive traffic. We check Postgres + RabbitMQ (tagged "ready").
// If either is down, this returns Unhealthy and Kubernetes stops routing traffic here.
app.MapHealthChecks("/readyz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// ── API Endpoints ────────────────────────────────────────────────────────────
// Minimal API — no controllers, no [ApiController] attribute ceremony.
// The entire endpoint is defined here: route, handler, response types.
//
// POST /api/calls/upload
// Accepts: multipart/form-data with a "file" field
// Headers: X-Tenant-Id: <guid>   (Phase 1 — replaced by JWT claim in Phase 2)
// Returns: 201 Created { callId: "..." }  |  400 Bad Request { error: "..." }
app.MapPost("/api/calls/upload", async (
    IFormFile file,             // ASP.NET binds the uploaded file from multipart form data
    HttpContext httpContext,     // Gives us access to request headers
    IMediator mediator,         // MediatR — routes the command to UploadCallCommandHandler
    CancellationToken cancellationToken) =>
{
    // Parse TenantId from the X-Tenant-Id header.
    // Phase 1: plain header. Phase 2: extract from JWT sub/tenant claim.
    // Using a header keeps the endpoint testable with curl/Postman without auth setup.
    if (!httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader)
        || !Guid.TryParse(tenantIdHeader, out var tenantId))
    {
        return Results.BadRequest(new { error = "X-Tenant-Id header is required and must be a valid Guid." });
    }

    // Open a Stream to the uploaded file without loading it all into memory.
    // The stream is passed directly to blob storage upload, which streams it
    // in chunks. This is why UploadCallCommand takes Stream, not byte[].
    await using var stream = file.OpenReadStream();

    var command = new UploadCallCommand(
        TenantId: tenantId,
        FileName: file.FileName,
        FileContent: stream,
        ContentType: file.ContentType,
        FileSizeBytes: file.Length);

    // mediator.Send() resolves UploadCallCommandHandler from DI, runs the
    // FluentValidation pipeline behaviour first, then calls Handle().
    // Returns Result<Guid> — never throws for business failures.
    var result = await mediator.Send(command, cancellationToken);

    // If the Result is a failure (invalid file type, size exceeded, blob upload
    // error), return 400 with the error message.
    // If success, return 201 Created with the new CallId in the body and
    // a Location header pointing to the resource (standard REST).
    return result.IsSuccess
        ? Results.Created($"/api/calls/{result.Value}", new { callId = result.Value })
        : Results.BadRequest(new { error = result.Error });
})
.WithName("UploadCall")
// DisableAntiforgery() is required for IFormFile in minimal APIs when
// antiforgery middleware is not set up (we don't have server-side forms here).
// The API is called by other services and CLIs, not browsers with form tokens.
.DisableAntiforgery();

// Start the application — blocks until the process is stopped.
// At this point ASP.NET Core:
//   1. Binds to the port in launchSettings.json / ASPNETCORE_URLS
//   2. Starts the OutboxPublisherWorker background service
//   3. Connects MassTransit to RabbitMQ
//   4. Begins accepting HTTP requests
app.Run();

// This partial class declaration makes Program visible to WebApplicationFactory<Program>
// in integration tests. Without it, the test project can't reference Program as
// the type parameter.
public partial class Program { }
