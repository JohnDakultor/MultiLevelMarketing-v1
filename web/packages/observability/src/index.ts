export interface ClientTelemetryEvent {
  name: string;
  durationMilliseconds?: number;
  outcome?: "success" | "failure";
  attributes?: Readonly<Record<string, string | number | boolean>>;
}

export interface ClientTelemetrySink {
  record(event: ClientTelemetryEvent): void;
}

export const noOpTelemetrySink: ClientTelemetrySink = {
  record: () => undefined,
};

export async function measure<T>(
  sink: ClientTelemetrySink,
  name: string,
  operation: () => Promise<T>,
  attributes?: ClientTelemetryEvent["attributes"],
): Promise<T> {
  const startedAt = performance.now();
  try {
    const result = await operation();
    sink.record({
      name,
      durationMilliseconds: performance.now() - startedAt,
      outcome: "success",
      attributes,
    });
    return result;
  } catch (error) {
    sink.record({
      name,
      durationMilliseconds: performance.now() - startedAt,
      outcome: "failure",
      attributes,
    });
    throw error;
  }
}

export function newCorrelationId(): string {
  return crypto.randomUUID();
}

export function createBrowserTelemetrySink(options: {
  endpoint?: string;
  application: string;
  release?: string;
}): ClientTelemetrySink {
  return {
    record(event) {
      if (!options.endpoint || typeof navigator === "undefined") return;
      const payload = JSON.stringify({
        ...sanitizeTelemetryEvent(event),
        application: options.application,
        release: options.release ?? "unknown",
        correlationId: newCorrelationId(),
        route: sanitizedRoute(),
        occurredAt: new Date().toISOString(),
      });
      navigator.sendBeacon(
        options.endpoint,
        new Blob([payload], { type: "application/json" }),
      );
    },
  };
}

export function sanitizedRoute(): string {
  if (typeof window === "undefined") return "server";
  return window.location.pathname.replace(
    /[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}/gi,
    ":id",
  );
}

const sensitiveAttributeName =
  /token|password|secret|authorization|cookie|email|phone|address|payment|card/i;

/** Removes accidental personal data and credentials before browser telemetry leaves the page. */
export function sanitizeTelemetryEvent(
  event: ClientTelemetryEvent,
): ClientTelemetryEvent {
  if (!event.attributes) return event;
  return {
    ...event,
    attributes: Object.fromEntries(
      Object.entries(event.attributes).filter(
        ([name]) => !sensitiveAttributeName.test(name),
      ),
    ),
  };
}

export function observeBrowserPerformance(
  sink: ClientTelemetrySink,
): () => void {
  if (typeof PerformanceObserver === "undefined") return () => undefined;

  const observers: PerformanceObserver[] = [];
  let cumulativeLayoutShift = 0;
  let largestContentfulPaint = 0;
  let interactionToNextPaint = 0;
  let reported = false;

  observe("paint", (entry) => {
    if (entry.name === "first-contentful-paint")
      sink.record({
        name: "web_vital.fcp",
        durationMilliseconds: entry.startTime,
        outcome: "success",
      });
  });
  observe("largest-contentful-paint", (entry) => {
    largestContentfulPaint = Math.max(largestContentfulPaint, entry.startTime);
  });
  observe("layout-shift", (entry) => {
    const shift = entry as PerformanceEntry & {
      value?: number;
      hadRecentInput?: boolean;
    };
    if (!shift.hadRecentInput) cumulativeLayoutShift += shift.value ?? 0;
  });
  observe("event", (entry) => {
    interactionToNextPaint = Math.max(interactionToNextPaint, entry.duration);
  });

  const report = () => {
    if (document.visibilityState !== "hidden" || reported) return;
    reported = true;
    if (largestContentfulPaint > 0)
      sink.record({
        name: "web_vital.lcp",
        durationMilliseconds: largestContentfulPaint,
        outcome: "success",
      });
    sink.record({
      name: "web_vital.cls",
      durationMilliseconds: cumulativeLayoutShift,
      outcome: "success",
    });
    if (interactionToNextPaint > 0)
      sink.record({
        name: "web_vital.inp",
        durationMilliseconds: interactionToNextPaint,
        outcome: "success",
      });
  };
  document.addEventListener("visibilitychange", report);

  return () => {
    document.removeEventListener("visibilitychange", report);
    for (const observer of observers) observer.disconnect();
  };

  function observe(
    type: string,
    consume: (entry: PerformanceEntry) => void,
  ): void {
    try {
      const observer = new PerformanceObserver((list) => {
        for (const entry of list.getEntries()) consume(entry);
      });
      observer.observe({ type, buffered: true });
      observers.push(observer);
    } catch {
      // Browsers may not support every entry type; remaining metrics still report.
    }
  }
}
