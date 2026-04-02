using FluentValidation;
using Senda.Application.Commands;

namespace Senda.Application.Validators;

/// <summary>
/// Validator for IngestDocumentCommand - ensures FileStream is not empty and ContentType is supported.
/// </summary>
public class IngestDocumentCommandValidator : AbstractValidator<IngestDocumentCommand>
{
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "text/plain"
    };

    public IngestDocumentCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("FileStream is required.")
            .Must(stream => stream.Length > 0)
            .WithMessage("FileStream cannot be empty.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("FileName is required.")
            .MaximumLength(255)
            .WithMessage("FileName cannot exceed 255 characters.");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("ContentType is required.")
            .Must(BeSupportedContentType)
            .WithMessage("ContentType must be 'application/pdf' or 'text/plain'.");
    }

    private static bool BeSupportedContentType(string contentType)
    {
        return SupportedContentTypes.Contains(contentType);
    }
}
