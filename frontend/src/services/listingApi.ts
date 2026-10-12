import type { SearchFilters, SearchResponse } from "../types/ListingTypes";

/** Requests one filtered page of listings and reports API or network failures. */
export async function searchListings(
  filters: SearchFilters,
  page: number,
  signal: AbortSignal,
): Promise<SearchResponse> {
  const configuredBaseUrl = import.meta.env.VITE_LISTING_API_BASE_URL?.trim();
  if (!configuredBaseUrl) {
    throw new Error(
      "Set VITE_LISTING_API_BASE_URL in frontend/.env.local before searching.",
    );
  }

  const query = new URLSearchParams();
  Object.entries(filters).forEach(([name, value]) => {
    const trimmedValue = value.trim();
    if (trimmedValue) {
      query.set(name, trimmedValue);
    }
  });
  query.set("page", String(page));

  const searchUrl = new URL("/api/listings/search", configuredBaseUrl);
  searchUrl.search = query.toString();

  const response = await fetch(searchUrl, { signal });
  if (!response.ok) {
    const body = await response.text();
    let problemMessage = "";
    try {
      const problem = JSON.parse(body) as {
        title?: string;
        detail?: string;
        errors?: Record<string, string[]>;
      };
      problemMessage =
        Object.values(problem.errors ?? {}).flat().join(" ") ||
        problem.detail ||
        problem.title ||
        "";
    } catch (error) {
      if (!(error instanceof SyntaxError)) {
        throw error;
      }
    }

    throw new Error(
      problemMessage ||
        body.slice(0, 200) ||
        `The listing API returned HTTP ${response.status}.`,
    );
  }

  return (await response.json()) as SearchResponse;
}
