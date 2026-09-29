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
import Link from "next/link";
import { useEffect, type ReactNode } from "react";
import { storefrontNavigation } from "../navigation/storefrontNavigation";
import { StorefrontFooter } from "../features/shared/StorefrontPrimitives";
import type { PublicOrganizationConfigDto } from "@modular-mlm/contracts";

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
    name: organization?.storeTitle || organization?.name || "Shop",
    contextLabel: "Online store",
    logoUrl: organization?.logoUrl,
  };

  return (
    <ApplicationShell
      variant="storefront"
      brand={brand}
      navigation={storefrontNavigation(authentication.user, organization)}
      currentPath={pathname}
      account={<StorefrontAccountAction />}
      linkComponent={Link}
      footer={
        organization ? (
          <StorefrontFooter
            storeName={organization.storeTitle || organization.name}
          />
        ) : undefined
      }
    >
      <StorefrontRouteFrame
        isIdentityPage={isIdentityPage}
        organization={organization}
      >
        {organizationState.error && !organization && !isIdentityPage ? (
          <ShellState>
            <ErrorState
              title="Storefront unavailable"
              description="This shop is temporarily unavailable. Please try again later."
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
      </StorefrontRouteFrame>
    </ApplicationShell>
  );
}

function StorefrontRouteFrame({
  isIdentityPage,
  organization,
  children,
}: {
  isIdentityPage: boolean;
  organization: PublicOrganizationConfigDto | null;
  children: ReactNode;
}) {
  if (!isIdentityPage)
    return <div className="application-container">{children}</div>;

  const storeName = organization?.storeTitle || organization?.name || "Shop";

  return (
    <div className="storefront-identity-stage">
      <aside className="storefront-identity-brand" aria-label={storeName}>
        <div className="storefront-identity-brand__content">
          {organization?.logoUrl ? (
            <span
              className="storefront-identity-brand__logo"
              style={{ backgroundImage: `url(${organization.logoUrl})` }}
              aria-hidden
            />
          ) : (
            <span className="storefront-identity-brand__mark" aria-hidden>
              {storeName.slice(0, 1).toUpperCase()}
            </span>
          )}
          <p className="storefront-kicker">Welcome to</p>
          <h1>{storeName}</h1>
          <p>
            Sign in or create an account to manage delivery details, follow
            orders, and complete checkout securely.
          </p>
        </div>
        <div className="storefront-identity-brand__trust">
          <span>Secure account</span>
          <span>Live order status</span>
          <span>Protected checkout</span>
        </div>
      </aside>
      <section className="storefront-identity-panel">{children}</section>
    </div>
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
        <Link href="/sign-in">Sign in</Link>
        <Link className="storefront-auth-actions__primary" href="/register">
          Create account
        </Link>
      </div>
    );

  return (
    <>
      <Link href="/account">
        {authentication.user.displayName || "My account"}
      </Link>
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
