import type { SearchResponse } from "../types/ListingTypes";
import { ListingCard } from "./ListingCard";

interface ListingsResultsProps {
  response: SearchResponse | null;
  isLoading: boolean;
  error: string | null;
}

/** Presents loading, API error, empty, and populated search-result states. */
export function ListingsResults({
  response,
  isLoading,
  error,
}: ListingsResultsProps) {
  if (isLoading) {
    return (
      <p className="status-message" role="status">
        Searching listings…
      </p>
    );
  }

  if (error) {
    return (
      <p className="error-message" role="alert">
        {error}
      </p>
    );
  }

  if (!response || response.items.length === 0) {
    return (
      <p className="status-message" role="status">
        No listings match these filters.
      </p>
    );
  }

  return (
    <section aria-label="Search results" className="results-list">
      {response.items.map((result) => (
        <ListingCard
          key={`${result.listing.source}:${result.listing.id}`}
          result={result}
        />
      ))}
    </section>
  );
}
