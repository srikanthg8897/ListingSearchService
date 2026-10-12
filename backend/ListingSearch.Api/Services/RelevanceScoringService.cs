using ListingSearch.Api.Models;

namespace ListingSearch.Api.Services;

/// <summary>
/// Scores listing budget proximity and age using a testable UTC date reference.
/// </summary>
public sealed class RelevanceScoringService(TimeProvider timeProvider)
{
    private const int RecencyWindowDays = 90;
    private const decimal BudgetWeight = 0.70m;
    private const decimal RecencyWeight = 0.30m;

    /// <summary>
    /// Returns a score from 0 to 100, using recency alone when no budget was requested.
    /// </summary>
    public decimal CalculateRelevanceScore(Listing listing, decimal? targetBudget)
    {
        var recencyScore = CalculateRecencyScore(listing);

        if (!targetBudget.HasValue)
        {
            return recencyScore;
        }

        // Budget fit is dominant; recency contributes the remaining weight.
        var budgetScore = CalculateBudgetProximityScore(listing.Price, targetBudget.Value);
        return (budgetScore * BudgetWeight) + (recencyScore * RecencyWeight);
    }

    /// <summary>
    /// Scores a price by its absolute percentage difference from the positive target budget.
    /// </summary>
    public static decimal CalculateBudgetProximityScore(decimal price, decimal targetBudget)
    {
        // Normalize by the target budget so a 100% deviation reaches zero.
        var difference = price >= targetBudget
            ? price - targetBudget
            : targetBudget - price;

        if (difference >= targetBudget)
        {
            return 0;
        }

        return 100m * (targetBudget - difference) / targetBudget;
    }

    /// <summary>
    /// Applies a 90-day linear decay, treating future dates as today and older dates as zero.
    /// </summary>
    public decimal CalculateRecencyScore(Listing listing)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var ageInDays = Math.Max(0, today.DayNumber - listing.ListedOn.DayNumber);

        if (ageInDays >= RecencyWindowDays)
        {
            return 0;
        }

        return 100m * (RecencyWindowDays - ageInDays) / RecencyWindowDays;
    }
}
