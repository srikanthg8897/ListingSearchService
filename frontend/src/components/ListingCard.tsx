import type { ListingSearchResult } from "../types/ListingTypes";

interface ListingCardProps {
  result: ListingSearchResult;
}

/** Displays a property's address, asking price, bedrooms, and relevance score. */
export function ListingCard({ result }: ListingCardProps) {
  const { listing, relevanceScore } = result;

  return (
    <article className="listing-card">
      <div>
        <h3>{listing.address}</h3>
        <p className="listing-location">
          {listing.city}, {listing.state} {listing.zip}
        </p>
      </div>
      <dl className="listing-details">
        <div>
          <dt>Price</dt>
          <dd>{formatPrice(listing.price)}</dd>
        </div>
        <div>
          <dt>Bedrooms</dt>
          <dd>{listing.bedrooms}</dd>
        </div>
        <div>
          <dt>Relevance</dt>
          <dd>{relevanceScore.toFixed(2)} / 100</dd>
        </div>
      </dl>
      <p className="listing-description">{listing.description}</p>
    </article>
  );
}

/** Formats numeric USD prices consistently for listing cards. */
function formatPrice(price: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 0,
  }).format(price);
}
