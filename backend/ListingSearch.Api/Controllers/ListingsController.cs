using ListingSearch.Api.DTOs;
using ListingSearch.Api.Services;
using ListingSearch.Api.Validation;
using Microsoft.AspNetCore.Mvc;

namespace ListingSearch.Api.Controllers;

/// <summary>
/// Exposes the listing search endpoint and maps invalid query values to HTTP 400.
/// </summary>
[ApiController]
[Route("api/listings")]
public sealed class ListingsController(
    ListingSearchService searchService,
    SearchRequestValidator validator) : ControllerBase
{
    /// <summary>
    /// Searches listings using the supplied filters, relevance criteria, and page.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType<SearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<SearchResponse> Search([FromQuery] SearchRequest request)
    {
        var errors = validator.ValidateSearchRequest(request);
        if (errors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(errors));
        }

        return Ok(searchService.SearchListings(request));
    }
}
