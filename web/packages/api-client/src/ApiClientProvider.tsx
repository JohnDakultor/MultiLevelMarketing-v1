"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import { ApiClient } from "./ApiClient";

const ApiClientContext = createContext<ApiClient | null>(null);

export function ApiClientProvider({
  client,
  children,
}: {
  client: ApiClient;
  children: ReactNode;
}) {
  return (
    <ApiClientContext.Provider value={client}>
      {children}
    </ApiClientContext.Provider>
  );
}

export function useApiClient(): ApiClient {
  const client = useContext(ApiClientContext);
  if (!client)
    throw new Error("useApiClient must be used inside ApiClientProvider.");
  return client;
}

export interface ApiQueryState<T> {
  data: T | null;
  error: Error | null;
  isLoading: boolean;
  reload(): void;
}

export function useApiQuery<T>(
  load: (client: ApiClient, signal: AbortSignal) => Promise<T>,
  dependencies: readonly unknown[],
  enabled = true,
  initialData?: T,
): ApiQueryState<T> {
  const client = useApiClient();
  const [data, setData] = useState<T | null>(initialData ?? null);
  const [error, setError] = useState<Error | null>(null);
  const [isLoading, setLoading] = useState(
    enabled && initialData === undefined,
  );
  const [revision, setRevision] = useState(0);
  const reload = useCallback(() => {
    setLoading(true);
    setError(null);
    setRevision((value) => value + 1);
  }, []);

  useEffect(() => {
    if (!enabled) return;

    const controller = new AbortController();
    void load(client, controller.signal)
      .then((result) => {
        if (!controller.signal.aborted) setData(result);
      })
      .catch((reason: unknown) => {
        if (!controller.signal.aborted)
          setError(
            reason instanceof Error ? reason : new Error("Request failed."),
          );
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => controller.abort();
    // Callers provide the precise cache identity for their request.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [client, enabled, revision, ...dependencies]);

  return { data, error, isLoading: enabled && isLoading, reload };
}
