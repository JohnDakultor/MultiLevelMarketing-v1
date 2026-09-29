"use client";

import { useApiQuery } from "@modular-mlm/api-client";
import {
  canAccessAgentOrganization,
  useAuthentication,
} from "@modular-mlm/auth";
import {
  ApplicationShell,
  Button,
  ErrorState,
  Skeleton,
} from "@modular-mlm/design-system";
import { useOrganization } from "@modular-mlm/organization-context";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { agentNavigation } from "../navigation/agentNavigation";
import { agentApi } from "../features/api/agentApi";
import { AgentOnboarding } from "../features/onboarding/AgentOnboarding";

export function AgentPortalShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const authentication = useAuthentication();
  const organizationState = useOrganization();
  const application = useApiQuery(
    (api, signal) =>
      agentApi.application(api, organizationState.organization!.id, signal),
    [organizationState.organization?.id],
    Boolean(
      authentication.user &&
      organizationState.organization &&
      !canAccessAgentOrganization(
        authentication.user,
        organizationState.organization.id,
      ),
    ),
  );

  if (pathname === "/sign-in" || pathname === "/register") {
    return <main className="auth-only-layout">{children}</main>;
  }

  if (authentication.isLoading || organizationState.isLoading) {
    return <PortalLoading label="Loading Agent Portal" />;
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

  if (!authentication.user) {
    return <RedirectToSignIn returnTo={pathname} />;
  }

  if (organizationState.error || !organizationState.organization) {
    return (
      <PortalState>
        <ErrorState
          title="Organization unavailable"
          description="This Agent Portal hostname is not connected to an active organization."
        />
      </PortalState>
    );
  }

  const organization = organizationState.organization;
  if (!canAccessAgentOrganization(authentication.user, organization.id)) {
    if (
      authentication.user.organizationId !== null &&
      authentication.user.organizationId !== organization.id
    ) {
      return (
        <PortalState>
          <ErrorState
            title="Account belongs to another organization"
            description={`This credential is assigned to organization ${authentication.user.organizationId}, but this Agent Portal is configured for ${organization.name}. Changing the portal slug does not transfer an Agent account. Sign in with an account created for this organization.`}
          />
        </PortalState>
      );
    }
    if (!organization.agentProgramEnabled) {
      return (
        <PortalState>
          <ErrorState
            title="Agent applications are unavailable"
            description="This organization has not enabled its Agent program."
          />
        </PortalState>
      );
    }
    return (
      <AgentApplicationAccessState
        organizationId={organization.id}
        organizationName={organization.name}
        application={application.data ?? undefined}
        isLoading={application.isLoading}
        error={application.error}
        reload={application.reload}
        refreshIdentity={authentication.refresh}
      />
    );
  }

  const disabledFeature =
    (pathname === "/network" && !organization.binaryNetworkEnabled) ||
    (["/wallet", "/transactions"].includes(pathname) &&
      !organization.walletEnabled) ||
    (pathname === "/payouts" && !organization.payoutEnabled) ||
    (pathname === "/referrals" && !organization.agentProgramEnabled);
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
        contextLabel: "Agent Portal",
        logoUrl: organization.logoUrl,
      }}
      navigation={agentNavigation(authentication.user, organization)}
      currentPath={pathname}
      account={<AgentAccountAction />}
      footer={`${organization.name} Agent Portal`}
      linkComponent={Link}
    >
      {children}
    </ApplicationShell>
  );
}

function AgentApplicationAccessState({
  organizationId,
  organizationName,
  application,
  isLoading,
  error,
  reload,
  refreshIdentity,
}: {
  organizationId: string;
  organizationName: string;
  application?: import("@modular-mlm/contracts").AgentApplicationDto;
  isLoading: boolean;
  error: Error | null;
  reload(): void;
  refreshIdentity(): Promise<void>;
}) {
  if (isLoading) return <PortalLoading label="Checking Agent application" />;
  return (
    <AgentOnboarding
      organizationId={organizationId}
      organizationName={organizationName}
      application={application}
      error={error}
      reload={reload}
      refreshIdentity={refreshIdentity}
    />
  );
}

function AgentAccountAction() {
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
      <p role="status">Redirecting you to Agent sign in…</p>
    </PortalState>
  );
}

function PortalLoading({ label }: { label: string }) {
  return (
    <PortalState>
      <span className="ds-sr-only" role="status">
        {label}
      </span>
      <Skeleton height="4rem" />
      <Skeleton height="16rem" />
    </PortalState>
  );
}

function PortalState({ children }: { children: ReactNode }) {
  return <main className="portal-state">{children}</main>;
}
