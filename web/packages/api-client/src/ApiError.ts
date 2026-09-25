import type { ProblemDetails } from "@modular-mlm/contracts";

const statusMessages: Readonly<Record<number, string>> = {
  400: "Check the information you entered and try again.",
  401: "Your session has expired. Sign in again to continue.",
  403: "You do not have permission to perform this action.",
  404: "The requested information could not be found.",
  408: "The request took too long. Check your connection and try again.",
  409: "This change conflicts with the current record. Refresh and try again.",
  422: "Some fields need your attention.",
  429: "Too many requests. Wait a moment and try again.",
  500: "Something went wrong on the server. Try again shortly.",
  502: "The service received an invalid response. Try again shortly.",
  503: "The service is temporarily unavailable. Try again shortly.",
  504: "The service took too long to respond. Try again shortly.",
};

const genericProblemTitles = new Set([
  "bad request",
  "unauthorized",
  "forbidden",
  "not found",
  "conflict",
  "unprocessable entity",
  "too many requests",
  "internal server error",
  "service unavailable",
]);

export function messageForStatus(status: number): string {
  if (status === 0) return "The server could not be reached.";
  return statusMessages[status] ?? "The request could not be completed.";
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
    options?: ErrorOptions,
  ) {
    super(messageForProblem(status, problem), options);
    this.name = "ApiError";
  }

  get isUnauthorized(): boolean {
    return this.status === 401;
  }

  get isForbidden(): boolean {
    return this.status === 403;
  }

  get isConflict(): boolean {
    return this.status === 409;
  }
}

function messageForProblem(status: number, problem: ProblemDetails): string {
  const detail = problem.detail?.trim();
  if (detail) return detail;
  const title = problem.title?.trim();
  if (title && !genericProblemTitles.has(title.toLowerCase())) return title;
  return messageForStatus(status);
}

export function validationMessages(error: unknown): string[] {
  if (!(error instanceof ApiError))
    return [
      error instanceof Error
        ? error.message
        : "Something unexpected happened. Try again.",
    ];

  const errors = error.problem.errors;
  if (Array.isArray(errors)) return errors;
  if (errors && typeof errors === "object") return Object.values(errors).flat();
  return [error.message];
}

export type FieldErrors = Readonly<Record<string, readonly string[]>>;

/** Maps RFC Problem Details validation keys to HTML form field names. */
export function validationFieldErrors(error: unknown): FieldErrors {
  if (!(error instanceof ApiError)) return {};
  const errors = error.problem.errors;
  if (!errors || Array.isArray(errors)) return {};

  return Object.fromEntries(
    Object.entries(errors).map(([key, messages]) => [
      normalizeFieldName(key),
      messages,
    ]),
  );
}

export function firstFieldError(
  errors: FieldErrors,
  fieldName: string,
): string | undefined {
  return errors[normalizeFieldName(fieldName)]?.[0];
}

export function isRetrySafe(error: unknown): boolean {
  if (!(error instanceof ApiError)) return true;
  return (
    error.status === 0 ||
    error.status === 408 ||
    error.status === 429 ||
    error.status >= 500
  );
}

function normalizeFieldName(value: string): string {
  const segment =
    value
      .split(/[.[\]]/)
      .filter(Boolean)
      .at(-1) ?? value;
  return segment.slice(0, 1).toLowerCase() + segment.slice(1);
}
