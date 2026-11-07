using Claims.Application.Contracts;
using Claims.Application.Validation;
using Claims.Domain;
using FluentValidation.TestHelper;
using Xunit;

namespace Claims.UnitTests;

public class CreateClaimRequestValidatorTests
{
    private static readonly CreateClaimRequest ValidRequest =
        new("cover-1", new DateOnly(2027, 1, 10), "Hull damage", ClaimType.Collision, 5_000m);

    private static TestValidationResult<CreateClaimRequest> Validate(CreateClaimRequest request) =>
        new CreateClaimRequestValidator().TestValidate(request);

    [Fact]
    public void A_complete_request_is_valid()
    {
        Validate(ValidRequest).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(100_000)]
    public void Damage_cost_from_just_above_0_up_to_100_000_is_valid(decimal damageCost)
    {
        Validate(ValidRequest with { DamageCost = damageCost }).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(100_000.01)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Damage_cost_above_100_000_or_not_positive_is_rejected(decimal damageCost)
    {
        Validate(ValidRequest with { DamageCost = damageCost }).ShouldHaveValidationErrorFor(r => r.DamageCost);
    }

    [Fact]
    public void An_empty_name_is_rejected()
    {
        Validate(ValidRequest with { Name = "" }).ShouldHaveValidationErrorFor(r => r.Name);
    }

    [Fact]
    public void An_empty_cover_id_is_rejected()
    {
        Validate(ValidRequest with { CoverId = "" }).ShouldHaveValidationErrorFor(r => r.CoverId);
    }

    [Fact]
    public void An_undefined_claim_type_is_rejected()
    {
        Validate(ValidRequest with { Type = (ClaimType)99 }).ShouldHaveValidationErrorFor(r => r.Type);
    }
}
