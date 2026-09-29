"use client";

import { useApiClient, useFormSubmission } from "@modular-mlm/api-client";
import {
  Button,
  FormErrorSummary,
  InputField,
  PageHeader,
} from "@modular-mlm/design-system";
import { useRouter } from "next/navigation";
import Link from "next/link";
import type { FormEvent } from "react";

export default function AgentRegisterPage() {
  const api = useApiClient();
  const router = useRouter();
  const feedback = useFormSubmission();

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const email = String(data.get("email") ?? "").trim();
    const password = String(data.get("password") ?? "");
    const confirmPassword = String(data.get("confirmPassword") ?? "");
    const created = await feedback.submit(async () => {
      if (password !== confirmPassword)
        throw new Error("Passwords must match.");
      await api.request<void>("/api/Users/register", {
        method: "POST",
        anonymous: true,
        skipAntiforgery: true,
        body: { email, password },
      });
    }, "Account created.");
    if (created)
      router.replace(
        `/sign-in?registered=true&email=${encodeURIComponent(email)}`,
      );
  }

  return (
    <div className="auth-page">
      <form className="auth-form" onSubmit={submit} noValidate>
        <PageHeader
          eyebrow="Agent Portal"
          title="Create your account"
          description="Create the identity you will use to submit and track your Agent application in this portal."
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
          Create Agent Portal account
        </Button>
        <p className="auth-form__alternate-action">
          Already have an account? <Link href="/sign-in">Sign in</Link>
        </p>
      </form>
    </div>
  );
}
