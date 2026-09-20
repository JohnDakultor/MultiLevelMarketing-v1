"use client";

import { ApiError, useApiQuery } from "@modular-mlm/api-client";
import {
  canAccessAgentOrganization,
  useAuthentication,
} from "@modular-mlm/auth";
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
import { agentPortalEnvironment } from "../lib/environment";
import { agentNavigation } from "../navigation/agentNavigation";
import { agentApi } from "../features/api/agentApi";

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

  if (pathname === "/sign-in") {
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
    return (
      <AgentApplicationAccessState
        organizationName={organization.name}
        application={application.data ?? undefined}
        isLoading={application.isLoading}
        hasNoApplication={
          application.error instanceof ApiError &&
          application.error.status === 404
        }
      />
    );
  }

  const disabledFeature =
    (pathname === "/network" && !organization.binaryNetworkEnabled) ||
    (pathname === "/wallet" && !organization.walletEnabled) ||
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
    >
      {children}
    </ApplicationShell>
  );
}

function AgentApplicationAccessState({
  organizationName,
  application,
  isLoading,
  hasNoApplication,
}: {
  organizationName: string;
  application?: { agentCode: string; status: number };
  isLoading: boolean;
  hasNoApplication: boolean;
}) {
  const { storefrontUrl } = agentPortalEnvironment();
  const applicationUrl = `${storefrontUrl}/account/agent-application`;
  if (isLoading) return <PortalLoading label="Checking Agent application" />;

  if (application) {
    const status = [
      "Applied",
      "Pending approval",
      "Active",
      "Inactive",
      "Suspended",
      "Closed",
    ][application.status];
    return (
      <PortalState>
        <PageHeader
          eyebrow="Agent application"
          title={status ?? `Status ${application.status}`}
          description={`Agent ${application.agentCode} for ${organizationName}.`}
        />
        <Card>
          <p>
            {application.status === 2
              ? "Your Agent record is active, but this identity has not yet received the Agent Portal role and organization assignment."
              : "The Agent Portal becomes available after an administrator approves and activates your application."}
          </p>
          <a className="portal-link" href={applicationUrl}>
            View application in storefront
          </a>
        </Card>
      </PortalState>
    );
  }

  return (
    <AccessDenied
      description={
        hasNoApplication
          ? `Apply to join the Agent program for ${organizationName}.`
          : `Your signed-in account cannot access the Agent workspace for ${organizationName}.`
      }
      actionHref={applicationUrl}
      actionLabel="Apply in the storefront"
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

function AccessDenied({
  description,
  actionHref,
  actionLabel,
}: {
  description: string;
  actionHref?: string;
  actionLabel?: string;
}) {
  const { storefrontUrl } = agentPortalEnvironment();
  return (
    <PortalState>
      <PageHeader
        eyebrow="Access denied"
        title="Agent access required"
        description={description}
      />
      <Card>
        <p>
          You are authenticated, but this account does not have the required
          Agent identity.
        </p>
        <a className="portal-link" href={actionHref ?? storefrontUrl}>
          {actionLabel ?? "Return to the storefront"}
        </a>
      </Card>
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
