"use client";

import { useApiClient, useFormSubmission } from "@modular-mlm/api-client";
import {
  Alert,
  Button,
  FormErrorSummary,
  InputField,
  PageHeader,
} from "@modular-mlm/design-system";
import { useState, type FormEvent } from "react";

export default function ForgotPasswordPage() {
  const api = useApiClient();
  const feedback = useFormSubmission();
  const [requested, setRequested] = useState(false);

  if (requested)
    return (
      <div className="auth-page">
        <Alert title="Check your email" tone="success">
          If an account exists for that address, password-reset instructions
          have been requested.
        </Alert>
        <a className="ds-button ds-button--primary" href="/sign-in">
          Return to sign in
        </a>
      </div>
    );

  return (
    <div className="auth-page">
      <form
        className="auth-form"
        onSubmit={async (event: FormEvent<HTMLFormElement>) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          const sent = await feedback.submit(async () => {
            await api.request<void>("/api/Users/forgotPassword", {
              method: "POST",
              anonymous: true,
              skipAntiforgery: true,
              body: { email: String(data.get("email")).trim() },
            });
          }, "Reset instructions requested.");
          if (sent) setRequested(true);
        }}
      >
        <PageHeader
          title="Reset your password"
          description="Enter the email address used for your marketplace account."
        />
        <FormErrorSummary
          errors={feedback.fieldErrors}
          generalErrors={feedback.formErrors}
          id={feedback.errorSummaryId}
        />
        <InputField
          name="email"
          label="Email address"
          type="email"
          autoComplete="email"
          required
          error={feedback.fieldError("email")}
        />
        <Button
          type="submit"
          isLoading={feedback.isSubmitting}
          disabled={feedback.isSubmitting}
        >
          Send reset instructions
        </Button>
      </form>
      <p>
        <a href="/sign-in">Return to sign in</a>
      </p>
    </div>
  );
}
