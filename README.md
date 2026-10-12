# Listing Search Service

A small full-stack property-listing search application built for the .NET coding assessment. The ASP.NET Core API reads the supplied JSON into memory, applies the documented filters, ranks matches, and returns paginated results to a React and TypeScript interface.

## Requirements implemented

- Minimum/maximum price, minimum bedrooms, city, and description-keyword filters.
- Budget- and recency-based relevance ranking with a stable, deterministic tie order.
- Validated query parameters, structured HTTP 400 responses, empty-result handling, and bounded pagination.
- A React interface connected to the live API, including loading, error, empty, and pagination states.
- xUnit unit and HTTP integration tests for search behavior and edge cases.

Source listings retain their original fields and source identities. Listings are not deduplicated, and status is not used as an undocumented search filter.

## Technology stack

- .NET 9 / ASP.NET Core Web API
- C# and `System.Text.Json`
- xUnit and ASP.NET Core's integration-test host
- React 18, TypeScript, and Vite
- Fetch API and in-memory listing storage

## Prerequisites

- .NET 9 SDK
- Node.js 18 or later and npm

The backend targets `net9.0`, matching the installed SDK used to develop this project. Vite requires a modern Node.js release; the legacy Node 6 executable previously present on the development machine is not sufficient.

## Project structure

```text
backend/
  ListingSearch.sln
  ListingSearch.Api/
    Controllers/
    DTOs/
    Models/
    Repositories/
    Services/
    Validation/
  ListingSearch.Tests/
frontend/
  src/
    components/
    services/
    types/
requirements/
  sample_listings.json
```

The assessment PDFs remain local and are excluded from Git. The supplied JSON is linked into the API and test outputs and is not modified.

## Run the backend

From the repository root:

```powershell
dotnet run --project backend\ListingSearch.Api -- --urls http://localhost:5000
```

The API loads and validates `requirements\sample_listings.json` at startup. A missing or malformed data file fails startup with a descriptive error rather than silently returning partial data.

## Run the frontend

In a second terminal, configure the API base URL and start Vite:

```powershell
Copy-Item frontend\.env.example frontend\.env.local
Set-Location frontend
npm install
npm run dev
```

Open `http://localhost:8080`. Vite binds to port `8080` by default, and the API allows the local UI origins `http://localhost:8080` and `http://localhost:5173`. `.env.local` is ignored by Git.

## API

### `GET /api/listings/search`

| Parameter | Meaning | Default / validation |
|---|---|---|
| `minPrice` | Inclusive minimum list price in USD | Optional; must be non-negative |
| `maxPrice` | Inclusive maximum list price in USD | Optional; must be non-negative and at least `minPrice` |
| `minBedrooms` | Inclusive minimum bedroom count | Optional; must be non-negative |
| `city` | Exact city match, ignoring case and surrounding whitespace | Optional |
| `keyword` | Case-insensitive substring matched against `description` | Optional; surrounding query whitespace is ignored |
| `targetBudget` | Budget used for relevance ranking | Optional; must be greater than zero when supplied |
| `page` | One-based page number | Defaults to `1`; must be at least `1` |
| `pageSize` | Number of results on a page | Defaults to `10`; must be in `1..100` |

Filtering is applied before scoring and ordering; pagination is applied last. An out-of-range page returns HTTP 200 with an empty `items` array and the actual total counts. Invalid values and malformed numeric query parameters return HTTP 400. A valid search with no matches returns HTTP 200 with zero results and zero pages.

Example:

```text
GET http://localhost:5000/api/listings/search?city=Springfield&targetBudget=500000&page=1&pageSize=1
```

The response wraps each unchanged listing in a `listing` property alongside its score:

```json
{
  "items": [
    {
      "listing": {
        "id": "A2",
        "source": "MLS_A",
        "address": "456 Oak Ave",
        "city": "Springfield",
        "state": "VA",
        "zip": "22150",
        "price": 525000,
        "bedrooms": 3,
        "bathrooms": 2,
        "sqft": 1450,
        "latitude": 38.7791,
        "longitude": -77.1901,
        "listedDate": "2026-09-02",
        "status": "active",
        "description": "Updated kitchen, fenced yard, close to schools."
      },
      "relevanceScore": 83.16666666666666666666666667
    }
  ],
  "page": 1,
  "pageSize": 1,
  "totalResults": 4,
  "totalPages": 4
}
```

The example demonstrates the response shape; relevance scores depend on the current UTC date.

## Relevance scoring and trade-offs

The score is bounded from 0 to 100 and uses only the agreed budget and date inputs:

1. **Budget proximity** (only when `targetBudget` is provided): `budgetScore = max(0, 100 * (1 - abs(price - targetBudget) / targetBudget))`. A listing at the target budget scores 100. A listing at least 100% away from the target scores 0.
2. **Recency**: `ageDays = max(0, UTC-today - listedDate)` and `recencyScore = max(0, 100 * (1 - ageDays / 90))`. This is a simple linear decay. Future-dated listings are treated as zero days old, and listings at least 90 days old score 0.
3. **Combined score when budget is supplied**: `relevanceScore = 0.70 * budgetScore + 0.30 * recencyScore`.
4. **No target budget**: rank by the full recency score, without applying the 30% weight.

