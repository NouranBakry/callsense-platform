using FluentValidation;

namespace CallSense.Intake.Application.Validators;

public class UploadCallRequestValidator : AbstractValidator<UploadCallRequest>
{
    private static readonly string[] AllowedContentTypes =
        ["audio/mpeg", "audio/wav", "audio/mp4", "audio/ogg", "audio/webm"];

    public UploadCallRequestValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("ContentType must be audio/mpeg, audio/wav, audio/mp4, audio/ogg, or audio/webm.");
        RuleFor(x => x.FileSizeBytes).GreaterThan(0).LessThanOrEqualTo(500 * 1024 * 1024);
        RuleFor(x => x.FileContent).NotNull();
    }
}
