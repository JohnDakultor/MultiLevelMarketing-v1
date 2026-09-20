"use client";

import {
  canAdministerOrganization,
  hasRole,
  useAuthentication,
} from "@modular-mlm/auth";
import { Roles } from "@modular-mlm/contracts";
import {
  ApplicationShell,
  Button,
  Card,
  ErrorState,
  PageHeader,
  Skeleton,
} from "@modular-mlm/design-system";
import { useOrganization } from "@modular-mlm/organization-context";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { adminPortalEnvironment } from "../lib/environment";
import { adminNavigation } from "../navigation/adminNavigation";

export function AdminPortalShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const authentication = useAuthentication();
  const organizationState = useOrganization();
  const isPlatformRoute = pathname.startsWith("/platform");

  if (pathname === "/sign-in" || pathname === "/invitations/accept")
    return <main className="auth-only-layout">{children}</main>;
  if (authentication.isLoading) {
    return <PortalLoading />;
  }
  if (authentication.error) {
    return (
      <PortalState>
        <ErrorState
          title="Identity unavailable"
          description={`${authentication.error.message} Confirm the Web API is running at the server-only BACKEND_API_BASE_URL, then try again.`}
          action={
            <Button onClick={() => void authentication.refresh()}>
              Try again
            </Button>
          }
        />
      </PortalState>
    );
  }
  if (!authentication.user) return <RedirectToSignIn returnTo={pathname} />;
  if (isPlatformRoute) {
    if (!hasRole(authentication.user, Roles.platformAdministrator)) {
      return (
        <AccessDenied description="Your account is not a platform administrator." />
      );
    }
    return (
      <ApplicationShell
        variant="workspace"
        brand={{ name: "Modular MLM", contextLabel: "Platform Administration" }}
        navigation={[
          { href: "/platform/organizations/new", label: "Create organization" },
          { href: "/platform/security", label: "Sign-in sessions" },
        ]}
        currentPath={pathname}
        account={<AdminAccountAction />}
        footer="Platform administration"
      >
        {children}
      </ApplicationShell>
    );
  }
  if (organizationState.isLoading) return <PortalLoading />;
  if (organizationState.error || !organizationState.organization) {
    return (
      <PortalState>
        <ErrorState
          title="Organization unavailable"
          description="This Admin Portal hostname is not connected to an active organization."
        />
      </PortalState>
    );
  }

  const organization = organizationState.organization;
  if (!canAdministerOrganization(authentication.user, organization.id)) {
    return (
      <AccessDenied
        description={`Your signed-in account cannot administer ${organization.name}.`}
      />
    );
  }

  const disabledFeature =
    (["/agents", "/compensation"].includes(pathname) &&
      !organization.agentProgramEnabled) ||
    (pathname === "/payouts" && !organization.payoutEnabled);
  if (disabledFeature) {
    return (
      <PortalState>
        <ErrorState
          title="Feature unavailable"
          description="This feature is disabled for the current organization."
        />
      </PortalState>
    );
  }

  return (
    <ApplicationShell
      variant="workspace"
      brand={{
        name: organization.name,
        contextLabel: "Admin Portal",
        logoUrl: organization.logoUrl,
      }}
      navigation={adminNavigation(authentication.user, organization)}
      currentPath={pathname}
      account={<AdminAccountAction />}
      footer={`Administering ${organization.name}`}
    >
      {children}
    </ApplicationShell>
  );
}

function AdminAccountAction() {
  const authentication = useAuthentication();
  const router = useRouter();
  return (
    <>
      <span className="account-name">{authentication.user?.displayName}</span>
      <Button
        variant="ghost"
        onClick={async () => {
          await authentication.signOut();
          router.replace("/sign-in");
        }}
      >
        Sign out
      </Button>
    </>
  );
}

function RedirectToSignIn({ returnTo }: { returnTo: string }) {
  const router = useRouter();
  useEffect(() => {
    router.replace(`/sign-in?returnTo=${encodeURIComponent(returnTo)}`);
  }, [returnTo, router]);
  return (
    <PortalState>
      <p role="status">Redirecting you to Administrator sign in…</p>
    </PortalState>
  );
}

function AccessDenied({ description }: { description: string }) {
  return (
    <PortalState>
      <PageHeader
        eyebrow="Access denied"
        title="Administrator access required"
        description={description}
      />
      <Card>
        <p>
          You are authenticated, but you do not have permission to administer
          this organization.
        </p>
        <a
          className="portal-link"
          href={adminPortalEnvironment().storefrontUrl}
        >
          Return to the storefront
        </a>
      </Card>
    </PortalState>
  );
}

function PortalLoading() {
  return (
    <PortalState>
      <span className="ds-sr-only" role="status">
        Loading Admin Portal
      </span>
      <Skeleton height="4rem" />
      <Skeleton height="16rem" />
    </PortalState>
  );
}

function PortalState({ children }: { children: ReactNode }) {
  return <main className="portal-state">{children}</main>;
}
