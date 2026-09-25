import { ApiError, type ApiClient } from "@modular-mlm/api-client";
import type {
  CurrentUserDto,
  AuthenticationSessionDto,
  Guid,
  RevokeCurrentSessionRequest,
} from "@modular-mlm/contracts";

export interface SignInCredentials {
  email: string;
  password: string;
}

export class AuthenticationService {
  constructor(private readonly api: ApiClient) {}

  async getCurrentUser(signal?: AbortSignal): Promise<CurrentUserDto | null> {
    const currentUser = await this.api.request<CurrentUserDto | undefined>(
      "/api/me",
      { anonymous: true, signal },
    );
    return currentUser ?? null;
  }

  async signIn(
    credentials: SignInCredentials,
    signal?: AbortSignal,
  ): Promise<void> {
    // Antiforgery tokens are identity-bound. Never carry a token from a prior
    // signed-out (or different) user into the newly authenticated session.
    this.api.clearAntiforgeryToken();
    try {
      await this.api.request<void>(
        "/api/Users/login?useCookies=true&useSessionCookies=true",
        {
          method: "POST",
          anonymous: true,
          skipAntiforgery: true,
          body: credentials,
          signal,
        },
      );
    } finally {
      this.api.clearAntiforgeryToken();
    }
  }

  async signOut(signal?: AbortSignal): Promise<void> {
    try {
      await this.api.request<void>("/api/Users/logout", {
        method: "POST",
        body: {},
        signal,
      });
    } catch (error) {
      // Logout is idempotent from the user's perspective. A 401 means the
      // server-side session is already gone, so the local signed-out state is valid.
      if (!(error instanceof ApiError && error.isUnauthorized)) throw error;
    } finally {
      this.api.clearAntiforgeryToken();
    }
  }

  revokeCurrentSession(sessionId: Guid, signal?: AbortSignal): Promise<void> {
    const request: RevokeCurrentSessionRequest = { sessionId };
    return this.api.request<void>("/api/me/session/revoke", {
      method: "POST",
      body: request,
      signal,
    });
  }

  getSessions(signal?: AbortSignal): Promise<AuthenticationSessionDto[]> {
    return this.api.request<AuthenticationSessionDto[]>("/api/me/sessions", {
      signal,
    });
  }
}
