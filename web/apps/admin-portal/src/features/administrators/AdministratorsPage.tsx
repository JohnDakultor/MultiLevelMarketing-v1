"use client";
import {
  useApiClient,
  useApiQuery,
  useFormSubmission,
} from "@modular-mlm/api-client";
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  FormErrorSummary,
  InputField,
  PageHeader,
  useConfirmation,
} from "@modular-mlm/design-system";
import { useState, type FormEvent } from "react";
import { adminApi } from "../api/adminApi";
import { Failure, Loading, date, useAdminScope } from "../shared/AdminState";

export function AdministratorsPage() {
  const api = useApiClient();
  const { confirm, prompt } = useConfirmation();
  const inviteFeedback = useFormSubmission();
  const scope = useAdminScope();
  const admins = useApiQuery(
    (client, signal) =>
      adminApi.administrators(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const invitations = useApiQuery(
    (client, signal) =>
      adminApi.invitations(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const [message, setMessage] = useState("");
  if (admins.isLoading || invitations.isLoading) return <Loading />;
  if (admins.error)
    return <Failure error={admins.error} retry={admins.reload} />;
  return (
    <div className="content-stack">
      <PageHeader
        title="Administrators"
        description="Invite administrators and revoke access within this organization."
      />
      {message && <Alert title={message} tone="info" />}
      <Card>
        <h2>Invite administrator</h2>
        <form
          className="inline-form"
          onSubmit={async (event: FormEvent<HTMLFormElement>) => {
            event.preventDefault();
            const form = event.currentTarget;
            const data = new FormData(form);
            const sent = await inviteFeedback.submit(
              () =>
                adminApi.invite(
                  api,
                  scope.organizationId,
                  String(data.get("email")),
                ),
              "Invitation queued for delivery.",
            );
            if (sent) {
              form.reset();
              setMessage("Invitation queued for delivery.");
              invitations.reload();
            }
          }}
        >
          <FormErrorSummary
            errors={inviteFeedback.fieldErrors}
            generalErrors={inviteFeedback.formErrors}
            id={inviteFeedback.errorSummaryId}
          />
          <InputField
            name="email"
            label="Email address"
            type="email"
            required
            error={inviteFeedback.fieldError("email")}
          />
          <Button
            type="submit"
            isLoading={inviteFeedback.isSubmitting}
            disabled={inviteFeedback.isSubmitting}
          >
            Send invitation
          </Button>
        </form>
      </Card>
      <h2>Current administrators</h2>
      <DataTable
        caption="Administrators"
        rows={admins.data ?? []}
        rowKey={(row) => row.id}
        columns={[
          {
            key: "name",
            header: "Administrator",
            cell: (row) => (
              <>
                {row.displayName}
                <br />
                <small>{row.email}</small>
              </>
            ),
          },
          {
            key: "confirmed",
            header: "Email",
            cell: (row) => (row.emailConfirmed ? "Confirmed" : "Unconfirmed"),
          },
          {
            key: "action",
            header: "",
            cell: (row) => (
              <Button
                variant="danger"
                onClick={async () => {
                  const value = await prompt({
                    title: "Reason for revocation",
                    description:
                      "This reason is recorded with the administrator access change.",
                    label: "Revocation reason",
                    submitLabel: "Continue",
                  });
                  if (!value) return;
                  if (
                    !(await confirm({
                      title: "Revoke administrator access?",
                      description: `Revoke the administrator role from ${row.email}? They will immediately lose organization administration access.`,
                      confirmLabel: "Revoke access",
                    }))
                  )
                    return;
                  await adminApi.revokeAdministrator(
                    api,
                    scope.organizationId,
                    row.id,
                    value,
                  );
                  setMessage("Administrator role revoked.");
                  admins.reload();
                }}
              >
                Revoke role
              </Button>
            ),
          },
        ]}
      />
      <h2>Pending invitations</h2>
      {!invitations.data?.length ? (
        <EmptyState
          title="No pending invitations"
          description="New invitations will appear here until accepted, expired, or revoked."
        />
      ) : (
        <DataTable
          caption="Administrator invitations"
          rows={invitations.data}
          rowKey={(row) => row.id}
          columns={[
            { key: "email", header: "Email", cell: (row) => row.email },
            {
              key: "invited",
              header: "Invited",
              cell: (row) => date(row.invitedAt),
            },
            {
              key: "expires",
              header: "Expires",
              cell: (row) => date(row.expiresAt),
            },
            { key: "status", header: "Status", cell: (row) => row.status },
            {
              key: "action",
              header: "",
              cell: (row) => (
                <Button
                  variant="danger"
                  onClick={async () => {
                    const value = await prompt({
                      title: "Reason for revocation",
                      description:
                        "This reason is recorded with the invitation revocation.",
                      label: "Revocation reason",
                      submitLabel: "Revoke invitation",
                    });
                    if (!value) return;
                    await adminApi.revokeInvitation(
                      api,
                      scope.organizationId,
                      row.id,
                      value,
                    );
                    setMessage("Invitation revoked.");
                    invitations.reload();
                  }}
                >
                  Revoke
                </Button>
              ),
            },
          ]}
        />
      )}
    </div>
  );
}
