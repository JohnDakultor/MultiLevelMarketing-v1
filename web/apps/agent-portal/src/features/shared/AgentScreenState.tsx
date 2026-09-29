import { ApiError, isRetrySafe } from "@modular-mlm/api-client";
import {
  Button,
  ErrorState,
  formatDate,
  formatMoney,
  PermissionDeniedState,
  Skeleton,
} from "@modular-mlm/design-system";

export function Loading() {
  return (
    <div className="content-stack" aria-busy="true">
      <span className="ds-sr-only" role="status">
        Loading Agent workspace…
      </span>
      <div className="agent-page-skeleton">
        <Skeleton width="35%" height="1rem" />
        <Skeleton width="65%" height="2.25rem" />
        <Skeleton width="80%" height="1rem" />
      </div>
      <div className="metric-grid">
        <Skeleton height="7.5rem" />
        <Skeleton height="7.5rem" />
        <Skeleton height="7.5rem" />
        <Skeleton height="7.5rem" />
      </div>
      <Skeleton height="18rem" />
    </div>
  );
}

export function Failure({ error, retry }: { error: Error; retry(): void }) {
  if (error instanceof ApiError && error.isForbidden)
    return <PermissionDeniedState />;
  return (
    <ErrorState
      description={error.message}
      action={
        isRetrySafe(error) ? (
          <Button onClick={retry}>Try again</Button>
        ) : undefined
      }
    />
  );
}

export const money = (value: number, currency: string) =>
  formatMoney(value, currency);
export const date = (value: string | null) => formatDate(value);
