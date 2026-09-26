"use client";

import {
  safeReturnPath,
  SignInForm,
  useAuthentication,
} from "@modular-mlm/auth";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useAgentStorefrontUrl } from "../app/providers";

export function AgentSignInPage() {
  const router = useRouter();
  const authentication = useAuthentication();
  const storefrontUrl = useAgentStorefrontUrl();

  useEffect(() => {
    if (authentication.user) router.replace(readReturnPath());
  }, [authentication.user, router]);

  return (
    <div className="auth-page">
      <SignInForm
        title="Agent sign in"
        description="Access your network, sales, commissions, wallet, and payouts."
        onSuccess={() => router.replace(readReturnPath())}
      />
      <p>
        Shopping instead? <a href={storefrontUrl}>Open the storefront</a>
      </p>
      <p>
        New Agent?{" "}
        <a
          href={`${storefrontUrl}/register?returnTo=${encodeURIComponent(
            "/account/agent-application",
          )}`}
        >
          Create an account and apply
        </a>
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
