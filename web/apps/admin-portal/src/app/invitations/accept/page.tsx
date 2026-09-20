"use client";
import { useApiClient, useFormSubmission } from "@modular-mlm/api-client";
import {
  Button,
  FormErrorSummary,
  InputField,
  PageHeader,
} from "@modular-mlm/design-system";
import { useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";

export default function AcceptAdministratorInvitationPage() {
  const api = useApiClient();
  const params = useSearchParams();
  const [complete, setComplete] = useState(false);
  const feedback = useFormSubmission();
  const token = params.get("token") ?? "";
  if (complete)
    return (
      <main className="auth-only-layout">
        <div className="auth-form">
          <PageHeader
            title="Invitation accepted"
            description="Your Administrator account is ready."
          />
          <a className="ds-button ds-button--primary" href="/sign-in">
            Sign in to Admin Portal
          </a>
        </div>
      </main>
    );
  return (
    <main className="auth-only-layout">
      <form
        className="auth-form"
        onSubmit={async (event: FormEvent<HTMLFormElement>) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          const password = String(data.get("password"));
          const confirmPassword = String(data.get("confirmPassword"));
          await feedback.submit(async () => {
            if (!token) throw new Error("This invitation link has no token.");
            if (password !== confirmPassword)
              throw new Error("Passwords must match.");
            await api.request<string>("/api/administrator-invitations/accept", {
              method: "POST",
              anonymous: true,
              skipAntiforgery: true,
              body: {
                rawToken: token,
                displayName: String(data.get("displayName")),
                password,
                confirmPassword,
              },
            });
            setComplete(true);
          }, "Invitation accepted.");
        }}
      >
        <PageHeader
          title="Accept Administrator invitation"
          description="Set up the account associated with this one-time invitation."
        />
        <FormErrorSummary
          errors={feedback.fieldErrors}
          generalErrors={feedback.formErrors}
          id={feedback.errorSummaryId}
        />
        <InputField
          name="displayName"
          label="Display name"
          required
          error={feedback.fieldError("displayName")}
        />
        <InputField
          name="password"
          label="Password"
          type="password"
          minLength={8}
          autoComplete="new-password"
          required
          error={feedback.fieldError("password")}
        />
        <InputField
          name="confirmPassword"
          label="Confirm password"
          type="password"
          minLength={8}
          autoComplete="new-password"
          required
          error={feedback.fieldError("confirmPassword")}
        />
        <Button
          type="submit"
          isLoading={feedback.isSubmitting}
          disabled={feedback.isSubmitting}
        >
          Accept invitation
        </Button>
      </form>
    </main>
  );
}
