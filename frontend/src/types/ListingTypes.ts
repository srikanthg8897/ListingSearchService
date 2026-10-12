/** Describes one property listing returned by the search API. */
export interface Listing {
  id: string;
  source: string;
  address: string;
  city: string;
  state: string;
  zip: string;
  price: number;
  bedrooms: number;
  bathrooms: number;
  sqft: number;
  latitude: number;
  longitude: number;
  listedDate: string;
  status: string;
  description: string;
}

/** Combines the unchanged source listing with its calculated relevance score. */
export interface ListingSearchResult {
  listing: Listing;
  relevanceScore: number;
}

/** Describes the results and pagination metadata returned by the API. */
export interface SearchResponse {
  items: ListingSearchResult[];
  page: number;
  pageSize: number;
  totalResults: number;
  totalPages: number;
}

/** Stores the editable search filter values as strings suitable for form controls. */
export interface SearchFilters {
  minPrice: string;
  maxPrice: string;
  minBedrooms: string;
  city: string;
  keyword: string;
  targetBudget: string;
  pageSize: string;
}

/** Provides the form's initial values, including the approved default page size. */
export const DEFAULT_SEARCH_FILTERS: SearchFilters = {
  minPrice: "",
  maxPrice: "",
  minBedrooms: "",
  city: "",
  keyword: "",
  targetBudget: "",
  pageSize: "10",
};
