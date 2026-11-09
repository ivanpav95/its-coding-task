using Claims.Application.Contracts;
using Claims.Application.Validation;
using Claims.Domain;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Claims.UnitTests;

public class CreateCoverRequestValidatorTests
{
    private static readonly DateOnly Today = new(2027, 1, 10);

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2027, 1, 10, 12, 0, 0, TimeSpan.Zero));

    public CreateCoverRequestValidatorTests() =>
        _time.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo"));

    private TestValidationResult<CreateCoverRequest> Validate(DateOnly start, DateOnly end, CoverType type = CoverType.Yacht) =>
        new CreateCoverRequestValidator(_time).TestValidate(new CreateCoverRequest(start, end, type));

    [Fact]
    public void A_cover_starting_today_is_valid()
    {
        Validate(Today, Today.AddDays(10)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_cover_starting_yesterday_is_rejected()
    {
        Validate(Today.AddDays(-1), Today.AddDays(10)).ShouldHaveValidationErrorFor(r => r.StartDate);
    }

    [Fact]
    public void After_midnight_in_Oslo_yesterday_is_rejected_although_UTC_is_still_on_it()
    {
        _time.SetUtcNow(new DateTimeOffset(2027, 1, 10, 23, 30, 0, TimeSpan.Zero));

        Validate(Today, Today.AddDays(10)).ShouldHaveValidationErrorFor(r => r.StartDate);
    }

    [Fact]
    public void Before_midnight_in_Oslo_today_is_still_valid()
    {
        _time.SetUtcNow(new DateTimeOffset(2027, 1, 10, 22, 30, 0, TimeSpan.Zero));

        Validate(Today, Today.AddDays(10)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Today_moves_with_the_clock()
    {
        _time.Advance(TimeSpan.FromDays(1));

        Validate(Today, Today.AddDays(10)).ShouldHaveValidationErrorFor(r => r.StartDate);
    }

    [Fact]
    public void End_date_on_the_start_date_is_valid()
    {
        Validate(Today, Today).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void End_date_before_the_start_date_is_rejected()
    {
        Validate(Today, Today.AddDays(-1)).ShouldHaveValidationErrorFor(r => r.EndDate);
    }

    [Fact]
    public void One_calendar_year_is_the_longest_period()
    {
        Validate(Today, new DateOnly(2028, 1, 9)).ShouldNotHaveAnyValidationErrors();
        Validate(Today, new DateOnly(2028, 1, 10)).ShouldHaveValidationErrorFor(r => r.EndDate);
    }

    [Fact]
    public void A_year_that_contains_29_February_allows_366_days()
    {
        var start = new DateOnly(2027, 3, 1);

        Validate(start, new DateOnly(2028, 2, 29)).ShouldNotHaveAnyValidationErrors();
        Validate(start, new DateOnly(2028, 3, 1)).ShouldHaveValidationErrorFor(r => r.EndDate);
    }

    [Fact]
    public void An_undefined_cover_type_is_rejected()
    {
        Validate(Today, Today.AddDays(10), (CoverType)99).ShouldHaveValidationErrorFor(r => r.Type);
    }
}