Scores are computed using decimal arithmetic. The current UTC date comes from the injectable .NET `TimeProvider`, allowing deterministic scoring tests. Equal scores are ordered by `listedDate` descending, then `source` ascending, then `id` ascending using ordinal string comparison. This transparent weighted model is easy to explain and change; it intentionally does not infer other preferences or use distance, status, or description relevance as ranking signals.

## Validation and pagination

`SearchRequestValidator` returns field-specific validation errors for negative price/bedroom filters, reversed price ranges, non-positive budgets, and invalid page/page-size values. ASP.NET model binding rejects malformed numeric values. The endpoint returns these errors as a validation-problem response with HTTP 400.

The approved defaults are page `1`, page size `10`, and maximum page size `100`. `totalPages` is `0` when there are no matches; requesting a page beyond the last page does not change the reported total metadata or cause an error.

## Automated tests

Run the backend unit and HTTP integration suite from the repository root:

```powershell
dotnet test backend\ListingSearch.sln
```

The tests use a fixed UTC clock and cover JSON loading/deserialization, source IDs, each filter and combined filters, empty results, validation, score boundaries and weights, deterministic tie ordering, pagination, HTTP behavior, and malformed query values.

Frontend type-check and production build:

```powershell
Set-Location frontend
npm run typecheck
npm run build
```

## Code Structure and Responsibilities

| Class or component | File location | Responsibility | Important methods |
|---|---|---|---|
| `Listing` | `backend/ListingSearch.Api/Models/Listing.cs` | Represents one original source record and parses its ISO date for comparisons. | `ListedOn` |
| `IListingRepository` | `backend/ListingSearch.Api/Repositories/IListingRepository.cs` | Defines read-only access to the listing collection. | `GetAll` |
| `ListingRepository` | `backend/ListingSearch.Api/Repositories/ListingRepository.cs` | Loads, validates, and retains the JSON records in memory. | Constructor, `GetAll` |
| `SearchRequest` | `backend/ListingSearch.Api/DTOs/SearchRequest.cs` | Binds the supported query filters and paging values. | — |
| `SearchResponse` | `backend/ListingSearch.Api/DTOs/SearchResponse.cs` | Carries the current page and result-count metadata. | — |
| `ListingSearchResult` | `backend/ListingSearch.Api/DTOs/SearchResponse.cs` | Associates a source listing with its relevance score. | — |
| `SearchRequestValidator` | `backend/ListingSearch.Api/Validation/SearchRequestValidator.cs` | Produces field-specific errors for invalid search input. | `ValidateSearchRequest` |
| `RelevanceScoringService` | `backend/ListingSearch.Api/Services/RelevanceScoringService.cs` | Scores target-budget proximity and 90-day recency. | `CalculateRelevanceScore`, `CalculateBudgetProximityScore`, `CalculateRecencyScore` |
| `ListingSearchService` | `backend/ListingSearch.Api/Services/ListingSearchService.cs` | Filters, scores, sorts, and paginates source records. | `SearchListings`, `MatchesFilters` |
| `ListingsController` | `backend/ListingSearch.Api/Controllers/ListingsController.cs` | Exposes the HTTP search endpoint and validation responses. | `Search` |
| `SearchFiltersForm` | `frontend/src/components/SearchFiltersForm.tsx` | Collects filter input, validates basic combinations, and resets criteria. | `updateFilter`, `submitSearch`, `resetFilters` |
| `ListingsResults` | `frontend/src/components/ListingsResults.tsx` | Displays loading, error, empty, and populated result states. | `ListingsResults` |
| `ListingCard` | `frontend/src/components/ListingCard.tsx` | Displays one listing and formats its price and score. | `ListingCard`, `formatPrice` |
| `PaginationControls` | `frontend/src/components/PaginationControls.tsx` | Displays result totals and bounded previous/next controls. | `PaginationControls` |
| `listingApi` | `frontend/src/services/listingApi.ts` | Builds the query URL and handles HTTP responses. | `searchListings` |
| `App` | `frontend/src/App.tsx` | Coordinates filter submission, API requests, result state, and page changes. | `handleSearch`, `handleReset`, `loadListings` |

## Git workflow

Development is on `feature/listing-search-service`. Keep the approved assessment PDFs local; the root `.gitignore` excludes those exact files while tracking the supplied JSON. Run tests and review `git status` and `git diff` before committing. Do not merge into `main` or push to a remote without explicit approval.

## Known limitations

- The data source is the supplied static JSON file and is intentionally held in memory.
- City matching is exact apart from case and surrounding whitespace; source inconsistencies are not otherwise normalized.
- Description keywords use substring matching; there is no full-text index, fuzzy match, deduplication, or status filter.
- The frontend uses only the local-development CORS origin; production deployment configuration is outside this assessment.
