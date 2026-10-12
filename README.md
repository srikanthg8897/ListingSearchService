# Listing Search Service

A small, full-stack property search app. A React page sends search filters to an ASP.NET Core API. The API reads the supplied JSON listings, filters them, ranks the matches, and returns one page of results.

## Run it locally

You need the **.NET 9 SDK** and **Node.js 18 or newer with npm**. Start the API and UI in two separate PowerShell terminals, from the repository root.

**Terminal 1 — start the API:**

```powershell
dotnet run --project backend\ListingSearch.Api -- --urls http://localhost:5000
```

Wait for the message `Now listening on: http://localhost:5000`.

**Terminal 2 — configure and start the UI:**

```powershell
Copy-Item frontend\.env.example frontend\.env.local
Set-Location frontend
npm install
npm run dev
```

Open **http://localhost:8080**. Keep both terminals running while using the app.

The `.env.local` file configures the UI to call `http://localhost:5000`. It is ignored by Git. The API permits the local UI origins `http://localhost:8080` and `http://localhost:5173`. Vite is configured to use port 8080; if that port is already occupied, stop the other process using it and restart Vite.

The API loads `requirements\sample_listings.json` at startup. If the file is missing or invalid, startup fails with an error instead of silently returning incomplete results. The supplied data contains 12 listings; it is tracked in Git and must not be modified to make tests pass. The assessment PDFs are kept local and ignored by Git.

## What happens when you search

1. Enter any combination of price, bedroom, city, description-keyword, and target-budget filters. The page size defaults to 10.
2. Select **Search**. The browser sends a `GET /api/listings/search` request to the API. Changing filters starts again from page 1.
3. The API validates the inputs, filters the full dataset, scores and sorts all matching listings, then selects the requested page.
4. The UI displays each result's address, price, bedroom count, and relevance score, along with the result totals and page controls.
5. Use **Reset** to clear filters. If no listings match, the UI displays an empty-results message.

The order matters: pagination happens **after** filtering and ranking. The UI cancels obsolete requests so a late response from an earlier search cannot replace the current results.

## Search rules

| Query parameter | Purpose | Default and validation |
|---|---|---|
| `minPrice` | Minimum list price, inclusive | Optional; must not be negative |
| `maxPrice` | Maximum list price, inclusive | Optional; must not be negative or below `minPrice` |
| `minBedrooms` | Minimum bedroom count, inclusive | Optional; must not be negative |
| `city` | Exact city match | Optional; ignores letter case and surrounding whitespace |
| `keyword` | Substring search in the listing description | Optional; ignores letter case and surrounding query whitespace |
| `targetBudget` | Budget input to relevance ranking | Optional; must be greater than zero when provided |
| `page` | One-based page number | Defaults to `1`; must be at least `1` |
| `pageSize` | Results per page | Defaults to `10`; must be from `1` to `100` |

Matching does not otherwise normalize source data. Addresses and city names are not rewritten. Listings are not deduplicated, even when separate feeds appear to describe the same property. Listing status is preserved but is not a search filter.

## API example

Request one result page of Springfield listings ranked against a $500,000 target budget:

```text
GET http://localhost:5000/api/listings/search?city=Springfield&targetBudget=500000&page=1&pageSize=2
```

The response contains the original listing plus its calculated score, and pagination metadata:

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

This example's score is for the sample listing when the UTC date is 2026-10-12. The score changes as the listing ages. The `items` collection contains up to `pageSize` results.

### Errors and empty results

- Invalid values, including malformed numeric query parameters, return **HTTP 400** with field-specific validation details.
- A valid search with no matches returns **HTTP 200**, an empty `items` array, `totalResults: 0`, and `totalPages: 0`.
- Requesting a page beyond the last page returns **HTTP 200** with an empty `items` array and the actual totals.

## How relevance is calculated

Every score is in the range **0–100**. The API uses the listing's price, its `listedDate`, the optional target budget, and today's UTC date.

**When a target budget is provided:**

1. Measure price difference as a percentage of the target budget: `budgetScore = max(0, 100 * (1 - abs(price - targetBudget) / targetBudget))`.
2. Give listings a linear recency score over 90 days: `ageDays = max(0, UTC-today - listedDate)` and `recencyScore = max(0, 100 * (1 - ageDays / 90))`.
3. Combine the two: `relevanceScore = 0.70 * budgetScore + 0.30 * recencyScore`.

A listing at the target price receives a budget score of 100; one at least 100% above or below the target receives 0. A listing from today receives a recency score of 100; one at least 90 days old receives 0. Future dates are treated as today.

