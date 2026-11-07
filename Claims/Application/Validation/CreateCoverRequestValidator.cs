using Claims.Application.Contracts;
using FluentValidation;

namespace Claims.Application.Validation;

public class CreateCoverRequestValidator : AbstractValidator<CreateCoverRequest>
{
    public CreateCoverRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(r => r.StartDate)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("Start date can't be in the past.");

        RuleFor(r => r.EndDate)
            .GreaterThanOrEqualTo(r => r.StartDate)
            .WithMessage("End date can't be before the start date.")
            .LessThanOrEqualTo(r => r.StartDate.AddYears(1).AddDays(-1))
            .WithMessage("The insurance period can't be longer than one year.");

        RuleFor(r => r.Type).IsInEnum();
    }
}
