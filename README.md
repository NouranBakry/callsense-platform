# CallSense

AI-powered call QA platform. Transcribes customer service calls and scores them for quality.

## Pipeline
```
POST /api/calls/upload (Intake :5092)
  → validate → transcribe (Groq Whisper) → blob storage → save to DB
  → OutboxWorker publishes CallTranscribed to RabbitMQ

CallTranscribed → Analysis service (:5093)
  → score with Groq LLaMA → save CallAnalysis → GET /api/analyses/{callId}
```

## Services
| Service | Port | Responsibility |
|---------|------|---------------|
| Intake | 5092 | Upload + transcribe audio |
| Analysis | 5093 | Score transcripts, generate reports |

## Local Dev
```bash
docker compose up -d
# Run Intake
cd services/CallSense.Intake/src/CallSense.Intake.API && dotnet run
# Run Analysis (separate terminal)
cd services/CallSense.Analysis/src/CallSense.Analysis.API && dotnet run
# Upload
curl -X POST http://localhost:5092/api/calls/upload \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -F "file=@test.m4a;type=audio/mp4"
# Get analysis (use callId from above)
curl http://localhost:5093/api/analyses/{callId} \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111"
```

## Tech Stack
- .NET 10, ASP.NET Core
- PostgreSQL + EF Core 10
- RabbitMQ + MassTransit 8
- Azure Blob Storage (Azurite locally)
- Groq Whisper API (transcription)
- Groq LLaMA 3.3 70B (analysis)
- FluentValidation, Transactional Outbox Pattern

## What's Done
- [x] Intake service — upload + inline transcription
- [x] Analysis service — LLM scoring via RabbitMQ consumer
- [x] Transactional Outbox Pattern
- [x] Clean Architecture (Domain/Application/Infrastructure/API)

## What's Next
- [ ] Auth (JWT per tenant)
- [ ] Unit + integration tests
- [ ] Notification service
