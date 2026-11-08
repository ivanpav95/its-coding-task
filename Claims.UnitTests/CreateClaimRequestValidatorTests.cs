using Claims.Application.Contracts;
using Claims.Application.Validation;
using Claims.Domain;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Claims.UnitTests;

public class CreateClaimRequestValidatorTests
{
    private static readonly DateTimeOffset Now = new(2027, 1, 10, 12, 0, 0, TimeSpan.Zero);

    private static readonly CreateClaimRequest ValidRequest =
        new("cover-1", Now.AddHours(-2), "Hull damage", ClaimType.Collision, 5_000m);

    private static TestValidationResult<CreateClaimRequest> Validate(CreateClaimRequest request) =>
        new CreateClaimRequestValidator(new FakeTimeProvider(Now)).TestValidate(request);

    [Fact]
    public void A_complete_request_is_valid()
    {
        Validate(ValidRequest).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_claim_dated_now_is_valid()
    {
        Validate(ValidRequest with { Created = Now }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_claim_dated_in_the_future_is_rejected()
    {
        Validate(ValidRequest with { Created = Now.AddMinutes(1) }).ShouldHaveValidationErrorFor(r => r.Created);
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
