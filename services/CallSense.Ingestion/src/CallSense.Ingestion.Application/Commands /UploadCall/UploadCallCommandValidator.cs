using FluentValidation;

namespace CallSense.Ingestion.Application.Commands.UploadCall;

// FluentValidation validator for UploadCallCommand.
// MediatR runs this BEFORE the handler via a pipeline behaviour (registered in DI).
// If validation fails, the handler is never called — the pipeline short-circuits
// and returns a validation error automatically.
//
// The handler has its own domain-level guards too (duplicate checking, extra safety),
// but FluentValidation is the first line of defence.
public class UploadCallCommandValidator : AbstractValidator<UploadCallCommand>
{
    // Allowed MIME types — matches the handler's AllowedContentTypes set.
    // Duplication is intentional: validator catches bad input at the API boundary,
    // handler guards catch logic violations deeper in.
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "audio/mpeg",   // .mp3
        "audio/wav",    // .wav
        "audio/mp4",    // .m4a
        "audio/ogg",    // .ogg
        "audio/webm"    // .webm
    ];

    private const long MaxFileSizeBytes = 500 * 1024 * 1024; // 500 MB

    public UploadCallCommandValidator()
    {
        // TenantId must be a non-empty Guid — Guid.Empty is a common accident
        // (e.g., forgot to set the header). Catching it here gives a clear error.
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        // FileName must exist and fit in the DB column (VARCHAR 255)
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("FileName is required.")
            .MaximumLength(255);

        // ContentType must be one of the supported audio formats.
        // Must() accepts a predicate — clean way to express set membership.
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("ContentType must be audio/mpeg, audio/wav, audio/mp4, audio/ogg, or audio/webm.");

        // File size must be positive and under 500 MB.
        // GreaterThan(0) rejects empty files. LessThanOrEqualTo catches oversized files
        // before the stream even reaches blob storage.
        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .WithMessage("File must not be empty.")
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("File size must not exceed 500 MB.");

        // FileContent must not be null — defensive check so the handler
        // never NullReferenceException on file.OpenReadStream().
        RuleFor(x => x.FileContent)
            .NotNull()
            .WithMessage("FileContent is required.");
    }
}
