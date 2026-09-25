import type { ReactNode } from "react";

export interface BreadcrumbItem {
  label: string;
  href?: string;
}

export function Breadcrumb({ items }: { items: readonly BreadcrumbItem[] }) {
  return (
    <nav aria-label="Breadcrumb">
      <ol className="ds-breadcrumb">
        {items.map((item, index) => (
          <li key={`${item.label}-${index}`}>
            {item.href && index < items.length - 1 ? (
              <a href={item.href}>{item.label}</a>
            ) : (
              <span
                aria-current={index === items.length - 1 ? "page" : undefined}
              >
                {item.label}
              </span>
            )}
          </li>
        ))}
      </ol>
    </nav>
  );
}

export function Pagination({
  page,
  totalPages,
  onPageChange,
}: {
  page: number;
  totalPages: number;
  onPageChange(page: number): void;
}) {
  return (
    <nav className="ds-pagination" aria-label="Pagination">
      <button
        type="button"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        Previous
      </button>
      <span aria-live="polite">
        Page {page} of {Math.max(totalPages, 1)}
      </span>
      <button
        type="button"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      >
        Next
      </button>
    </nav>
  );
}

export interface TabItem {
  id: string;
  label: string;
  content: ReactNode;
}

export function Tabs({
  items,
  activeId,
  onChange,
}: {
  items: readonly TabItem[];
  activeId: string;
  onChange(id: string): void;
}) {
  const active = items.find((item) => item.id === activeId) ?? items[0];
  return (
    <div>
      <div className="ds-tabs" role="tablist">
        {items.map((item) => (
          <button
            key={item.id}
            id={`${item.id}-tab`}
            type="button"
            role="tab"
            aria-selected={item.id === active?.id}
            aria-controls={`${item.id}-panel`}
            onClick={() => onChange(item.id)}
          >
            {item.label}
          </button>
        ))}
      </div>
      {active && (
        <div
          id={`${active.id}-panel`}
          role="tabpanel"
          aria-labelledby={`${active.id}-tab`}
        >
          {active.content}
        </div>
      )}
    </div>
  );
}
