"use client";

import {
  safeReturnPath,
  SignInForm,
  useAuthentication,
} from "@modular-mlm/auth";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useEffect } from "react";
import { useAgentStorefrontUrl } from "../app/providers";
import { Alert } from "@modular-mlm/design-system";

export function AgentSignInPage({
  accountCreated = false,
  registeredEmail = "",
}: {
  accountCreated?: boolean;
  registeredEmail?: string;
}) {
  const router = useRouter();
  const authentication = useAuthentication();
  const storefrontUrl = useAgentStorefrontUrl();

  useEffect(() => {
    if (authentication.user) router.replace(readReturnPath());
  }, [authentication.user, router]);

  return (
    <div className="auth-page">
      {accountCreated && (
        <Alert tone="success" title="Account created">
          Confirm your email address if required, then sign in to submit your
          Agent application.
        </Alert>
      )}
      <SignInForm
        title="Agent sign in"
        description="Access your network, sales, commissions, wallet, and payouts."
        initialEmail={registeredEmail}
        onSuccess={() => router.replace(readReturnPath())}
      />
      <p>
        Shopping instead? <a href={storefrontUrl}>Open the storefront</a>
      </p>
      <p>
        New Agent?{" "}
        <Link href="/register">Create an account and apply here</Link>
      </p>
    </div>
  );
}

function readReturnPath(): string {
  return safeReturnPath(
    new URLSearchParams(window.location.search).get("returnTo"),
    "/",
  );
}
