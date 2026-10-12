import { useEffect, useRef, useState } from "react";
import { ListingsResults } from "./components/ListingsResults";
import { PaginationControls } from "./components/PaginationControls";
import { SearchFiltersForm } from "./components/SearchFiltersForm";
import { searchListings } from "./services/listingApi";
import {
  DEFAULT_SEARCH_FILTERS,
  type SearchFilters,
  type SearchResponse,
} from "./types/ListingTypes";

/** Connects the filter form, paginated API requests, and listing result states. */
export default function App() {
  const [filters, setFilters] = useState<SearchFilters>({
    ...DEFAULT_SEARCH_FILTERS,
  });
  const [page, setPage] = useState(1);
  const [response, setResponse] = useState<SearchResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const requestVersion = useRef(0);

  useEffect(() => {
    const controller = new AbortController();
    const currentRequest = ++requestVersion.current;
    setIsLoading(true);
    setError(null);

    /** Ignores cancelled requests so stale responses cannot replace current results. */
    async function loadListings() {
      try {
        const result = await searchListings(filters, page, controller.signal);
        if (currentRequest === requestVersion.current) {
          setResponse(result);
        }
      } catch (requestError) {
        if (
          controller.signal.aborted ||
          currentRequest !== requestVersion.current
        ) {
          return;
        }
        setError(
          requestError instanceof Error
            ? requestError.message
            : "An unexpected error occurred while searching listings.",
        );
      } finally {
        if (currentRequest === requestVersion.current) {
          setIsLoading(false);
        }
      }
    }

    void loadListings();
    return () => controller.abort();
  }, [filters, page]);

  /** Applies new filters and restarts navigation from the first result page. */
  function handleSearch(nextFilters: SearchFilters) {
    setFilters(nextFilters);
    setPage(1);
  }

  /** Clears active criteria and restores the first result page. */
  function handleReset() {
    setFilters({ ...DEFAULT_SEARCH_FILTERS });
    setPage(1);
  }

  return (
    <main className="page-shell">
      <header className="page-header">
        <p className="eyebrow">Property discovery</p>
        <h1>Listing Search</h1>
        <p>Filter property listings and sort them by budget fit and recency.</p>
      </header>

      <section aria-labelledby="filters-heading" className="panel">
        <h2 id="filters-heading">Search filters</h2>
        <SearchFiltersForm onSearch={handleSearch} onReset={handleReset} />
      </section>

      <section aria-labelledby="results-heading" className="results-section">
        <h2 id="results-heading">Listings</h2>
        {response && !isLoading && !error && (
          <PaginationControls
            page={response.page}
            totalPages={response.totalPages}
            totalResults={response.totalResults}
            onPageChange={setPage}
          />
        )}
        <ListingsResults
          response={response}
          isLoading={isLoading}
          error={error}
        />
        {response && !isLoading && !error && response.totalPages > 1 && (
          <PaginationControls
            page={response.page}
            totalPages={response.totalPages}
            totalResults={response.totalResults}
            onPageChange={setPage}
          />
        )}
      </section>
    </main>
  );
}
