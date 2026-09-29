import { ApiError, isRetrySafe } from "@modular-mlm/api-client";
import {
  Button,
  ErrorState,
  formatDateTime,
  formatMoney,
  PermissionDeniedState,
  Skeleton,
} from "@modular-mlm/design-system";
import { useOrganization } from "@modular-mlm/organization-context";

export function useAdminScope() {
  const { organization } = useOrganization();
  return {
    organizationId: organization?.id ?? "",
    currency: organization?.currencyCode ?? "PHP",
    isReady: Boolean(organization),
  };
}

export function Loading() {
  return (
    <div className="content-stack admin-loading" aria-busy="true">
      <span className="ds-sr-only" role="status">
        Loading administration data…
      </span>
      <div className="admin-loading__heading">
        <Skeleton height="1.9rem" />
        <Skeleton height="1rem" />
      </div>
      <div className="metric-grid">
        <Skeleton height="7rem" />
        <Skeleton height="7rem" />
        <Skeleton height="7rem" />
        <Skeleton height="7rem" />
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
export const date = (value: string | null) => formatDateTime(value);
