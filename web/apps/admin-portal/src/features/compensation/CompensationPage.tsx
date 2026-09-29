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
  Checkbox,
  DataTable,
  EmptyState,
  FormErrorSummary,
  InputField,
  PageHeader,
  SelectField,
  SectionHeader,
  StatusBadge,
  useConfirmation,
} from "@modular-mlm/design-system";
import { useState, type FormEvent } from "react";
import {
  CommissionPlanStatus,
  type CommissionPlanDto,
} from "@modular-mlm/contracts";
import { adminApi } from "../api/adminApi";
import { Failure, Loading, date, useAdminScope } from "../shared/AdminState";
import { commissionPlanStatus } from "../shared/status";

export function CompensationPage() {
  const api = useApiClient();
  const { confirm, prompt } = useConfirmation();
  const scope = useAdminScope();
  const plans = useApiQuery(
    (client, signal) => adminApi.plans(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const [message, setMessage] = useState("");
  const [actingAction, setActingAction] = useState<string | null>(null);
  const [editing, setEditing] = useState<CommissionPlanDto | null>(null);
  const lifecycle = useFormSubmission();
  if (plans.isLoading) return <Loading />;
  if (plans.error) return <Failure error={plans.error} retry={plans.reload} />;
  const action = async (
    id: string,
    name: string,
    type: "publish" | "retire",
  ) => {
    const retirementReason =
      type === "retire"
        ? await prompt({
            title: "Reason for retiring this plan",
            description:
              "This explanation is retained in the audit trail and should describe the business decision.",
            label: "Retirement reason",
            submitLabel: "Continue",
          })
        : null;
    if (type === "retire" && !retirementReason) return;
    if (
      !(await confirm({
        title: `${type} compensation plan?`,
        description: `${type} compensation plan ${name}? This changes commission processing for the organization.`,
        confirmLabel: `${type} plan`,
      }))
    )
      return;
    setActingAction(`${id}:${type}`);
    const completed = await lifecycle.submit(
      () =>
        adminApi.planAction(
          api,
          scope.organizationId,
          id,
          type,
          type === "retire"
            ? {
                effectiveTo: new Date().toISOString(),
                reason: retirementReason,
              }
            : undefined,
        ),
      `${name} ${type} completed.`,
    );
    setActingAction(null);
    if (!completed) return;
    setMessage(`${name} ${type} completed.`);
    plans.reload();
  };
  return (
    <div className="content-stack">
      <PageHeader
        eyebrow="Compensation"
        title="Compensation plans"
        description="Plan rules and caps remain server-evaluated. Publishing and retiring require confirmation."
      />
      {message && <Alert title={message} tone="success" />}
      <FormErrorSummary
        errors={lifecycle.fieldErrors}
        generalErrors={lifecycle.formErrors}
        id={lifecycle.errorSummaryId}
      />
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
            {
              key: "status",
              header: "Status",
              cell: (row) => {
                const status = commissionPlanStatus(row.status);
                return <StatusBadge label={status.label} tone={status.tone} />;
              },
            },
            {
              key: "effective",
              header: "Effective",
              cell: (row) => date(row.effectiveFrom),
            },
            {
              key: "direct",
              header: "Direct rate",
              cell: (row) => `${row.directSalesRate * 100}%`,
            },
            {
              key: "binary",
              header: "Binary",
              cell: (row) =>
                row.binaryPairingEnabled
                  ? `${(row.binaryPairingRate ?? 0) * 100}%`
                  : "Disabled",
            },
            {
              key: "actions",
              header: "Actions",
              cell: (row) => (
                <div className="button-cluster">
                  {row.status === CommissionPlanStatus.draft && (
                    <>
                      <Button
                        size="sm"
                        variant="secondary"
                        onClick={() => setEditing(row)}
                      >
                        Edit
                      </Button>
                      <Button
                        size="sm"
                        disabled={lifecycle.isSubmitting}
                        isLoading={actingAction === `${row.id}:publish`}
                        onClick={() => void action(row.id, row.name, "publish")}
                      >
                        Publish
                      </Button>
                    </>
                  )}
                  {row.status === CommissionPlanStatus.active && (
                    <Button
                      size="sm"
                      variant="danger"
                      disabled={lifecycle.isSubmitting}
                      isLoading={actingAction === `${row.id}:retire`}
                      onClick={() => void action(row.id, row.name, "retire")}
                    >
                      Retire
                    </Button>
                  )}
                </div>
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
          const directSalesEnabled = data.get("directSalesEnabled") === "on";
          const binaryPairingEnabled =
            data.get("binaryPairingEnabled") === "on";
          const pairingCalculationType = Number(
            data.get("pairingCalculationType"),
          );
          const saved = await feedback.submit(
            () =>
              adminApi.updatePlan(api, scope.organizationId, plan.id, {
                directSalesEnabled,
                directSalesRate: directSalesEnabled
                  ? Number(data.get("directSalesRate"))
                  : 0,
                binaryPairingEnabled,
                pairingCalculationType,
                binaryPairingRate:
                  binaryPairingEnabled && pairingCalculationType === 0
                    ? Number(data.get("binaryPairingRate")) || null
                    : null,
                pairUnitBv:
                  binaryPairingEnabled && pairingCalculationType === 1
                    ? Number(data.get("pairUnitBv")) || null
                    : null,
                fixedPairAmount:
                  binaryPairingEnabled && pairingCalculationType === 1
                    ? Number(data.get("fixedPairAmount")) || null
                    : null,
                processingFrequency: Number(data.get("processingFrequency")),
                carryForwardEnabled: data.get("carryForwardEnabled") === "on",
                qualificationRulesJson: qualificationRulesJson(data),
                capRulesJson: capRulesJson(data),
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
        <Checkbox
          name="directSalesEnabled"
          label="Enable direct sales commissions"
          defaultChecked={plan.directSalesRate > 0}
        />
        <InputField
          name="directSalesRate"
          label="Direct sales rate"
          type="number"
          min={0}
          step="0.000001"
          defaultValue={plan.directSalesRate}
          required
        />
        <Checkbox
          name="binaryPairingEnabled"
          label="Enable binary pairing commissions"
          defaultChecked={plan.binaryPairingEnabled}
        />
        <SelectField
          name="pairingCalculationType"
          label="Pairing calculation type"
          defaultValue={plan.pairingCalculationType}
          required
        >
          <option value="0">Percentage of matched volume</option>
          <option value="1">Fixed amount per BV pair</option>
        </SelectField>
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
        <SelectField
          name="processingFrequency"
          label="Processing frequency"
          defaultValue={plan.processingFrequency}
          required
        >
          <option value="0">Daily</option>
          <option value="1">Weekly</option>
          <option value="2">Monthly</option>
        </SelectField>
        <Checkbox
          name="carryForwardEnabled"
          label="Carry unmatched volume forward"
          defaultChecked={plan.carryForwardEnabled}
        />
        <CommissionRulesFields
          qualification={readQualificationRules(plan.qualificationRulesJson)}
          cap={readCapRules(plan.capRulesJson)}
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
  const [formVersion, setFormVersion] = useState(0);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const binaryPairingEnabled = data.get("binaryPairingEnabled") === "on";
    const pairingCalculationType = Number(data.get("pairingCalculationType"));
    const saved = await feedback.submit(
      () =>
        adminApi.createPlan(api, scope.organizationId, {
          name: String(data.get("name")),
          version: Number(data.get("version")),
          effectiveFrom: new Date(
            String(data.get("effectiveFrom")),
          ).toISOString(),
          directSalesRate: Number(data.get("directSalesRate")),
          binaryPairingEnabled,
          binaryPairingRate:
            binaryPairingEnabled && pairingCalculationType === 0
              ? Number(data.get("binaryPairingRate")) || null
              : null,
          processingFrequency: Number(data.get("processingFrequency")),
          pairingCalculationType,
          pairUnitBv:
            binaryPairingEnabled && pairingCalculationType === 1
              ? Number(data.get("pairUnitBv")) || null
              : null,
          fixedPairAmount:
            binaryPairingEnabled && pairingCalculationType === 1
              ? Number(data.get("fixedPairAmount")) || null
              : null,
          carryForwardEnabled: data.get("carryForwardEnabled") === "on",
          qualificationRulesJson: qualificationRulesJson(data),
          capRulesJson: capRulesJson(data),
        }),
      "Commission plan created.",
    );
    if (saved) {
      form.reset();
      setFormVersion((value) => value + 1);
      onSaved();
    }
  }
  return (
    <Card>
      <details>
        <summary>
          <strong>Create plan</strong>
        </summary>
        <form key={formVersion} className="form-grid" onSubmit={submit}>
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
          <Checkbox
            name="binaryPairingEnabled"
            label="Enable binary pairing commissions"
          />
          <InputField
            name="binaryPairingRate"
            label="Binary pairing rate"
            type="number"
            min={0}
            step="0.01"
          />
          <SelectField
            name="processingFrequency"
            label="Processing frequency"
            defaultValue={0}
          >
            <option value="0">Daily</option>
            <option value="1">Weekly</option>
            <option value="2">Monthly</option>
          </SelectField>
          <SelectField
            name="pairingCalculationType"
            label="Pairing calculation type"
            defaultValue={0}
          >
            <option value="0">Percentage of matched volume</option>
            <option value="1">Fixed amount per BV pair</option>
          </SelectField>
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
          <Checkbox
            name="carryForwardEnabled"
            label="Carry unmatched volume forward"
            defaultChecked
          />
          <CommissionRulesFields
            qualification={defaultQualificationRules}
            cap={defaultCapRules}
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

interface QualificationRulesForm {
  requireActiveAgent: boolean;
  requiredQualificationState: string;
  minimumPersonalSales: number;
  minimumPersonalBusinessVolume: number;
  minimumActiveDirectRecruits: number;
  requireActiveLeftLeg: boolean;
  requireActiveRightLeg: boolean;
}

interface CapRulesForm {
  enabled: boolean;
  maximumAmount: number;
}

const defaultQualificationRules: QualificationRulesForm = {
  requireActiveAgent: false,
  requiredQualificationState: "",
  minimumPersonalSales: 0,
  minimumPersonalBusinessVolume: 0,
  minimumActiveDirectRecruits: 0,
  requireActiveLeftLeg: false,
  requireActiveRightLeg: false,
};

const defaultCapRules: CapRulesForm = { enabled: false, maximumAmount: 0 };

function CommissionRulesFields({
  qualification,
  cap,
}: {
  qualification: QualificationRulesForm;
  cap: CapRulesForm;
}) {
  const [capEnabled, setCapEnabled] = useState(cap.enabled);
  return (
    <>
      <section className="form-section form-grid">
        <SectionHeader
          title="Pairing qualification"
          description="Set the requirements an agent must satisfy before receiving binary-pairing commission."
        />
        <Checkbox
          name="requireActiveAgent"
          label="Require an active agent account"
          defaultChecked={qualification.requireActiveAgent}
        />
        <InputField
          name="requiredQualificationState"
          label="Required qualification state"
          hint="Optional. Leave blank when no named qualification state is required."
          defaultValue={qualification.requiredQualificationState}
          maxLength={100}
        />
        <InputField
          name="minimumPersonalSales"
          label="Minimum personal sales"
          type="number"
          min={0}
          step="0.01"
          defaultValue={qualification.minimumPersonalSales}
          required
        />
        <InputField
          name="minimumPersonalBusinessVolume"
          label="Minimum personal business volume"
          type="number"
          min={0}
          step="0.01"
          defaultValue={qualification.minimumPersonalBusinessVolume}
          required
        />
        <InputField
          name="minimumActiveDirectRecruits"
          label="Minimum active direct recruits"
          type="number"
          min={0}
          step={1}
          defaultValue={qualification.minimumActiveDirectRecruits}
          required
        />
        <Checkbox
          name="requireActiveLeftLeg"
          label="Require an active left leg"
          defaultChecked={qualification.requireActiveLeftLeg}
        />
        <Checkbox
          name="requireActiveRightLeg"
          label="Require an active right leg"
          defaultChecked={qualification.requireActiveRightLeg}
        />
      </section>
      <section className="form-section form-grid">
        <SectionHeader
          title="Pairing commission cap"
          description="Optionally limit the commission produced by one pairing run."
        />
        <Checkbox
          name="capEnabled"
          label="Enable a maximum pairing commission"
          checked={capEnabled}
          onChange={(event) => setCapEnabled(event.currentTarget.checked)}
        />
        <InputField
          name="maximumCapAmount"
          label="Maximum commission amount"
          type="number"
          min={0.01}
          step="0.01"
          defaultValue={cap.maximumAmount || ""}
          disabled={!capEnabled}
          required={capEnabled}
        />
      </section>
    </>
  );
}

export function qualificationRulesJson(data: FormData): string {
  return JSON.stringify({
    requireActiveAgent: data.get("requireActiveAgent") === "on",
    requiredQualificationState:
      String(data.get("requiredQualificationState") ?? "").trim() || null,
    minimumPersonalSales: Number(data.get("minimumPersonalSales")),
    minimumPersonalBusinessVolume: Number(
      data.get("minimumPersonalBusinessVolume"),
    ),
    minimumActiveDirectRecruits: Number(
      data.get("minimumActiveDirectRecruits"),
    ),
    requireActiveLeftLeg: data.get("requireActiveLeftLeg") === "on",
    requireActiveRightLeg: data.get("requireActiveRightLeg") === "on",
  });
}

export function capRulesJson(data: FormData): string {
  const enabled = data.get("capEnabled") === "on";
  return JSON.stringify({
    enabled,
    maximumAmount: enabled ? Number(data.get("maximumCapAmount")) : 0,
  });
}

export function readQualificationRules(json: string): QualificationRulesForm {
  if (!json.trim() || json.trim() === "[]" || json.trim() === "{}")
    return { ...defaultQualificationRules };
  try {
    const value = normalizeRuleKeys(
      JSON.parse(json),
    ) as Partial<QualificationRulesForm>;
    return {
      requireActiveAgent: Boolean(value.requireActiveAgent),
      requiredQualificationState: value.requiredQualificationState ?? "",
      minimumPersonalSales: Number(value.minimumPersonalSales ?? 0),
      minimumPersonalBusinessVolume: Number(
        value.minimumPersonalBusinessVolume ?? 0,
      ),
      minimumActiveDirectRecruits: Number(
        value.minimumActiveDirectRecruits ?? 0,
      ),
      requireActiveLeftLeg: Boolean(value.requireActiveLeftLeg),
      requireActiveRightLeg: Boolean(value.requireActiveRightLeg),
    };
  } catch {
    return { ...defaultQualificationRules };
  }
}

export function readCapRules(json: string): CapRulesForm {
  if (!json.trim() || json.trim() === "[]" || json.trim() === "{}")
    return { ...defaultCapRules };
  try {
    const value = normalizeRuleKeys(JSON.parse(json)) as Partial<CapRulesForm>;
    return {
      enabled: Boolean(value.enabled),
      maximumAmount: Number(value.maximumAmount ?? 0),
    };
  } catch {
    return { ...defaultCapRules };
  }
}

function normalizeRuleKeys(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== "object" || Array.isArray(value)) return {};
  return Object.fromEntries(
    Object.entries(value).map(([key, entry]) => [
      `${key.charAt(0).toLowerCase()}${key.slice(1)}`,
      entry,
    ]),
  );
}
