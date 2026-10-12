using ListingSearch.Api.Models;

namespace ListingSearch.Api.DTOs;

/// <summary>
/// Returns the current scored page and the metadata needed to navigate all matches.
/// </summary>
public sealed record SearchResponse(
    IReadOnlyList<ListingSearchResult> Items,
    int Page,
    int PageSize,
    int TotalResults,
    int TotalPages);

/// <summary>
/// Associates one original listing with its computed relevance score.
/// </summary>
public sealed record ListingSearchResult(Listing Listing, decimal RelevanceScore);
