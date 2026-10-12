using System.Text.Json.Serialization;

namespace ListingSearch.Api.Models;

/// <summary>
/// Represents one property record as supplied by a listing source.
/// </summary>
public sealed class Listing
{
    public required string Id { get; init; }

    public required string Source { get; init; }

    public required string Address { get; init; }

    public required string City { get; init; }

    public required string State { get; init; }

    public required string Zip { get; init; }

    public required decimal Price { get; init; }

    public required int Bedrooms { get; init; }

    public required decimal Bathrooms { get; init; }

    public required int Sqft { get; init; }

    public required decimal Latitude { get; init; }

    public required decimal Longitude { get; init; }

    public required string ListedDate { get; init; }

    public required string Status { get; init; }

    public required string Description { get; init; }

    /// <summary>
    /// Parses the source ISO date for date comparisons without changing its serialized value.
    /// </summary>
    [JsonIgnore]
    public DateOnly ListedOn => DateOnly.ParseExact(ListedDate, "yyyy-MM-dd");
}
