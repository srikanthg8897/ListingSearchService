import { useState, type FormEvent } from "react";
import {
  DEFAULT_SEARCH_FILTERS,
  type SearchFilters,
} from "../types/ListingTypes";

interface SearchFiltersFormProps {
  onSearch: (filters: SearchFilters) => void;
  onReset: () => void;
}

/** Collects listing filters and provides immediate feedback for invalid combinations. */
export function SearchFiltersForm({
  onSearch,
  onReset,
}: SearchFiltersFormProps) {
  const [filters, setFilters] = useState<SearchFilters>({
    ...DEFAULT_SEARCH_FILTERS,
  });
  const [error, setError] = useState("");

  /** Updates one input while preserving the other form values. */
  function updateFilter(name: keyof SearchFilters, value: string) {
    setFilters((current) => ({ ...current, [name]: value }));
    setError("");
  }

  /** Validates cross-field constraints before submitting filters to the API. */
  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const minPrice = filters.minPrice ? Number(filters.minPrice) : null;
    const maxPrice = filters.maxPrice ? Number(filters.maxPrice) : null;
    if (minPrice !== null && maxPrice !== null && minPrice > maxPrice) {
      setError("Maximum price must be greater than or equal to minimum price.");
      return;
    }

    if (filters.targetBudget && Number(filters.targetBudget) <= 0) {
      setError("Target budget must be greater than zero.");
      return;
    }

    if (
      !filters.pageSize ||
      Number(filters.pageSize) < 1 ||
      Number(filters.pageSize) > 100
    ) {
      setError("Page size must be between 1 and 100.");
      return;
    }

    setError("");
    onSearch({ ...filters });
  }

  /** Clears the form and restores the approved default page size. */
  function resetFilters() {
    setFilters({ ...DEFAULT_SEARCH_FILTERS });
    setError("");
    onReset();
  }

  return (
    <form className="search-form" onSubmit={submitSearch}>
      <div className="filter-grid">
        <label>
          Minimum price
          <input
            type="number"
            min="0"
            step="any"
            value={filters.minPrice}
            onChange={(event) => updateFilter("minPrice", event.target.value)}
          />
        </label>
        <label>
          Maximum price
          <input
            type="number"
            min="0"
            step="any"
            value={filters.maxPrice}
            onChange={(event) => updateFilter("maxPrice", event.target.value)}
          />
        </label>
        <label>
          Minimum bedrooms
          <input
            type="number"
            min="0"
            step="1"
            value={filters.minBedrooms}
            onChange={(event) =>
              updateFilter("minBedrooms", event.target.value)
            }
          />
        </label>
        <label>
          City
          <input
            type="text"
            value={filters.city}
            onChange={(event) => updateFilter("city", event.target.value)}
          />
        </label>
        <label>
          Description keyword
          <input
            type="text"
            value={filters.keyword}
            onChange={(event) => updateFilter("keyword", event.target.value)}
          />
        </label>
        <label>
          Target budget
          <input
            type="number"
            min="0"
            step="any"
            value={filters.targetBudget}
            onChange={(event) =>
              updateFilter("targetBudget", event.target.value)
            }
          />
        </label>
        <label>
          Page size
          <input
            type="number"
            min="1"
            max="100"
            step="1"
            required
            value={filters.pageSize}
            onChange={(event) => updateFilter("pageSize", event.target.value)}
          />
        </label>
      </div>

      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}

      <div className="form-actions">
        <button type="submit">Search</button>
        <button className="secondary-button" type="button" onClick={resetFilters}>
          Reset
        </button>
      </div>
    </form>
  );
}
