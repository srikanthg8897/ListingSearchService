using System.Text.Json;
using ListingSearch.Api.Models;
using ListingSearch.Api.Repositories;

namespace ListingSearch.Tests;

/// <summary>
/// Verifies source JSON loading, required record fields, and malformed-file errors.
/// </summary>
public sealed class ListingRepositoryTests
{
    [Fact]
    public void ListingRepository_WhenSampleJsonIsLoaded_PreservesAllTwelveRecordsAndFields()
    {
        var listings = TestListings.Sample;

        Assert.Equal(12, listings.Count);
        Assert.Equal("MLS_A", listings[0].Source);
        Assert.Equal(450_000m, listings[0].Price);
        Assert.Equal("2026-08-29", listings[0].ListedDate);
        Assert.Contains(listings, listing => listing.Status == "pending");
    }

    [Fact]
    public void ListingRepository_WhenJsonIsMalformed_ThrowsClearDataException()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "[{");

            var exception = Assert.Throws<InvalidDataException>(() => new ListingRepository(path));

            Assert.Contains("invalid JSON", exception.Message);
            Assert.IsType<JsonException>(exception.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ListingRepository_WhenRecordOmitsRequiredField_RejectsTheRecord()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """[{"id":"missing-fields"}]""");

            Assert.Throws<InvalidDataException>(() => new ListingRepository(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ListingRepository_WhenSourceIdsRepeatAcrossFeeds_PreservesBothListings()
    {
        var path = Path.GetTempFileName();
        try
        {
            var listings = new[]
            {
                TestListings.Create("same", source: "MLS_A"),
                TestListings.Create("same", source: "MLS_B")
            };
            File.WriteAllText(path, JsonSerializer.Serialize(listings));

            var repository = new ListingRepository(path);

            Assert.Equal(2, repository.GetAll().Count);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
