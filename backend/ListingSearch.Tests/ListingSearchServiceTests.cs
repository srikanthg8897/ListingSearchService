using ListingSearch.Api.DTOs;
using ListingSearch.Api.Models;
using ListingSearch.Api.Repositories;
using ListingSearch.Api.Services;

namespace ListingSearch.Tests;

/// <summary>
/// Verifies filtering, stable ranking, and pagination through the search service.
/// </summary>
public sealed class ListingSearchServiceTests
{
    [Fact]
    public void SearchListings_WithoutFilters_ReturnsAllListingsAndDefaultPagination()
    {
        var response = CreateSearchService(TestListings.Sample).SearchListings(new SearchRequest());

        Assert.Equal(12, response.TotalResults);
        Assert.Equal(1, response.Page);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(2, response.TotalPages);
        Assert.Equal(10, response.Items.Count);
    }

    [Fact]
    public void SearchListings_WhenMinimumPriceIsProvided_ReturnsListingsAtOrAboveMinimum()
    {
        var response = Search(TestListings.Sample, new SearchRequest { MinPrice = 525_000 });

        Assert.All(response.Items, result => Assert.True(result.Listing.Price >= 525_000));
        Assert.Equal(5, response.TotalResults);
    }

    [Fact]
    public void SearchListings_WhenMaximumPriceIsProvided_ReturnsListingsAtOrBelowMaximum()
    {
        var response = Search(TestListings.Sample, new SearchRequest { MaxPrice = 415_000 });

        Assert.All(response.Items, result => Assert.True(result.Listing.Price <= 415_000));
        Assert.Equal(3, response.TotalResults);
    }

    [Fact]
    public void SearchListings_WhenMinimumBedroomsIsProvided_ReturnsListingsAtOrAboveMinimum()
    {
        var response = Search(TestListings.Sample, new SearchRequest { MinBedrooms = 4 });

        Assert.Equal(2, response.TotalResults);
        Assert.All(response.Items, result => Assert.True(result.Listing.Bedrooms >= 4));
    }

    [Fact]
    public void SearchListings_WhenCityHasCaseAndWhitespaceDifferences_MatchesTrimmedCity()
    {
        var response = Search(TestListings.Sample, new SearchRequest { City = "  sPrInGfIeLd " });

        Assert.Equal(4, response.TotalResults);
        Assert.All(response.Items, result => Assert.Equal("Springfield", result.Listing.City));
    }

    [Fact]
    public void SearchListings_WhenSourceCityHasSurroundingWhitespace_MatchesTrimmedCity()
    {
        var listings = new[] { TestListings.Create("one", city: "  Springfield  ") };

        var response = Search(listings, new SearchRequest { City = "Springfield" });

        Assert.Single(response.Items);
    }

    [Fact]
    public void SearchListings_WhenKeywordHasCaseAndWhitespaceDifferences_MatchesDescription()
    {
        var response = Search(TestListings.Sample, new SearchRequest { Keyword = "  METRO " });

        Assert.Equal(2, response.TotalResults);
    }

