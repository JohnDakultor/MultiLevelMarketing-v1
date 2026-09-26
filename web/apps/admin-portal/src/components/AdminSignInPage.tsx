"use client";

import {
  safeReturnPath,
  SignInForm,
  useAuthentication,
} from "@modular-mlm/auth";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useAdminStorefrontUrl } from "../app/providers";

export function AdminSignInPage() {
  const router = useRouter();
  const authentication = useAuthentication();
  const storefrontUrl = useAdminStorefrontUrl();

  useEffect(() => {
    if (authentication.user) router.replace(readReturnPath());
  }, [authentication.user, router]);

  return (
    <div className="auth-page">
      <SignInForm
        title="Administrator sign in"
        description="Manage your organization, catalog, Agents, finance, and operations."
        onSuccess={() => router.replace(readReturnPath())}
      />
      <p>
        Shopping instead? <a href={storefrontUrl}>Open the storefront</a>
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
