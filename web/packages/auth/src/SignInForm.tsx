"use client";

import { ApiError, validationMessages } from "@modular-mlm/api-client";
import { useRef, useState, type FormEvent } from "react";
import { useAuthentication } from "./AuthenticationProvider";

export function SignInForm({
  title = "Sign in",
  description,
  initialEmail = "",
  onSuccess,
}: {
  title?: string;
  description?: string;
  initialEmail?: string;
  onSuccess(): void;
}) {
  const authentication = useAuthentication();
  const [email, setEmail] = useState(initialEmail);
  const [password, setPassword] = useState("");
  const [isSubmitting, setSubmitting] = useState(false);
  const [messages, setMessages] = useState<string[]>([]);
  const errorRef = useRef<HTMLDivElement>(null);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true);
    setMessages([]);
    try {
      await authentication.signIn(email.trim(), password);
      onSuccess();
    } catch (error) {
      setMessages(signInErrorMessages(error));
      requestAnimationFrame(() => errorRef.current?.focus());
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="auth-form" onSubmit={submit} noValidate>
      <header>
        <h1>{title}</h1>
        {description && <p>{description}</p>}
      </header>

      {messages.length > 0 && (
        <div
          ref={errorRef}
          className="auth-form__error"
          role="alert"
          tabIndex={-1}
        >
          <strong>Sign-in failed</strong>
          <ul>
            {messages.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </ul>
        </div>
      )}

      <label>
        <span>Email address</span>
        <input
          name="email"
          type="email"
          autoComplete="email"
          required
          value={email}
          onChange={(event) => setEmail(event.target.value)}
        />
      </label>

      <label>
        <span>Password</span>
        <input
          name="password"
          type="password"
          autoComplete="current-password"
          required
          value={password}
          onChange={(event) => setPassword(event.target.value)}
        />
      </label>

      <button type="submit" disabled={isSubmitting} aria-busy={isSubmitting}>
        {isSubmitting ? "Signing in..." : "Sign in"}
      </button>
    </form>
  );
}

export function signInErrorMessages(error: unknown): string[] {
  if (!(error instanceof ApiError)) return validationMessages(error);

  switch (error.status) {
    case 0:
      return [
        "The sign-in service could not be reached. Confirm the API is running and check your connection.",
      ];
    case 400:
    case 422:
      return validationMessages(error);
    case 401:
      return [
        "The email or password is incorrect. Check your credentials and try again.",
      ];
    case 403:
      return [
        "This account is not allowed to sign in here. Confirm the account is active and has access to this portal.",
      ];
    case 429:
      return [
        "Too many sign-in attempts were made. Wait a moment before trying again.",
      ];
    default:
      return validationMessages(error);
  }
}

export function safeReturnPath(value: string | null, fallback = "/"): string {
  if (!value || !value.startsWith("/") || value.startsWith("//"))
    return fallback;
  return value;
}
