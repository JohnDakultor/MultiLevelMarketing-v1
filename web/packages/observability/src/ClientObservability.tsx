"use client";

import { useEffect, useMemo, type ReactNode } from "react";
import { createBrowserTelemetrySink, observeBrowserPerformance } from "./index";

export function ClientObservability({
  application,
  endpoint,
  release,
  children,
}: {
  application: string;
  endpoint?: string;
  release?: string;
  children: ReactNode;
}) {
  const sink = useMemo(
    () => createBrowserTelemetrySink({ application, endpoint, release }),
    [application, endpoint, release],
  );

  useEffect(() => {
    const routeError = (event: Event) => {
      const detail = (event as CustomEvent<{ name?: string }>).detail;
      sink.record({
        name: "route.error",
        outcome: "failure",
        attributes: { errorType: detail?.name ?? "Error" },
      });
    };
    const unhandledRejection = () =>
      sink.record({ name: "browser.unhandled_rejection", outcome: "failure" });

    window.addEventListener("modular-mlm:route-error", routeError);
    window.addEventListener("unhandledrejection", unhandledRejection);

    const navigation = performance.getEntriesByType("navigation")[0];
    if (navigation)
      sink.record({
        name: "route.load",
        durationMilliseconds: navigation.duration,
        outcome: "success",
      });

    const stopPerformanceObservation = observeBrowserPerformance(sink);

    return () => {
      stopPerformanceObservation();
      window.removeEventListener("modular-mlm:route-error", routeError);
      window.removeEventListener("unhandledrejection", unhandledRejection);
    };
  }, [sink]);

  return children;
}
