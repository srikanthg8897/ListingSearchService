using ListingSearch.Api.Services;

namespace ListingSearch.Tests;

/// <summary>
/// Verifies budget and recency scoring at boundaries and under a fixed UTC date.
/// </summary>
public sealed class RelevanceScoringServiceTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(450_000, 450_000, 100)]
    [InlineData(400_000, 450_000, 88.89)]
    [InlineData(0, 450_000, 0)]
    [InlineData(900_000, 450_000, 0)]
    public void CalculateBudgetProximityScore_WhenPriceDiffersFromBudget_ReturnsBoundedScore(
        decimal price,
        decimal budget,
        decimal expected)
    {
        var score = RelevanceScoringService.CalculateBudgetProximityScore(price, budget);

        Assert.Equal(expected, decimal.Round(score, 2));
        Assert.InRange(score, 0, 100);
    }

    [Theory]
    [InlineData("2026-09-04", 0)]
    [InlineData("2026-08-20", 15)]
    [InlineData("2026-06-06", 90)]
    public void CalculateRecencyScore_WhenListingAgeVaries_UsesNinetyDayLinearDecay(
        string listedDate,
        int expectedAgeInDays)
    {
        var service = CreateScoringService();
        var score = service.CalculateRecencyScore(TestListings.Create("one", listedDate: listedDate));

        var expectedScore = expectedAgeInDays >= 90
            ? 0
            : 100m * (90 - expectedAgeInDays) / 90;
        Assert.Equal(expectedScore, score);
    }

    [Fact]
    public void CalculateRecencyScore_WhenListingIsFutureDated_TreatsItAsListedToday()
    {
        var service = CreateScoringService();
        var score = service.CalculateRecencyScore(
            TestListings.Create("future", listedDate: "2026-09-05"));

        Assert.Equal(100, score);
    }

    [Fact]
    public void CalculateRelevanceScore_WhenBudgetIsOmitted_UsesFullRecencyScore()
    {
        var service = CreateScoringService();
        var listing = TestListings.Create("one", listedDate: "2026-08-20");

        var score = service.CalculateRelevanceScore(listing, null);

        Assert.Equal(service.CalculateRecencyScore(listing), score);
    }

    [Fact]
    public void CalculateRelevanceScore_WhenBudgetIsProvided_UsesSeventyThirtyWeighting()
    {
        var service = CreateScoringService();
        var listing = TestListings.Create("one", price: 400_000, listedDate: "2026-08-20");

        var score = service.CalculateRelevanceScore(listing, 450_000);

        var expected = (100m * 400_000 / 450_000 * 0.70m) + (100m * 75 / 90 * 0.30m);
        Assert.Equal(expected, score);
    }

    /// <summary>
    /// Creates a scorer whose UTC date is fixed for repeatable age calculations.
    /// </summary>
    private static RelevanceScoringService CreateScoringService() =>
        new(new FixedTimeProvider(FixedUtcNow));

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
