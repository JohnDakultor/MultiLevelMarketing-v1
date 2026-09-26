import type {
  AntiforgeryTokenResponse,
  ProblemDetails,
} from "@modular-mlm/contracts";
import { ApiError, messageForStatus } from "./ApiError";

const safeMethods = new Set(["GET", "HEAD", "OPTIONS"]);

export interface ApiClientOptions {
  baseUrl?: string;
  timeoutMilliseconds?: number;
  authentication?: "cookie" | "bearer";
  getAccessToken?: () => string | null | Promise<string | null>;
  onUnauthorized?: () => void | Promise<void>;
  fetchImplementation?: typeof fetch;
  createCorrelationId?: () => string;
}

export interface ApiRequestOptions extends Omit<RequestInit, "body"> {
  body?: unknown;
  anonymous?: boolean;
  skipAntiforgery?: boolean;
  optionalAntiforgery?: boolean;
}

export class ApiClient {
  private readonly baseUrl: string;
  private readonly timeoutMilliseconds: number;
  private readonly authentication: "cookie" | "bearer";
  private readonly fetchImplementation: typeof fetch;
  private antiforgeryToken: AntiforgeryTokenResponse | null = null;

  constructor(private readonly options: ApiClientOptions = {}) {
    this.baseUrl = (options.baseUrl ?? "").replace(/\/$/, "");
    this.timeoutMilliseconds = options.timeoutMilliseconds ?? 15_000;
    this.authentication = options.authentication ?? "cookie";
    this.fetchImplementation =
      options.fetchImplementation ?? globalThis.fetch.bind(globalThis);
  }

  async request<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
    const method = (options.method ?? "GET").toUpperCase();
    const headers = new Headers(options.headers);
    const isFormData =
      typeof FormData !== "undefined" && options.body instanceof FormData;
    headers.set("Accept", "application/json");
    headers.set("Api-Version", "1.0");
    if (!headers.has("X-Correlation-ID"))
      headers.set(
        "X-Correlation-ID",
        this.options.createCorrelationId?.() ?? crypto.randomUUID(),
      );

    if (options.body !== undefined && !isFormData)
      headers.set("Content-Type", "application/json");
    await this.applyAuthentication(headers, options.anonymous === true);

    if (
      this.authentication === "cookie" &&
      (!options.anonymous || options.optionalAntiforgery === true) &&
      !options.skipAntiforgery &&
      !safeMethods.has(method)
    ) {
      try {
        const antiforgery = await this.getAntiforgeryToken(options.signal);
        headers.set(antiforgery.headerName, antiforgery.requestToken);
      } catch (error) {
        if (!(
          options.optionalAntiforgery &&
          error instanceof ApiError &&
          error.isUnauthorized
        ))
          throw error;
      }
    }

    const timeout = AbortSignal.timeout(this.timeoutMilliseconds);
    const signal = options.signal
      ? AbortSignal.any([options.signal, timeout])
      : timeout;
    const requestBody: BodyInit | undefined =
      options.body === undefined
        ? undefined
        : isFormData
          ? (options.body as FormData)
          : JSON.stringify(options.body);

    let response: Response;
    try {
      response = await this.fetchImplementation(`${this.baseUrl}${path}`, {
        ...options,
        method,
        headers,
        signal,
        credentials: "include",
        body: requestBody,
      });
    } catch (error) {
      if (error instanceof DOMException && error.name === "AbortError")
        throw error;
      throw new ApiError(
        0,
        {
          title: "Cannot reach the server",
          detail: "Check your connection and try again.",
        },
        { cause: error },
      );
    }

    if (response.status === 401 && !options.anonymous)
      await this.options.onUnauthorized?.();
    if (!response.ok)
      throw new ApiError(response.status, await parseProblem(response));

    if (
      response.status === 204 ||
      response.status === 205 ||
      method === "HEAD" ||
      response.headers.get("content-length") === "0"
    ) {
      return undefined as T;
    }

    // ASP.NET TypedResults.Ok() can return HTTP 200 with an empty body and no
    // Content-Length header (for example, the logout endpoint). Calling
    // Response.json() for that valid response throws "Unexpected end of JSON
    // input", so inspect the body before decoding it.
    const responseBody = await response.text();
    if (!responseBody.trim()) return undefined as T;

    return JSON.parse(responseBody) as T;
  }

  clearAntiforgeryToken(): void {
    this.antiforgeryToken = null;
  }

  private async applyAuthentication(
    headers: Headers,
    anonymous: boolean,
  ): Promise<void> {
    if (anonymous || this.authentication !== "bearer") return;
    const token = await this.options.getAccessToken?.();
    if (token) headers.set("Authorization", `Bearer ${token}`);
  }

  private async getAntiforgeryToken(
    signal?: AbortSignal | null,
  ): Promise<AntiforgeryTokenResponse> {
    if (this.antiforgeryToken) return this.antiforgeryToken;

    const response = await this.fetchImplementation(
      `${this.baseUrl}/api/security/antiforgery-token`,
      {
        method: "GET",
        headers: {
          Accept: "application/json",
          "Api-Version": "1.0",
          "X-Correlation-ID":
            this.options.createCorrelationId?.() ?? crypto.randomUUID(),
        },
        credentials: "include",
        signal,
      },
    );

    if (!response.ok)
      throw new ApiError(response.status, await parseProblem(response));
    this.antiforgeryToken = (await response.json()) as AntiforgeryTokenResponse;
    return this.antiforgeryToken;
  }
}

async function parseProblem(response: Response): Promise<ProblemDetails> {
  const fallback: ProblemDetails = {
    status: response.status,
    title: messageForStatus(response.status),
  };
  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.includes("json")) return fallback;

  try {
    return { ...fallback, ...((await response.json()) as ProblemDetails) };
  } catch {
    return fallback;
  }
}
