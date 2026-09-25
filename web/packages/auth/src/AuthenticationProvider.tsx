"use client";

import { ApiError } from "@modular-mlm/api-client";
import type {
  AuthenticationSessionDto,
  CurrentUserDto,
} from "@modular-mlm/contracts";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import type { AuthenticationService } from "./AuthenticationService";

export const authenticationUnauthorizedEvent = "modular-mlm:unauthorized";

export interface AuthenticationState {
  user: CurrentUserDto | null;
  isLoading: boolean;
  error: Error | null;
  isAuthenticated: boolean;
  signIn(email: string, password: string): Promise<void>;
  signOut(): Promise<void>;
  refresh(): Promise<void>;
  listSessions(signal?: AbortSignal): Promise<AuthenticationSessionDto[]>;
  revokeSession(sessionId: string): Promise<void>;
  clear(): void;
}

const AuthenticationContext = createContext<AuthenticationState | null>(null);

export function AuthenticationProvider({
  service,
  children,
}: {
  service: AuthenticationService;
  children: ReactNode;
}) {
  const [user, setUser] = useState<CurrentUserDto | null>(null);
  const [isLoading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setUser(await service.getCurrentUser());
    } catch (requestError) {
      setUser(null);
      if (!isUnauthorizedError(requestError)) {
        setError(asError(requestError));
      }
    } finally {
      setLoading(false);
    }
  }, [service]);

  useEffect(() => {
    const controller = new AbortController();

    void service
      .getCurrentUser(controller.signal)
      .then((currentUser) => setUser(currentUser))
      .catch((requestError: unknown) => {
        if (controller.signal.aborted) return;
        setUser(null);
        if (!isUnauthorizedError(requestError)) {
          setError(asError(requestError));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => controller.abort();
  }, [service]);

  useEffect(() => {
    const clearUnauthorizedSession = () => {
      setUser(null);
      setError(null);
      setLoading(false);
    };
    window.addEventListener(
      authenticationUnauthorizedEvent,
      clearUnauthorizedSession,
    );
    return () =>
      window.removeEventListener(
        authenticationUnauthorizedEvent,
        clearUnauthorizedSession,
      );
  }, []);

  const value = useMemo<AuthenticationState>(
    () => ({
      user,
      isLoading,
      error,
      isAuthenticated: user !== null,
      async signIn(email, password) {
        await service.signIn({ email, password });
        await refresh();
      },
      async signOut() {
        try {
          await service.signOut();
        } finally {
          setUser(null);
          setError(null);
        }
      },
      refresh,
      listSessions: (signal) => service.getSessions(signal),
      async revokeSession(sessionId) {
        await service.revokeCurrentSession(sessionId);
      },
      clear: () => {
        setUser(null);
        setError(null);
      },
    }),
    [error, isLoading, refresh, service, user],
  );

  return (
    <AuthenticationContext.Provider value={value}>
      {children}
    </AuthenticationContext.Provider>
  );
}

export function useAuthentication(): AuthenticationState {
  const value = useContext(AuthenticationContext);
  if (!value)
    throw new Error(
      "useAuthentication must be used inside AuthenticationProvider.",
    );
  return value;
}

function asError(value: unknown): Error {
  return value instanceof Error
    ? value
    : new Error("Authentication could not be loaded.");
}

function isUnauthorizedError(value: unknown): boolean {
  if (value instanceof ApiError) return value.isUnauthorized;
  return (
    typeof value === "object" &&
    value !== null &&
    "status" in value &&
    value.status === 401
  );
}
