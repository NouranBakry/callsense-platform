using CallSense.SharedKernel;
using MediatR;

namespace CallSense.Ingestion.Application.Commands.UploadCall;

// IRequest<Result<Guid>> tells MediatR two things:
//   1. This is a command (something that changes state)
//   2. When handled, it returns Result<Guid> — the new CallId on success, or an error
public record UploadCallCommand(
    Guid TenantId,
    string FileName,
    Stream FileContent,
    string ContentType,
    long FileSizeBytes
) : IRequest<Result<Guid>>;
