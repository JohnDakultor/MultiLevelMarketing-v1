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
import type { CommissionPlanDto } from "@modular-mlm/contracts";
import { adminApi } from "../api/adminApi";
import { Failure, Loading, date, useAdminScope } from "../shared/AdminState";

export function CompensationPage() {
  const api = useApiClient();
  const { confirm } = useConfirmation();
  const scope = useAdminScope();
  const plans = useApiQuery(
    (client, signal) => adminApi.plans(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const [message, setMessage] = useState("");
  const [editing, setEditing] = useState<CommissionPlanDto | null>(null);
  if (plans.isLoading) return <Loading />;
  if (plans.error) return <Failure error={plans.error} retry={plans.reload} />;
  const action = async (
    id: string,
    name: string,
    type: "publish" | "retire",
  ) => {
    if (
      !(await confirm({
        title: `${type} compensation plan?`,
        description: `${type} compensation plan ${name}? This changes commission processing for the organization.`,
        confirmLabel: `${type} plan`,
      }))
    )
      return;
    await adminApi.planAction(
      api,
      scope.organizationId,
      id,
      type,
      type === "retire"
        ? {
            effectiveTo: new Date().toISOString(),
            reason: "Retired by administrator",
          }
        : undefined,
    );
    setMessage(`${name} ${type} completed.`);
    plans.reload();
  };
  return (
    <div className="content-stack">
      <PageHeader
        title="Compensation plans"
        description="Plan rules and caps remain server-evaluated. Publishing and retiring require confirmation."
      />
      {message && <Alert title={message} tone="success" />}
      <CreatePlan
        onSaved={() => {
          setMessage("Draft compensation plan created.");
          plans.reload();
        }}
      />
      {!plans.data?.length ? (
        <EmptyState
          title="No compensation plans"
          description="Create a draft plan before publishing commission rules."
        />
      ) : (
        <DataTable
          caption="Compensation plans"
          rows={plans.data}
          rowKey={(row) => row.id}
          columns={[
            {
              key: "name",
              header: "Plan",
              cell: (row) => (
                <>
                  {row.name}
                  <br />
                  <small>Version {row.version}</small>
                </>
              ),
            },
            { key: "status", header: "Status", cell: (row) => row.status },
            {
              key: "effective",
              header: "Effective",
              cell: (row) => date(row.effectiveFrom),
            },
            {
              key: "direct",
              header: "Direct rate",
              cell: (row) => `${row.directSalesRate}%`,
            },
            {
              key: "binary",
              header: "Binary",
              cell: (row) =>
                row.binaryPairingEnabled
                  ? `${row.binaryPairingRate ?? 0}%`
                  : "Disabled",
            },
            {
              key: "actions",
              header: "Actions",
              cell: (row) => (
                <>
                  {row.status === 0 && (
                    <>
                      <Button
                        variant="secondary"
                        onClick={() => setEditing(row)}
                      >
                        Edit
                      </Button>{" "}
                    </>
                  )}
                  <Button
                    onClick={() => void action(row.id, row.name, "publish")}
                  >
                    Publish
                  </Button>{" "}
                  <Button
                    variant="danger"
                    onClick={() => void action(row.id, row.name, "retire")}
                  >
                    Retire
                  </Button>
                </>
              ),
            },
          ]}
        />
      )}
      {editing && (
        <EditPlan
          plan={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            setMessage("Draft compensation plan updated.");
            plans.reload();
          }}
        />
      )}
    </div>
  );
}

function EditPlan({
  plan,
  onClose,
  onSaved,
}: {
  plan: CommissionPlanDto;
  onClose(): void;
  onSaved(): void;
}) {
  const api = useApiClient();
  const scope = useAdminScope();
  const feedback = useFormSubmission();
  return (
    <Card>
      <div className="action-row">
        <h2>Edit {plan.name}</h2>
        <Button variant="ghost" onClick={onClose}>
          Close
        </Button>
      </div>
      <form
        className="form-grid"
        onSubmit={async (event) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          const saved = await feedback.submit(
            () =>
              adminApi.updatePlan(api, scope.organizationId, plan.id, {
                directSalesEnabled: data.get("directSalesEnabled") === "on",
                directSalesRate: Number(data.get("directSalesRate")),
                binaryPairingEnabled: data.get("binaryPairingEnabled") === "on",
                pairingCalculationType: Number(
                  data.get("pairingCalculationType"),
                ),
                binaryPairingRate:
                  Number(data.get("binaryPairingRate")) || null,
                pairUnitBv: Number(data.get("pairUnitBv")) || null,
                fixedPairAmount: Number(data.get("fixedPairAmount")) || null,
                processingFrequency: Number(data.get("processingFrequency")),
                carryForwardEnabled: data.get("carryForwardEnabled") === "on",
                qualificationRulesJson: String(
                  data.get("qualificationRulesJson"),
                ),
                capRulesJson: String(data.get("capRulesJson")),
                expectedConfigurationVersion: plan.configurationVersion,
              }),
            "Commission plan updated.",
          );
          if (saved) onSaved();
        }}
      >
        <FormErrorSummary
          errors={feedback.fieldErrors}
          generalErrors={feedback.formErrors}
          id={feedback.errorSummaryId}
        />
        <label>
          <input
            name="directSalesEnabled"
            type="checkbox"
            defaultChecked={plan.directSalesRate > 0}
          />{" "}
          Enable direct sales
        </label>
        <InputField
          name="directSalesRate"
          label="Direct sales rate"
          type="number"
          min={0}
          step="0.000001"
          defaultValue={plan.directSalesRate}
          required
        />
        <label>
          <input
            name="binaryPairingEnabled"
            type="checkbox"
            defaultChecked={plan.binaryPairingEnabled}
          />{" "}
          Enable binary pairing
        </label>
        <InputField
          name="pairingCalculationType"
          label="Pairing calculation type"
          type="number"
          min={0}
          defaultValue={plan.pairingCalculationType}
          required
        />
        <InputField
          name="binaryPairingRate"
          label="Binary pairing rate"
          type="number"
          min={0}
          step="0.000001"
          defaultValue={plan.binaryPairingRate ?? ""}
        />
        <InputField
          name="pairUnitBv"
          label="Pair unit BV"
          type="number"
          min={0}
          step="0.01"
          defaultValue={plan.pairUnitBv ?? ""}
        />
        <InputField
          name="fixedPairAmount"
          label="Fixed pair amount"
          type="number"
          min={0}
          step="0.01"
          defaultValue={plan.fixedPairAmount ?? ""}
        />
        <InputField
          name="processingFrequency"
          label="Processing frequency"
          type="number"
          min={0}
          defaultValue={plan.processingFrequency}
          required
        />
        <label>
          <input
            name="carryForwardEnabled"
            type="checkbox"
            defaultChecked={plan.carryForwardEnabled}
          />{" "}
          Carry forward
        </label>
        <InputField
          name="qualificationRulesJson"
          label="Qualification rules JSON"
          defaultValue={plan.qualificationRulesJson}
          required
        />
        <InputField
          name="capRulesJson"
          label="Cap rules JSON"
          defaultValue={plan.capRulesJson}
          required
        />
        <Button type="submit" isLoading={feedback.isSubmitting}>
          Save draft
        </Button>
      </form>
    </Card>
  );
}
function CreatePlan({ onSaved }: { onSaved(): void }) {
  const api = useApiClient();
  const scope = useAdminScope();
  const feedback = useFormSubmission();
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const saved = await feedback.submit(
      () =>
        adminApi.createPlan(api, scope.organizationId, {
          name: String(data.get("name")),
          version: Number(data.get("version")),
          effectiveFrom: new Date(
            String(data.get("effectiveFrom")),
          ).toISOString(),
          directSalesRate: Number(data.get("directSalesRate")),
          binaryPairingEnabled: data.get("binaryPairingEnabled") === "on",
          binaryPairingRate: Number(data.get("binaryPairingRate")) || null,
          processingFrequency: Number(data.get("processingFrequency")),
          pairingCalculationType: Number(data.get("pairingCalculationType")),
          pairUnitBv: Number(data.get("pairUnitBv")) || null,
          fixedPairAmount: Number(data.get("fixedPairAmount")) || null,
          carryForwardEnabled: data.get("carryForwardEnabled") === "on",
          qualificationRulesJson: String(
            data.get("qualificationRulesJson") || "{}",
          ),
          capRulesJson: String(data.get("capRulesJson") || "{}"),
        }),
      "Commission plan created.",
    );
    if (saved) {
      form.reset();
      onSaved();
    }
  }
  return (
    <Card>
      <details>
        <summary>
          <strong>Create plan</strong>
        </summary>
        <form className="form-grid" onSubmit={submit}>
          <FormErrorSummary
            errors={feedback.fieldErrors}
            generalErrors={feedback.formErrors}
            id={feedback.errorSummaryId}
          />
          <InputField
            name="name"
            label="Name"
            required
            error={feedback.fieldError("name")}
          />
          <InputField
            name="version"
            label="Version"
            type="number"
            min={1}
            required
            error={feedback.fieldError("version")}
          />
          <InputField
            name="effectiveFrom"
            label="Effective from"
            type="datetime-local"
            required
            error={feedback.fieldError("effectiveFrom")}
          />
          <InputField
            name="directSalesRate"
            label="Direct sales rate"
            type="number"
            min={0}
            step="0.01"
            required
            error={feedback.fieldError("directSalesRate")}
          />
          <label>
            <input name="binaryPairingEnabled" type="checkbox" /> Enable binary
            pairing
          </label>
          <InputField
            name="binaryPairingRate"
            label="Binary pairing rate"
            type="number"
            min={0}
            step="0.01"
          />
          <InputField
            name="processingFrequency"
            label="Processing frequency"
            type="number"
            min={0}
            defaultValue={0}
          />
          <InputField
            name="pairingCalculationType"
            label="Pairing calculation type"
            type="number"
            min={0}
            defaultValue={0}
          />
          <InputField
            name="pairUnitBv"
            label="Pair unit BV"
            type="number"
            min={0}
            step="0.01"
          />
          <InputField
            name="fixedPairAmount"
            label="Fixed pair amount"
            type="number"
            min={0}
            step="0.01"
          />
          <label>
            <input name="carryForwardEnabled" type="checkbox" defaultChecked />{" "}
            Carry forward
          </label>
          <InputField
            name="qualificationRulesJson"
            label="Qualification rules JSON"
            defaultValue="{}"
          />
          <InputField
            name="capRulesJson"
            label="Cap rules JSON"
            defaultValue="{}"
          />
          <Button
            type="submit"
            isLoading={feedback.isSubmitting}
            disabled={feedback.isSubmitting}
          >
            Create draft plan
          </Button>
        </form>
      </details>
    </Card>
  );
}
