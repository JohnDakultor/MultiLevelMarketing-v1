"use client";

import {
  ResendConfirmationAction,
  safeReturnPath,
  SignInForm,
  useAuthentication,
} from "@modular-mlm/auth";
import { Alert } from "@modular-mlm/design-system";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useEffect } from "react";

export function StorefrontSignInPage({
  accountCreated = false,
  registeredEmail = "",
}: {
  accountCreated?: boolean;
  registeredEmail?: string;
}) {
  const router = useRouter();
  const authentication = useAuthentication();

  function returnPath(): string {
    return safeReturnPath(
      new URLSearchParams(window.location.search).get("returnTo"),
      "/account",
    );
  }

  useEffect(() => {
    if (authentication.user) router.replace(returnPath());
  }, [authentication.user, router]);

  return (
    <div className="auth-page">
      {accountCreated && (
        <Alert tone="success" title="Account created">
          Confirm your email address if a confirmation message was sent, then
          sign in with the password you created.
        </Alert>
      )}
      {accountCreated && registeredEmail && (
        <ResendConfirmationAction email={registeredEmail} />
      )}
      <SignInForm
        title="Sign in to your account"
        description="Manage your addresses, orders, cancellations, and refunds."
        initialEmail={registeredEmail}
        onSuccess={() => router.replace(returnPath())}
      />
      <p>
        <Link href="/forgot-password">Forgot your password?</Link>
      </p>
      <p>
        New here? <Link href="/register">Create an account</Link>
      </p>
    </div>
  );
}
