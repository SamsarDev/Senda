using FluentValidation;
using Senda.Application.Commands;

namespace Senda.Application.Validators;

/// <summary>
/// Validator for SendMessageCommand - prevents empty or excessively long messages.
/// </summary>
public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    private const int MaxMessageLength = 4000; // Protection against token saturation
    private const int MinMessageLength = 1;

    public SendMessageCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        RuleFor(x => x.CustomerIdentifier)
            .NotEmpty()
            .WithMessage("CustomerIdentifier is required.")
            .MaximumLength(255)
            .WithMessage("CustomerIdentifier cannot exceed 255 characters.");

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message cannot be empty.")
            .MinimumLength(MinMessageLength)
            .WithMessage($"Message must be at least {MinMessageLength} character(s).")
            .MaximumLength(MaxMessageLength)
            .WithMessage($"Message cannot exceed {MaxMessageLength} characters.");
    }
}
