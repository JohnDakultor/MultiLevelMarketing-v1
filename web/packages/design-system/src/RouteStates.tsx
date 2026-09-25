"use client";

import { useEffect } from "react";
import { Button } from "./Button";
import { ErrorState, Skeleton } from "./Feedback";

export function RouteLoading({ label = "Loading page" }: { label?: string }) {
  return (
    <div className="content-stack" aria-busy="true">
      <span className="ds-sr-only" role="status">
        {label}…
      </span>
      <Skeleton height="4rem" />
      <Skeleton height="14rem" />
    </div>
  );
}

export function RouteError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset(): void;
}) {
  useEffect(() => {
    window.dispatchEvent(
      new CustomEvent("modular-mlm:route-error", {
        detail: { name: error.name, digest: error.digest },
      }),
    );
  }, [error]);

  return (
    <ErrorState
      title="This page could not be loaded"
      description="The application encountered an unexpected problem. No sensitive diagnostic details are displayed."
      action={<Button onClick={reset}>Try again</Button>}
    />
  );
}