    [Fact]
    public void SearchListings_WhenFiltersAreCombined_ReturnsOnlyListingsMatchingEveryFilter()
    {
        var response = Search(TestListings.Sample, new SearchRequest
        {
            MinPrice = 450_000,
            MaxPrice = 500_000,
            MinBedrooms = 3,
            City = "Vienna",
            Keyword = "metro"
        });

        Assert.Equal(2, response.TotalResults);
        Assert.All(response.Items, result =>
        {
            Assert.InRange(result.Listing.Price, 450_000, 500_000);
            Assert.True(result.Listing.Bedrooms >= 3);
            Assert.Equal("Vienna", result.Listing.City);
            Assert.Contains("metro", result.Listing.Description, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void SearchListings_WhenCityHasNoMatches_ReturnsEmptyPageAndZeroPages()
    {
        var response = Search(TestListings.Sample, new SearchRequest { City = "Unknown City" });

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalResults);
        Assert.Equal(0, response.TotalPages);
    }

    [Fact]
    public void SearchListings_WhenKeywordHasNoMatches_ReturnsEmptyPage()
    {
        var response = Search(TestListings.Sample, new SearchRequest { Keyword = "nonexistent phrase" });

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalResults);
    }

    [Fact]
    public void SearchListings_WhenBudgetIsProvided_RanksCloserPricesHigher()
    {
        var listings = new[]
        {
            TestListings.Create("far", price: 600_000),
            TestListings.Create("close", price: 450_000)
        };

        var response = Search(listings, new SearchRequest { TargetBudget = 450_000 });

        Assert.Equal("close", response.Items[0].Listing.Id);
    }

    [Fact]
    public void SearchListings_WhenBudgetIsOmitted_RanksNewerListingsHigher()
    {
        var listings = new[]
        {
            TestListings.Create("old", listedDate: "2026-08-20"),
            TestListings.Create("new", listedDate: "2026-09-04")
        };

        var response = Search(listings, new SearchRequest());

        Assert.Equal("new", response.Items[0].Listing.Id);
    }

    [Fact]
    public void SearchListings_WhenScoresAndDatesTie_SortsBySourceThenIdOrdinally()
    {
        var listings = new[]
        {
            TestListings.Create("B", source: "MLS_B"),
            TestListings.Create("Z", source: "MLS_A"),
            TestListings.Create("A", source: "MLS_A")
        };

        var response = Search(listings, new SearchRequest());

        Assert.Equal(new[] { ("MLS_A", "A"), ("MLS_A", "Z"), ("MLS_B", "B") },
            response.Items.Select(item => (item.Listing.Source, item.Listing.Id)));
    }

    [Fact]
    public void SearchListings_WhenPagesAreRequested_ReturnsStableNonOverlappingPages()
    {
        var listings = Enumerable.Range(1, 5)
            .Select(index => TestListings.Create($"id-{index:D2}"))
            .Reverse()
            .ToArray();
        var service = CreateSearchService(listings);
        var firstPage = service.SearchListings(new SearchRequest { PageSize = 2, Page = 1 });
        var secondPage = service.SearchListings(new SearchRequest { PageSize = 2, Page = 2 });

        Assert.Equal(new[] { "id-01", "id-02" }, firstPage.Items.Select(item => item.Listing.Id));
        Assert.Equal(new[] { "id-03", "id-04" }, secondPage.Items.Select(item => item.Listing.Id));
        Assert.Equal(3, firstPage.TotalPages);
    }

    [Fact]
    public void SearchListings_WhenLastPageIsRequested_ReturnsRemainingListings()
    {
        var listings = Enumerable.Range(1, 5)
            .Select(index => TestListings.Create($"id-{index:D2}"))
            .ToArray();

        var response = Search(listings, new SearchRequest { Page = 3, PageSize = 2 });

        Assert.Single(response.Items);
        Assert.Equal("id-05", response.Items[0].Listing.Id);
        Assert.Equal(3, response.TotalPages);
    }

    [Fact]
    public void SearchListings_WhenPageExceedsAvailableResults_ReturnsEmptyItemsAndAccurateMetadata()
    {
        var response = Search(TestListings.Sample, new SearchRequest { Page = 8, PageSize = 5 });

        Assert.Empty(response.Items);
        Assert.Equal(12, response.TotalResults);
        Assert.Equal(3, response.TotalPages);
        Assert.Equal(8, response.Page);
    }

    [Fact]
    public void SearchListings_WhenIdsRepeatAcrossSources_PreservesBothSourceRecords()
    {
        var listings = new[]
        {
            TestListings.Create("same", source: "MLS_A"),
            TestListings.Create("same", source: "MLS_B")
        };

        var response = Search(listings, new SearchRequest());

        Assert.Equal(2, response.TotalResults);
        Assert.Equal(new[] { "MLS_A", "MLS_B" },
            response.Items.Select(item => item.Listing.Source));
    }

    /// <summary>
    /// Searches a fixture with the same fixed UTC scoring date used throughout these tests.
    /// </summary>
    private static ListingSearchService CreateSearchService(IEnumerable<Listing> listings) =>
        new(
            new InMemoryListingRepository(listings),
            new RelevanceScoringService(new FixedTimeProvider(
                new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero))));

    /// <summary>
    /// Runs a single request against a deterministic listing fixture.
    /// </summary>
    private static ListingSearch.Api.DTOs.SearchResponse Search(
        IEnumerable<Listing> listings,
        SearchRequest request) =>
        CreateSearchService(listings).SearchListings(request);

    private sealed class InMemoryListingRepository(IEnumerable<Listing> listings) : IListingRepository
    {
        public IReadOnlyList<Listing> GetAll() => listings.ToArray();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
