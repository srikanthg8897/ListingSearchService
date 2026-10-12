using ListingSearch.Api.Models;

namespace ListingSearch.Api.Repositories;

/// <summary>
/// Provides read-only access to the source listing collection.
/// </summary>
public interface IListingRepository
{
    /// <summary>
    /// Returns every listing without deduplication or status filtering.
    /// </summary>
    IReadOnlyList<Listing> GetAll();
}
