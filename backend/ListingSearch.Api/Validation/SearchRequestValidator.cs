using ListingSearch.Api.DTOs;

namespace ListingSearch.Api.Validation;

/// <summary>
/// Collects field-specific errors for invalid search filters and pagination values.
/// </summary>
public sealed class SearchRequestValidator
{
    private const int MaximumPageSize = 100;

    /// <summary>
    /// Returns an empty error map for valid input or query-field errors for invalid input.
    /// </summary>
    public Dictionary<string, string[]> ValidateSearchRequest(SearchRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        if (request.MinPrice < 0)
        {
            AddError(errors, "minPrice", "Minimum price cannot be negative.");
        }

        if (request.MaxPrice < 0)
        {
            AddError(errors, "maxPrice", "Maximum price cannot be negative.");
        }

        if (request.MinPrice.HasValue
            && request.MaxPrice.HasValue
            && request.MinPrice > request.MaxPrice)
        {
            AddError(errors, "maxPrice", "Maximum price must be greater than or equal to minimum price.");
        }

        if (request.MinBedrooms < 0)
        {
            AddError(errors, "minBedrooms", "Minimum bedrooms cannot be negative.");
        }

        if (request.TargetBudget <= 0)
        {
            AddError(errors, "targetBudget", "Target budget must be greater than zero.");
        }

        if (request.Page < 1)
        {
            AddError(errors, "page", "Page number must be at least 1.");
        }

        if (request.PageSize < 1 || request.PageSize > MaximumPageSize)
        {
            AddError(errors, "pageSize", "Page size must be between 1 and 100.");
        }

        return errors.ToDictionary(
            error => error.Key,
            error => error.Value.ToArray(),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Adds a validation message to the field's error collection.
    /// </summary>
    private static void AddError(
        IDictionary<string, List<string>> errors,
        string field,
        string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors.Add(field, messages);
        }

        messages.Add(message);
    }
}
