"use client";

import { useAuthentication } from "@modular-mlm/auth";
import {
  ApplicationShell,
  Button,
  ErrorState,
  Skeleton,
} from "@modular-mlm/design-system";
import { useOrganization } from "@modular-mlm/organization-context";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { storefrontNavigation } from "../navigation/storefrontNavigation";

export function StorefrontShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const authentication = useAuthentication();
  const organizationState = useOrganization();
  const requiresCustomer =
    pathname === "/account" ||
    pathname.startsWith("/account/") ||
    pathname === "/notifications";
  const isIdentityPage = [
    "/sign-in",
    "/register",
    "/forgot-password",
    "/reset-password",
  ].includes(pathname);

  useEffect(() => {
    if (
      requiresCustomer &&
      !authentication.isLoading &&
      !authentication.isAuthenticated
    ) {
      router.replace(`/sign-in?returnTo=${encodeURIComponent(pathname)}`);
    }
  }, [
    authentication.isAuthenticated,
    authentication.isLoading,
    pathname,
    requiresCustomer,
    router,
  ]);

  const organization = organizationState.organization;
  const brand = {
    name: organization?.storeTitle || organization?.name || "Marketplace",
    contextLabel: organization?.name || "Storefront",
    logoUrl: organization?.logoUrl,
  };

  return (
    <ApplicationShell
      variant="storefront"
      brand={brand}
      navigation={storefrontNavigation(authentication.user, organization)}
      currentPath={pathname}
      account={<StorefrontAccountAction />}
      footer={
        organization
          ? `${organization.name} · Prices shown in ${organization.currencyCode}`
          : undefined
      }
    >
      {organizationState.error && !organization && !isIdentityPage ? (
        <ShellState>
          <ErrorState
            title="Storefront unavailable"
            description="This hostname is not connected to an active organization."
          />
        </ShellState>
      ) : requiresCustomer && authentication.isLoading ? (
        <ShellState>
          <Skeleton height="9rem" />
        </ShellState>
      ) : requiresCustomer && !authentication.isAuthenticated ? (
        <ShellState>
          <p role="status">Redirecting you to sign in…</p>
        </ShellState>
      ) : (
        children
      )}
    </ApplicationShell>
  );
}

function StorefrontAccountAction() {
  const authentication = useAuthentication();
  const router = useRouter();

  if (authentication.isLoading)
    return <Skeleton width="7rem" height="2.25rem" />;
  if (!authentication.user)
    return (
      <div className="storefront-auth-actions">
        <a href="/sign-in">Sign in</a>
        <a className="storefront-auth-actions__primary" href="/register">
          Create account
        </a>
      </div>
    );

  return (
    <>
      <a href="/account">{authentication.user.displayName || "My account"}</a>
      <Button
        variant="ghost"
        type="button"
        onClick={async () => {
          await authentication.signOut();
          router.replace("/");
        }}
      >
        Sign out
      </Button>
    </>
  );
}

function ShellState({ children }: { children: ReactNode }) {
  return <div className="shell-state">{children}</div>;
}
