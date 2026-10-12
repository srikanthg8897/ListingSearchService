using ListingSearch.Api.Models;
using ListingSearch.Api.Repositories;

namespace ListingSearch.Tests;

/// <summary>
/// Builds small deterministic search fixtures and exposes the supplied sample dataset.
/// </summary>
internal static class TestListings
{
    public static IReadOnlyList<Listing> Sample =>
        new ListingRepository(Path.Combine(AppContext.BaseDirectory, "Data", "sample_listings.json"))
            .GetAll();

    /// <summary>
    /// Creates a listing with valid defaults that tests can override for one behavior.
    /// </summary>
    public static Listing Create(
        string id,
        string source = "MLS_A",
        string city = "Springfield",
        decimal price = 450_000,
        int bedrooms = 2,
        string listedDate = "2026-09-04",
        string description = "Bright home near shops.")
    {
        return new Listing
        {
            Id = id,
            Source = source,
            Address = $"{id} Main St",
            City = city,
            State = "VA",
            Zip = "22150",
            Price = price,
            Bedrooms = bedrooms,
            Bathrooms = 1.5m,
            Sqft = 980,
            Latitude = 38.7893m,
            Longitude = -77.1873m,
            ListedDate = listedDate,
            Status = "active",
            Description = description
        };
    }
}
