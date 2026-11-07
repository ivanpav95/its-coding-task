using Claims.Application.Contracts;
using FluentValidation;

namespace Claims.Application.Validation;

public class CreateClaimRequestValidator : AbstractValidator<CreateClaimRequest>
{
    public const decimal MaxDamageCost = 100_000m;

    public CreateClaimRequestValidator()
    {
        RuleFor(r => r.CoverId).NotEmpty();
        RuleFor(r => r.Name).NotEmpty();
        RuleFor(r => r.Type).IsInEnum();
        RuleFor(r => r.DamageCost).GreaterThan(0).LessThanOrEqualTo(MaxDamageCost);
    }
}
