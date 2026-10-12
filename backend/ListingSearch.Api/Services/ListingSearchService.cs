using ListingSearch.Api.DTOs;
using ListingSearch.Api.Models;
using ListingSearch.Api.Repositories;

namespace ListingSearch.Api.Services;

/// <summary>
/// Filters, scores, deterministically sorts, and paginates the in-memory listings.
/// </summary>
public sealed class ListingSearchService(
    IListingRepository listingRepository,
    RelevanceScoringService scoringService)
{
    /// <summary>
    /// Returns every matching listing in stable relevance order before applying pagination.
    /// </summary>
    public SearchResponse SearchListings(SearchRequest request)
    {
        var matchingListings = listingRepository.GetAll()
            .Where(listing => MatchesFilters(listing, request))
            .Select(listing => new ListingSearchResult(
                listing,
                scoringService.CalculateRelevanceScore(listing, request.TargetBudget)))
            .OrderByDescending(result => result.RelevanceScore)
            .ThenByDescending(result => result.Listing.ListedOn)
            .ThenBy(result => result.Listing.Source, StringComparer.Ordinal)
            .ThenBy(result => result.Listing.Id, StringComparer.Ordinal)
            .ToList();

        var totalResults = matchingListings.Count;
        var totalPages = (int)Math.Ceiling(totalResults / (decimal)request.PageSize);
        var offset = ((long)request.Page - 1) * request.PageSize;
        var pageItems = offset >= totalResults
            ? []
            : matchingListings.Skip((int)offset).Take(request.PageSize).ToArray();

        return new SearchResponse(
            pageItems,
            request.Page,
            request.PageSize,
            totalResults,
            totalPages);
    }

    /// <summary>
    /// Applies only the documented price, bedroom, city, and description filters.
    /// </summary>
    private static bool MatchesFilters(Listing listing, SearchRequest request)
    {
        if (request.MinPrice.HasValue && listing.Price < request.MinPrice.Value)
        {
            return false;
        }

        if (request.MaxPrice.HasValue && listing.Price > request.MaxPrice.Value)
        {
            return false;
        }

        if (request.MinBedrooms.HasValue && listing.Bedrooms < request.MinBedrooms.Value)
        {
            return false;
        }

        var city = request.City?.Trim();
        if (!string.IsNullOrEmpty(city)
            && !string.Equals(listing.City.Trim(), city, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var keyword = request.Keyword?.Trim();
        return string.IsNullOrEmpty(keyword)
            || listing.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }
}