**When no target budget is provided**, the listing's relevance score is its full recency score. The API does not reduce this score to the 30% weight.

`TimeProvider` supplies the current UTC date and can be replaced with a fixed clock in tests. Results are sorted by relevance descending, then listing date descending, then source ascending, then ID ascending. String tie-breakers use ordinal comparison. This simple score intentionally does not rank by location, status, or keyword relevance.

## Project map

```text
backend/
  ListingSearch.sln
  ListingSearch.Api/       ASP.NET Core API
  ListingSearch.Tests/     xUnit unit and HTTP integration tests
frontend/                  React, TypeScript, and Vite app
requirements/
  sample_listings.json     Original 12-record dataset
```

### Backend responsibilities

| Class | File | Responsibility |
|---|---|---|
| `Listing` | `backend/ListingSearch.Api/Models/Listing.cs` | Represents one source listing and parses its ISO date for comparisons. |
| `IListingRepository` | `backend/ListingSearch.Api/Repositories/IListingRepository.cs` | Defines read-only access to listings. |
| `ListingRepository` | `backend/ListingSearch.Api/Repositories/ListingRepository.cs` | Loads and validates the JSON dataset. |
| `SearchRequest` | `backend/ListingSearch.Api/DTOs/SearchRequest.cs` | Holds filters and pagination values from the query. |
| `SearchResponse`, `ListingSearchResult` | `backend/ListingSearch.Api/DTOs/SearchResponse.cs` | Describe the scored result page and its metadata. |
| `SearchRequestValidator` | `backend/ListingSearch.Api/Validation/SearchRequestValidator.cs` | Reports invalid search and pagination values. |
| `RelevanceScoringService` | `backend/ListingSearch.Api/Services/RelevanceScoringService.cs` | Calculates budget-fit and recency scores. |
| `ListingSearchService` | `backend/ListingSearch.Api/Services/ListingSearchService.cs` | Applies filters, scoring, stable sorting, and pagination. |
| `ListingsController` | `backend/ListingSearch.Api/Controllers/ListingsController.cs` | Exposes `GET /api/listings/search` and HTTP responses. |

### Frontend responsibilities

| Component or service | File | Responsibility |
|---|---|---|
| `App` | `frontend/src/App.tsx` | Coordinates filter submission, API requests, and page state. |
| `SearchFiltersForm` | `frontend/src/components/SearchFiltersForm.tsx` | Collects filter values and provides validation feedback. |
| `ListingsResults` | `frontend/src/components/ListingsResults.tsx` | Displays loading, error, empty, or populated results. |
| `ListingCard` | `frontend/src/components/ListingCard.tsx` | Displays a listing and formats its price and score. |
| `PaginationControls` | `frontend/src/components/PaginationControls.tsx` | Displays totals and previous/next page controls. |
| `listingApi` | `frontend/src/services/listingApi.ts` | Builds the API request and reports HTTP errors. |
| `ListingTypes` | `frontend/src/types/ListingTypes.ts` | Defines shared listing, search, and response types. |

Important backend methods include `ValidateSearchRequest`, `SearchListings`, `MatchesFilters`, `CalculateRelevanceScore`, `CalculateBudgetProximityScore`, and `CalculateRecencyScore`. Frontend helpers `submitSearch`, `handleSearch`, and `loadListings` validate form input, reset pagination when filters change, and ignore stale API responses.

## Build and test

From the repository root, build the API and run its unit and HTTP integration tests:

```powershell
dotnet test backend\ListingSearch.sln
```

From the `frontend` directory, check TypeScript types and create a production build:

```powershell
npm run typecheck
npm run build
```

The tests cover filtering, combined filters, empty results, validation, budget/recency ranking, ties, stable pagination, JSON loading, and API behavior. They use a fixed UTC date so scoring expectations are repeatable.

## Design choices and limitations

- Storage is an in-memory collection loaded from the provided static JSON file; there is no database.
- City matching is exact except for letter case and surrounding whitespace. Keyword matching is a case-insensitive description substring.
- Duplicate source records are retained. There is no deduplication, address normalization, fuzzy matching, full-text index, or status filter.
- CORS is configured for local UI development origins; production deployment settings are outside this project.
- The scorer and search service have separate responsibilities so ranking and search behavior can be extended and tested independently.

The assessment PDFs are intentionally kept out of Git. The sample JSON and application source are tracked. Development is on `feature/listing-search-service`; do not merge into `main` or push without approval.
