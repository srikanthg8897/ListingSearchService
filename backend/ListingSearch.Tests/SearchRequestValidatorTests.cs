using ListingSearch.Api.DTOs;
using ListingSearch.Api.Validation;

namespace ListingSearch.Tests;

/// <summary>
/// Verifies field-level validation for search filters and pagination.
/// </summary>
public sealed class SearchRequestValidatorTests
{
    private readonly SearchRequestValidator _validator = new();

    [Fact]
    public void ValidateSearchRequest_WhenDefaultsAreUsed_ReturnsNoErrors()
    {
        var errors = _validator.ValidateSearchRequest(new SearchRequest());

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateSearchRequest_WhenMinimumPriceExceedsMaximum_ReturnsPriceError()
    {
        var errors = _validator.ValidateSearchRequest(new SearchRequest
        {
            MinPrice = 500_000,
            MaxPrice = 400_000
        });

        Assert.Contains("maxPrice", errors.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateSearchRequest_WhenTargetBudgetIsNotPositive_ReturnsBudgetError(decimal targetBudget)
    {
        var errors = _validator.ValidateSearchRequest(new SearchRequest
        {
            TargetBudget = targetBudget
        });

        Assert.Contains("targetBudget", errors.Keys);
    }

    [Theory]
    [InlineData(-1, null, "minPrice")]
    [InlineData(null, -1, "maxPrice")]
    [InlineData(null, null, "minBedrooms")]
    [InlineData(null, null, "targetBudget")]
    public void ValidateSearchRequest_WhenNumericFilterIsInvalid_ReturnsFieldError(
        int? minPrice,
        int? maxPrice,
        string expectedField)
    {
        var request = new SearchRequest();
        if (expectedField == "minPrice")
        {
            request.MinPrice = minPrice;
        }
        else if (expectedField == "maxPrice")
        {
            request.MaxPrice = maxPrice;
        }
        else if (expectedField == "minBedrooms")
        {
            request.MinBedrooms = -1;
        }
        else
        {
            request.TargetBudget = 0;
        }

        var errors = _validator.ValidateSearchRequest(request);

        Assert.Contains(expectedField, errors.Keys);
    }

    [Theory]
    [InlineData(0, 10, "page")]
    [InlineData(1, 0, "pageSize")]
    [InlineData(1, -1, "pageSize")]
    [InlineData(1, 101, "pageSize")]
    public void ValidateSearchRequest_WhenPaginationIsOutsideAllowedRange_ReturnsFieldError(
        int page,
        int pageSize,
        string expectedField)
    {
        var errors = _validator.ValidateSearchRequest(new SearchRequest
        {
            Page = page,
            PageSize = pageSize
        });

        Assert.Contains(expectedField, errors.Keys);
    }
}
