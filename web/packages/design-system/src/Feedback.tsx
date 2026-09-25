import type { ReactNode } from "react";

export type FeedbackTone =
  "neutral" | "success" | "warning" | "danger" | "info";

export function Badge({
  children,
  tone = "neutral",
}: {
  children: ReactNode;
  tone?: FeedbackTone;
}) {
  return <span className={`ds-badge ds-badge--${tone}`}>{children}</span>;
}

export function Alert({
  title,
  children,
  tone = "info",
}: {
  title: string;
  children?: ReactNode;
  tone?: Exclude<FeedbackTone, "neutral">;
}) {
  return (
    <div
      className={`ds-alert ds-alert--${tone}`}
      role={tone === "danger" ? "alert" : "status"}
    >
      <strong>{title}</strong>
      {children && <div>{children}</div>}
    </div>
  );
}

export function Skeleton({
  width = "100%",
  height = "1rem",
}: {
  width?: string;
  height?: string;
}) {
  return <span className="ds-skeleton" style={{ width, height }} aria-hidden />;
}

export function EmptyState({
  title,
  description,
  action,
}: {
  title: string;
  description: string;
  action?: ReactNode;
}) {
  return (
    <section className="ds-empty-state">
      <h2>{title}</h2>
      <p>{description}</p>
      {action}
    </section>
  );
}

export function ErrorState({
  title = "We could not load this",
  description,
  action,
}: {
  title?: string;
  description: string;
  action?: ReactNode;
}) {
  return (
    <section className="ds-error-state" role="alert">
      <h2>{title}</h2>
      <p>{description}</p>
      {action}
    </section>
  );
}

export function PermissionDeniedState({
  description = "You are signed in, but you do not have permission to view this information.",
}: {
  description?: string;
}) {
  return (
    <section className="ds-error-state" role="alert">
      <h2>Access denied</h2>
      <p>{description}</p>
    </section>
  );
}

export function FormErrorSummary({
  title = "Check the highlighted fields",
  errors,
  generalErrors = [],
  id,
}: {
  title?: string;
  errors: Readonly<Record<string, readonly string[]>>;
  generalErrors?: readonly string[];
  id?: string;
}) {
  const entries = Object.entries(errors).filter(
    ([, messages]) => messages.length > 0,
  );
  if (entries.length === 0 && generalErrors.length === 0) return null;

  return (
    <div id={id} className="ds-form-error-summary" role="alert" tabIndex={-1}>
      <strong>{title}</strong>
      <ul>
        {generalErrors.map((message) => (
          <li key={message}>{message}</li>
        ))}
        {entries.map(([field, messages]) => (
          <li key={field}>
            <a href={`#${field}`}>{messages[0]}</a>
          </li>
        ))}
      </ul>
    </div>
  );
}

export function ToastRegion({ children }: { children: ReactNode }) {
  return (
    <div className="ds-toast-region" aria-live="polite" aria-atomic="true">
      {children}
    </div>
  );
}
