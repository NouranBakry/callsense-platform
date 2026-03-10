# CallSense — Architecture Decisions

## Services

### CallSense.Intake (port 5092)
Receives audio uploads, transcribes them inline via Groq Whisper, stores the file in Azure Blob Storage, and persists the call record with transcript to Postgres. Uses the Transactional Outbox Pattern to reliably publish `CallTranscribed` events to RabbitMQ.

**Flow:**
1. `POST /api/calls/upload` — validate → transcribe → blob upload → DB save + outbox
2. `OutboxPublisherWorker` (background, every 5s) → reads unprocessed outbox messages → publishes to RabbitMQ

### CallSense.Analysis (port 5093)
Consumes `CallTranscribed` events from RabbitMQ via MassTransit. Sends the transcript to Groq's LLaMA 3.3 70B model for quality scoring. Stores the analysis result and exposes it via `GET /api/analyses/{callId}`.

**Flow:**
1. `CallTranscribedConsumer` receives event → calls Groq LLM → saves `CallAnalysis` to DB
2. `GET /api/analyses/{callId}` — returns score, report, strengths, improvements

## Shared Libraries
- **CallSense.SharedKernel** — `Entity` base class, `Result<T>` pattern
- **CallSense.Contracts** — `CallTranscribed` message record (shared between services)
- **CallSense.Infrastructure.Shared** — `OutboxMessage` entity

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| Inline transcription in Intake | Simpler than a separate transcription service; Groq Whisper is fast enough for synchronous use |
| Transactional Outbox Pattern | Guarantees at-least-once delivery without distributed transactions — event is saved atomically with the business record |
| MassTransit 8.4.0 | v9 requires a paid commercial license |
| Groq (free tier) | `whisper-large-v3-turbo` for transcription, `llama-3.3-70b-versatile` for analysis — both free |
| Per-tenant blob containers | Isolation: each tenant's audio files live in `tenant-{tenantId}` container |
| `X-Tenant-Id` header | Simple tenant identification; enforced in service layer (not just controller) |
| `MemoryStream` copy in controller | `IFormFile.OpenReadStream()` is non-seekable — must be copied before passing to service that reads it twice |
| `ByteArrayContent` for Groq upload | `StreamContent` disposes the underlying stream when the HTTP form is disposed |

## Local Ports
| Container/Service | Port |
|-------------------|------|
| Intake API | 5092 |
| Analysis API | 5093 |
| PostgreSQL | 5433 (host) → 5432 (container) |
| RabbitMQ AMQP | 5672 |
| RabbitMQ Management UI | 15672 |
| Azurite Blob | 10000 |
