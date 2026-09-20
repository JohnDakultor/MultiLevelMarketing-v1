"use client";
import { useApiClient, useFormSubmission } from "@modular-mlm/api-client";
import { safeReturnPath } from "@modular-mlm/auth";
import {
  Button,
  FormErrorSummary,
  InputField,
  PageHeader,
} from "@modular-mlm/design-system";
import { useRouter, useSearchParams } from "next/navigation";
import type { FormEvent } from "react";

export default function RegisterPage() {
  const api = useApiClient();
  const router = useRouter();
  const searchParams = useSearchParams();
  const feedback = useFormSubmission();
  return (
    <div className="auth-only-layout">
      <form
        className="auth-form"
        onSubmit={async (event: FormEvent<HTMLFormElement>) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          const password = String(data.get("password"));
          await feedback.submit(async () => {
            if (password !== String(data.get("confirmPassword")))
              throw new Error("Passwords must match.");
            await api.request<void>("/api/Users/register", {
              method: "POST",
              anonymous: true,
              skipAntiforgery: true,
              body: { email: String(data.get("email")).trim(), password },
            });
            const email = String(data.get("email")).trim();
            const returnTo = safeReturnPath(
              searchParams.get("returnTo"),
              "/account",
            );
            router.replace(
              `/sign-in?registered=true&email=${encodeURIComponent(
                email,
              )}&returnTo=${encodeURIComponent(returnTo)}`,
            );
          }, "Account created.");
        }}
      >
        <PageHeader
          title="Create customer account"
          description="Create an account for this marketplace. Your customer profile is provisioned by the backend."
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
        <InputField
          name="password"
          label="Password"
          type="password"
          autoComplete="new-password"
          minLength={8}
          required
          error={feedback.fieldError("password")}
        />
        <InputField
          name="confirmPassword"
          label="Confirm password"
          type="password"
          autoComplete="new-password"
          minLength={8}
          required
          error={feedback.fieldError("confirmPassword")}
        />
        <Button
          type="submit"
          isLoading={feedback.isSubmitting}
          disabled={feedback.isSubmitting}
        >
          Create account
        </Button>
        <p className="auth-form__alternate-action">
          Already have an account? <a href="/sign-in">Sign in</a>
        </p>
      </form>
    </div>
  );
}
