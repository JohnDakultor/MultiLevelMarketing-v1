"use client";

import { useApiClient, useFormSubmission } from "@modular-mlm/api-client";
import {
  Alert,
  Button,
  FormErrorSummary,
  InputField,
  PageHeader,
} from "@modular-mlm/design-system";
import { useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";

export default function ResetPasswordPage() {
  const api = useApiClient();
  const parameters = useSearchParams();
  const feedback = useFormSubmission();
  const [complete, setComplete] = useState(false);
  const email = parameters.get("email") ?? "";
  const resetCode = parameters.get("code") ?? parameters.get("resetCode") ?? "";

  if (!email || !resetCode)
    return (
      <div className="auth-page">
        <Alert title="Reset link is incomplete" tone="danger">
          Request a new password-reset email and use the complete link from that
          message.
        </Alert>
        <a className="ds-button ds-button--primary" href="/forgot-password">
          Request another link
        </a>
      </div>
    );

  if (complete)
    return (
      <div className="auth-page">
        <Alert title="Password changed" tone="success">
          You can now sign in using your new password.
        </Alert>
        <a className="ds-button ds-button--primary" href="/sign-in">
          Continue to sign in
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
          const password = String(data.get("password"));
          const saved = await feedback.submit(async () => {
            if (password !== String(data.get("confirmPassword")))
              throw new Error("Passwords must match.");
            await api.request<void>("/api/Users/resetPassword", {
              method: "POST",
              anonymous: true,
              skipAntiforgery: true,
              body: { email, resetCode, newPassword: password },
            });
          }, "Password changed.");
          if (saved) setComplete(true);
        }}
      >
        <PageHeader
          title="Choose a new password"
          description={`Reset the password for ${email}.`}
        />
        <FormErrorSummary
          errors={feedback.fieldErrors}
          generalErrors={feedback.formErrors}
          id={feedback.errorSummaryId}
        />
        <InputField
          name="password"
          label="New password"
          type="password"
          autoComplete="new-password"
          minLength={8}
          required
          error={feedback.fieldError("newPassword")}
        />
        <InputField
          name="confirmPassword"
          label="Confirm new password"
          type="password"
          autoComplete="new-password"
          minLength={8}
          required
        />
        <Button
          type="submit"
          isLoading={feedback.isSubmitting}
          disabled={feedback.isSubmitting}
        >
          Change password
        </Button>
      </form>
    </div>
  );
}
