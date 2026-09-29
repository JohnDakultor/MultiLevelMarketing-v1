"use client";
import {
  useApiClient,
  useApiQuery,
  useFormSubmission,
} from "@modular-mlm/api-client";
import { AgentStatus } from "@modular-mlm/contracts";
import {
  Alert,
  Button,
  Card,
  EmptyState,
  FormErrorSummary,
  PageHeader,
  SelectField,
} from "@modular-mlm/design-system";
import { useState } from "react";
import { agentApi } from "../api/agentApi";
import { Failure, Loading, date } from "../shared/AgentScreenState";
import { useAgentScope } from "../shared/useAgentScope";
import { AgentStatusBadge } from "../shared/status";

export function AgentProfilePage() {
  const api = useApiClient();
  const scope = useAgentScope();
  const feedback = useFormSubmission();
  const profile = useApiQuery(
    (client, signal) => agentApi.profile(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const qualification = useApiQuery(
    (client, signal) =>
      agentApi.qualification(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const [preferredLeg, setPreferredLeg] = useState("");
  const [message, setMessage] = useState("");
  if (profile.isLoading || qualification.isLoading) return <Loading />;
  const loadError = profile.error ?? qualification.error;
  if (loadError)
    return (
      <Failure
        error={loadError}
        retry={() => {
          profile.reload();
          qualification.reload();
        }}
      />
    );
  if (!profile.data)
    return (
      <EmptyState
        title="Agent profile unavailable"
        description="No active Agent profile is associated with this account in the current organization."
      />
    );
  return (
    <div className="content-stack">
      <PageHeader
        title="Agent profile"
        description="Identity fields are read-only because the backend exposes only preferred-leg editing."
      />
      {message && <Alert title={message} tone="success" />}
      {profile.data.status !== AgentStatus.active && (
        <Alert title="Profile actions are restricted" tone="warning">
          Placement preferences can be changed only while the Agent account is
          active.
        </Alert>
      )}
      <div className="detail-grid">
        <Card>
          <h2>{profile.data.displayName}</h2>
          <dl className="definition-list">
            <dt>Agent code</dt>
            <dd>{profile.data.agentCode}</dd>
            <dt>Referral code</dt>
            <dd>{profile.data.referralCode}</dd>
            <dt>Email</dt>
            <dd>{profile.data.email}</dd>
            <dt>Status</dt>
            <dd>
              <AgentStatusBadge value={profile.data.status} />
            </dd>
            <dt>Joined</dt>
            <dd>{date(profile.data.joinedAt)}</dd>
            <dt>Sponsor</dt>
            <dd>{profile.data.sponsorAgentCode ?? "None"}</dd>
            <dt>Placement parent</dt>
            <dd>{profile.data.placementParentAgentCode ?? "Not placed"}</dd>
          </dl>
        </Card>
        <Card>
          <h2>Placement preference</h2>
          <FormErrorSummary
            errors={feedback.fieldErrors}
            generalErrors={feedback.formErrors}
            id={feedback.errorSummaryId}
          />
          <SelectField
            id="preferred-leg"
            label="Preferred leg"
            value={preferredLeg || String(profile.data.preferredLeg ?? 0)}
            onChange={(event) => setPreferredLeg(event.target.value)}
          >
            <option value="0">Left</option>
            <option value="1">Right</option>
          </SelectField>
          <Button
            isLoading={feedback.isSubmitting}
            disabled={
              feedback.isSubmitting ||
              profile.data.status !== AgentStatus.active
            }
            onClick={async () => {
              const saved = await feedback.submit(
                () =>
                  agentApi.setPreferredLeg(
                    api,
                    scope.organizationId,
                    Number(preferredLeg || profile.data!.preferredLeg || 0),
                  ),
                "Preferred leg updated.",
              );
              if (saved) {
                setMessage("Preferred leg updated.");
                profile.reload();
              }
            }}
          >
            Save preference
          </Button>
        </Card>
      </div>
      {qualification.data && (
        <Card>
          <h2>Qualification</h2>
          <p>{qualification.data.state}</p>
          {qualification.data.failures.map((failure) => (
            <Alert key={failure.code} title={failure.code} tone="warning">
              {failure.message}
            </Alert>
          ))}
        </Card>
      )}
    </div>
  );
}
