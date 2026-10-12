using System.Net;
using System.Net.Http.Json;
using ListingSearch.Api.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ListingSearch.Tests;

/// <summary>
/// Verifies real HTTP binding, validation responses, and search results through the API.
/// </summary>
public sealed class ListingsEndpointTests
{
    [Fact]
    public async Task Search_WhenCalledWithoutFilters_ReturnsFirstPageOfLoadedListings()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/listings/search");
        var body = await response.Content.ReadFromJsonAsync<SearchResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(12, body.TotalResults);
        Assert.Equal(10, body.PageSize);
        Assert.Equal(10, body.Items.Count);
    }

    [Fact]
    public async Task Search_WhenPriceRangeIsInvalid_ReturnsFieldSpecificHttp400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/listings/search?minPrice=500000&maxPrice=400000");
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains("maxPrice", body.Errors.Keys);
    }

    [Theory]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=-2")]
    [InlineData("?pageSize=101")]
    [InlineData("?page=0")]
    [InlineData("?targetBudget=0")]
    [InlineData("?minPrice=not-a-price")]
    public async Task Search_WhenQueryValueIsInvalid_ReturnsHttp400(string query)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/listings/search{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WhenNoListingsMatch_ReturnsHttp200WithEmptyResults()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/listings/search?city=Unknown");
        var body = await response.Content.ReadFromJsonAsync<SearchResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Empty(body.Items);
        Assert.Equal(0, body.TotalPages);
    }

    [Fact]
    public async Task Search_WhenPageIsBeyondResults_ReturnsHttp200AndAccurateTotals()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/listings/search?page=99&pageSize=5");
        var body = await response.Content.ReadFromJsonAsync<SearchResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Empty(body.Items);
        Assert.Equal(12, body.TotalResults);
        Assert.Equal(3, body.TotalPages);
    }

    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("http://localhost:8080")]
    public async Task Cors_WhenLocalFrontendRequestsApi_AllowsItsOrigin(string origin)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/listings/search");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    private sealed record ValidationProblemDetails(
        Dictionary<string, string[]> Errors);
}
