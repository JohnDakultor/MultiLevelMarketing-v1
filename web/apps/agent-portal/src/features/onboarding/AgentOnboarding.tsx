"use client";

import {
  ApiError,
  useApiClient,
  useFormSubmission,
} from "@modular-mlm/api-client";
import type { AgentApplicationDto } from "@modular-mlm/contracts";
import {
  Alert,
  Button,
  Card,
  ErrorState,
  FormErrorSummary,
  InputField,
  PageHeader,
  StatusBadge,
} from "@modular-mlm/design-system";
import { useEffect, type FormEvent, type ReactNode } from "react";
import { useSearchParams } from "next/navigation";
import { agentApi } from "../api/agentApi";
import { date } from "../shared/AgentScreenState";
import { AgentStatusBadge } from "../shared/status";

export function AgentOnboarding({
  organizationId,
  organizationName,
  application,
  error,
  reload,
  refreshIdentity,
}: {
  organizationId: string;
  organizationName: string;
  application?: AgentApplicationDto;
  error: Error | null;
  reload(): void;
  refreshIdentity(): Promise<void>;
}) {
  const api = useApiClient();
  const submission = useFormSubmission();
  const searchParameters = useSearchParams();
  const invitedByCode = searchParameters.get("sponsor")?.trim() ?? "";
  const hasNoApplication = error instanceof ApiError && error.status === 404;

  useEffect(() => {
    if (application?.status !== 2) return;

    const refreshKey = `agent-access-refreshed:${application.agentId}`;
    if (window.sessionStorage.getItem(refreshKey)) return;

    // Approval updates the identity role and organization assignment after the
    // application was created. Refresh the live identity once so an Agent who
    // kept this page open does not need to sign out merely to see that access.
    window.sessionStorage.setItem(refreshKey, "true");
    void refreshIdentity();
  }, [application?.agentId, application?.status, refreshIdentity]);

  if (error && !hasNoApplication) {
    return (
      <OnboardingLayout>
        <ErrorState
          title="Application status unavailable"
          description={error.message}
          action={<Button onClick={reload}>Try again</Button>}
        />
      </OnboardingLayout>
    );
  }

  if (application) {
    return (
      <OnboardingLayout>
        <PageHeader
          eyebrow="Agent onboarding"
          title="Your application is being handled here"
          description={`Track your ${organizationName} Agent application without returning to the Storefront.`}
        />
        <Card className="agent-application-card">
          <div className="agent-application-card__header">
            <div>
              <span className="muted-label">Application status</span>
              <h2>{application.agentCode}</h2>
            </div>
            <AgentStatusBadge value={application.status} />
          </div>
          <dl className="details-list">
            <div>
              <dt>Submitted</dt>
              <dd>{date(application.joinedAt)}</dd>
            </div>
            <div>
              <dt>Sponsor</dt>
              <dd>{application.sponsorAgentCode ?? "No sponsor"}</dd>
            </div>
            <div>
              <dt>Placement</dt>
              <dd>
                {application.sponsorAgentId === null
                  ? "Network root"
                  : application.isPlacementPending
                    ? "Pending placement"
                    : "Assigned"}
              </dd>
            </div>
            <div>
              <dt>Recorded qualification state</dt>
              <dd>{application.qualificationState}</dd>
            </div>
          </dl>
          {application.status === 2 ? (
            <Alert title="Access is being synchronized" tone="warning">
              <p>
                The Agent record is active, but the latest identity role or
                organization assignment has not loaded yet.
              </p>
              <Button
                variant="secondary"
                onClick={() => void refreshIdentity()}
              >
                Retry Agent access
              </Button>
            </Alert>
          ) : (
            <Alert title="Administrator action required" tone="info">
              Placement, approval, and activation are controlled by the
              organization administrator. No additional action is required here.
            </Alert>
          )}
          <Button variant="secondary" onClick={reload}>
            Refresh status
          </Button>
        </Card>
      </OnboardingLayout>
    );
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const sponsorReferralCode = String(
      new FormData(form).get("sponsorReferralCode") ?? "",
    ).trim();
    const submitted = await submission.submit(
      () => agentApi.apply(api, organizationId, sponsorReferralCode || null),
      "Agent application submitted.",
    );
    if (submitted) reload();
  }

  return (
    <OnboardingLayout>
      <PageHeader
        eyebrow="Agent onboarding"
        title={`Apply to join ${organizationName}`}
        description="This application belongs to your signed-in identity and is completed entirely inside the Agent Portal."
      />
      <div className="detail-grid agent-onboarding-grid">
        <Card className="form-card">
          <h2>Agent application</h2>
          <p>
            The backend creates your Agent code and submits the application for
            administrator approval. It does not allow self-placement or
            self-activation.
          </p>
          <form className="form-grid" onSubmit={submit} noValidate>
            <FormErrorSummary
              errors={submission.fieldErrors}
              generalErrors={submission.formErrors}
              id={submission.errorSummaryId}
            />
            <InputField
              name="sponsorReferralCode"
              label="Sponsor referral code (optional)"
              hint="Invitation links fill this automatically. The code must belong to an active Agent in this organization."
              defaultValue={invitedByCode}
              maxLength={64}
              autoComplete="off"
              error={submission.fieldError("sponsorReferralCode")}
            />
            <Button
              type="submit"
              isLoading={submission.isSubmitting}
              disabled={submission.isSubmitting}
            >
              Submit Agent application
            </Button>
          </form>
        </Card>
        <Card>
          <h2>What happens next</h2>
          <ol className="agent-steps">
            <li>
              <StatusBadge label="1" tone="info" /> Application submitted
            </li>
            <li>
              <StatusBadge label="2" tone="warning" /> Administrator reviews and
              places the account
            </li>
            <li>
              <StatusBadge label="3" tone="success" /> Agent access becomes
              active
            </li>
          </ol>
        </Card>
      </div>
    </OnboardingLayout>
  );
}

function OnboardingLayout({ children }: { children: ReactNode }) {
  return <main className="portal-state agent-onboarding">{children}</main>;
}
