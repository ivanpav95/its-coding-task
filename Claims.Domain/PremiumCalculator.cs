namespace Claims.Domain;

public static class PremiumCalculator
{
    private const decimal BaseDayRate = 1250m;
    private const int FullRateDays = 30;
    private const int FirstDiscountDays = 150;

    /// <summary>
    /// Premium for an insurance period. Both dates are included, so a cover from 1 Jan to 1 Jan is one day.
    /// </summary>
    public static decimal Calculate(DateOnly startDate, DateOnly endDate, CoverType coverType)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(endDate, startDate);

        var days = endDate.DayNumber - startDate.DayNumber + 1;
        var rates = RatesFor(coverType);
        var dayRate = BaseDayRate * rates.Multiplier;

        var fullRate = Math.Min(days, FullRateDays);
        var firstDiscount = Math.Clamp(days - FullRateDays, 0, FirstDiscountDays);
        var secondDiscount = Math.Max(days - FullRateDays - FirstDiscountDays, 0);

        return dayRate * (fullRate
            + firstDiscount * (1 - rates.FirstDiscount)
            + secondDiscount * (1 - rates.SecondDiscount));
    }

    // Discounts are off the base day rate; "additional" is added to the previous discount.
    private static Rates RatesFor(CoverType coverType) => coverType switch
    {
        CoverType.Yacht => new(Multiplier: 1.1m, FirstDiscount: 0.05m, SecondDiscount: 0.05m + 0.03m),
        CoverType.PassengerShip => new(Multiplier: 1.2m, FirstDiscount: 0.02m, SecondDiscount: 0.02m + 0.01m),
        CoverType.Tanker => new(Multiplier: 1.5m, FirstDiscount: 0.02m, SecondDiscount: 0.02m + 0.01m),
        _ => new(Multiplier: 1.3m, FirstDiscount: 0.02m, SecondDiscount: 0.02m + 0.01m)
    };

    private sealed record Rates(decimal Multiplier, decimal FirstDiscount, decimal SecondDiscount);
}
