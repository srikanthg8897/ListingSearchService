interface PaginationControlsProps {
  page: number;
  totalPages: number;
  totalResults: number;
  onPageChange: (page: number) => void;
}

/** Shows result totals and keeps previous/next navigation within valid pages. */
export function PaginationControls({
  page,
  totalPages,
  totalResults,
  onPageChange,
}: PaginationControlsProps) {
  return (
    <nav aria-label="Search result pages" className="pagination">
      <p>
        Page {page} of {totalPages} · {totalResults}{" "}
        {totalResults === 1 ? "result" : "results"}
      </p>
      <div>
        <button
          type="button"
          className="secondary-button"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          Previous
        </button>
        <button
          type="button"
          className="secondary-button"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          Next
        </button>
      </div>
    </nav>
  );
}
