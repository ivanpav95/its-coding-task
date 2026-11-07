using Claims.Domain;
using Xunit;

namespace Claims.UnitTests;

public class PremiumCalculatorTests
{
    private static readonly DateOnly Start = new(2027, 1, 1);

    private static decimal PremiumForDays(int days, CoverType type) =>
        PremiumCalculator.Calculate(Start, Start.AddDays(days - 1), type);

    [Theory]
    [InlineData(CoverType.Yacht, 1375)]          // 1250 × 1.1
    [InlineData(CoverType.PassengerShip, 1500)]  // 1250 × 1.2
    [InlineData(CoverType.Tanker, 1875)]         // 1250 × 1.5
    [InlineData(CoverType.ContainerShip, 1625)]  // 1250 × 1.3
    [InlineData(CoverType.BulkCarrier, 1625)]    // 1250 × 1.3
    public void Days_1_to_30_cost_the_full_day_rate(CoverType type, decimal dayRate)
    {
        Assert.Equal(dayRate, PremiumForDays(1, type));
        Assert.Equal(30 * dayRate, PremiumForDays(30, type));
    }

    [Theory]
    [InlineData(CoverType.Yacht, 1306.25)]         // 1375 × 0.95
    [InlineData(CoverType.PassengerShip, 1470)]    // 1500 × 0.98
    [InlineData(CoverType.Tanker, 1837.50)]        // 1875 × 0.98
    [InlineData(CoverType.ContainerShip, 1592.50)] // 1625 × 0.98
    public void Days_31_to_180_get_the_first_discount(CoverType type, decimal dayRate)
    {
        Assert.Equal(dayRate, PremiumForDays(31, type) - PremiumForDays(30, type));
        Assert.Equal(dayRate, PremiumForDays(180, type) - PremiumForDays(179, type));
    }

    [Theory]
    [InlineData(CoverType.Yacht, 1265)]            // 1375 × 0.92 (5% + additional 3%)
    [InlineData(CoverType.PassengerShip, 1455)]    // 1500 × 0.97 (2% + additional 1%)
    [InlineData(CoverType.Tanker, 1818.75)]        // 1875 × 0.97
    [InlineData(CoverType.ContainerShip, 1576.25)] // 1625 × 0.97
    public void Day_181_onwards_gets_the_additional_discount(CoverType type, decimal dayRate)
    {
        Assert.Equal(dayRate, PremiumForDays(181, type) - PremiumForDays(180, type));
        Assert.Equal(dayRate, PremiumForDays(366, type) - PremiumForDays(365, type));
    }

    [Theory]
    [InlineData(10, 13_750.00)]   // 10 × 1375
    [InlineData(100, 132_687.50)] // 30 × 1375 + 70 × 1306.25
    [InlineData(200, 262_487.50)] // 30 × 1375 + 150 × 1306.25 + 20 × 1265
    [InlineData(366, 472_477.50)] // 30 × 1375 + 150 × 1306.25 + 186 × 1265
    public void Yacht_totals(int days, decimal expected)
    {
        Assert.Equal(expected, PremiumForDays(days, CoverType.Yacht));
    }

    [Fact]
    public void Tanker_total_for_200_days()
    {
        // 30 × 1875 + 150 × 1837.50 + 20 × 1818.75
        Assert.Equal(368_250.00m, PremiumForDays(200, CoverType.Tanker));
    }

    [Fact]
    public void Start_and_end_on_the_same_day_is_one_day()
    {
        Assert.Equal(1375m, PremiumCalculator.Calculate(Start, Start, CoverType.Yacht));
    }

    [Fact]
    public void A_full_leap_year_cover_charges_all_366_days()
    {
        var start = new DateOnly(2027, 3, 1);
        var end = new DateOnly(2028, 2, 29); // StartDate.AddYears(1).AddDays(-1), the longest allowed cover

        Assert.Equal(472_477.50m, PremiumCalculator.Calculate(start, end, CoverType.Yacht));
    }

    [Fact]
    public void End_date_before_start_date_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PremiumCalculator.Calculate(Start, Start.AddDays(-1), CoverType.Yacht));
    }
}
