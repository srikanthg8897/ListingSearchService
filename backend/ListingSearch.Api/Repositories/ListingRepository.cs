using System.Text.Json;
using ListingSearch.Api.Models;

namespace ListingSearch.Api.Repositories;

/// <summary>
/// Loads and validates the supplied JSON listing collection once for in-memory searches.
/// </summary>
public sealed class ListingRepository : IListingRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IReadOnlyList<Listing> _listings;

    /// <summary>
    /// Reads the listing file and rejects malformed JSON or incomplete source records.
    /// </summary>
    public ListingRepository(string listingsFilePath)
    {
        List<Listing>? listings;

        try
        {
            listings = JsonSerializer.Deserialize<List<Listing>>(
                File.ReadAllText(listingsFilePath),
                JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"The listing file '{listingsFilePath}' contains invalid JSON or listing fields.",
                exception);
        }

        if (listings is null)
        {
            throw new InvalidDataException(
                $"The listing file '{listingsFilePath}' must contain a JSON array.");
        }

        foreach (var listing in listings)
        {
            if (listing is null
                || string.IsNullOrWhiteSpace(listing.Id)
                || string.IsNullOrWhiteSpace(listing.Source)
                || string.IsNullOrWhiteSpace(listing.Address)
                || string.IsNullOrWhiteSpace(listing.City)
                || string.IsNullOrWhiteSpace(listing.State)
                || string.IsNullOrWhiteSpace(listing.Zip)
                || string.IsNullOrWhiteSpace(listing.Status)
                || string.IsNullOrWhiteSpace(listing.Description)
                || !DateOnly.TryParseExact(listing.ListedDate, "yyyy-MM-dd", out _))
            {
                throw new InvalidDataException(
                    $"The listing file '{listingsFilePath}' contains a record with missing fields or an invalid listedDate.");
            }
        }

        _listings = listings.AsReadOnly();
    }

    /// <summary>
    /// Returns the in-memory source records as a read-only collection.
    /// </summary>
    public IReadOnlyList<Listing> GetAll() => _listings;
}
