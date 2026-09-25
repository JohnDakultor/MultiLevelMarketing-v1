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
  EmptyState,
  FormErrorSummary,
  InputField,
  PageHeader,
  SelectField,
  useConfirmation,
} from "@modular-mlm/design-system";
import { useState, type FormEvent, type ReactNode } from "react";
import { adminApi } from "../api/adminApi";
import { Failure, Loading, date, useAdminScope } from "../shared/AdminState";

export function OrganizationSettingsPage() {
  const api = useApiClient();
  const { confirm } = useConfirmation();
  const scope = useAdminScope();
  const settings = useApiQuery(
    (client, signal) => adminApi.settings(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const wallet = useApiQuery(
    (client, signal) =>
      adminApi.walletSettings(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const [message, setMessage] = useState("");
  const domainFeedback = useFormSubmission();
  if (settings.isLoading || wallet.isLoading) return <Loading />;
  if (settings.error)
    return <Failure error={settings.error} retry={settings.reload} />;
  if (!settings.data)
    return (
      <EmptyState
        title="Organization settings unavailable"
        description="The organization could not be found or is no longer available to this administrator."
      />
    );
  const save = async (operation: () => Promise<unknown>, success: string) => {
    try {
      await operation();
      setMessage(success);
      settings.reload();
      wallet.reload();
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "The setting could not be saved.",
      );
      throw error;
    }
  };
  return (
    <div className="content-stack">
      <PageHeader
        title="Organization settings"
        description="All changes are scoped to the organization resolved by this portal."
      />
      {message && <Alert title={message} tone="info" />}
      <ReferralSettingsCard />
      <div className="detail-grid">
        <SettingsForm
          title="Profile"
          submit={(data) =>
            save(
              () =>
                adminApi.updateProfile(api, scope.organizationId, {
                  name: text(data, "name"),
                  currencyCode: text(data, "currencyCode"),
                  timeZone: text(data, "timeZone"),
                  locale: text(data, "locale"),
                }),
              "Profile updated.",
            )
          }
        >
          <InputField
            name="name"
            label="Organization name"
            required
            defaultValue={settings.data.profile.name}
          />
          <InputField
            name="currencyCode"
            label="Currency code"
            required
            maxLength={3}
            defaultValue={settings.data.profile.currencyCode}
          />
          <InputField
            name="timeZone"
            label="Time zone"
            required
            defaultValue={settings.data.profile.timeZone}
          />
          <InputField
            name="locale"
            label="Locale"
            required
            defaultValue={settings.data.profile.locale}
          />
        </SettingsForm>
        <SettingsForm
          title="Store branding"
          submit={(data) =>
            save(
              () =>
                adminApi.updateBranding(api, scope.organizationId, {
                  storeTitle: text(data, "storeTitle"),
                  supportEmail: text(data, "supportEmail"),
                  primaryColor: text(data, "primaryColor"),
                  secondaryColor: text(data, "secondaryColor"),
                  accentColor: text(data, "accentColor"),
                  logoUrl: optional(data, "logoUrl"),
                  faviconUrl: optional(data, "faviconUrl"),
                  supportPhone: optional(data, "supportPhone"),
                  footerText: optional(data, "footerText"),
                }),
              "Branding draft updated.",
            )
          }
        >
          <InputField
            name="storeTitle"
            label="Store title"
            required
            defaultValue={settings.data.branding.storeTitle}
          />
          <InputField
            name="supportEmail"
            label="Support email"
            type="email"
            required
            defaultValue={settings.data.branding.supportEmail}
          />
          <InputField
            name="primaryColor"
            label="Primary color"
            type="color"
            defaultValue={settings.data.branding.primaryColor}
          />
          <InputField
            name="secondaryColor"
            label="Secondary color"
            type="color"
            defaultValue={settings.data.branding.secondaryColor}
          />
          <InputField
            name="accentColor"
            label="Accent color"
            type="color"
            defaultValue={settings.data.branding.accentColor}
          />
          <InputField
            name="logoUrl"
            label="Logo URL"
            type="url"
            defaultValue={settings.data.branding.logoUrl ?? ""}
          />
          <InputField
            name="faviconUrl"
            label="Favicon URL"
            type="url"
            defaultValue={settings.data.branding.faviconUrl ?? ""}
          />
          <InputField
            name="supportPhone"
            label="Support phone"
            defaultValue={settings.data.branding.supportPhone ?? ""}
          />
          <InputField
            name="footerText"
            label="Footer text"
            defaultValue={settings.data.branding.footerText ?? ""}
          />
          <Button
            type="button"
            variant="secondary"
            onClick={() =>
              void save(
                () => adminApi.publishBranding(api, scope.organizationId),
                "Branding published.",
              )
            }
          >
            Publish branding
          </Button>
        </SettingsForm>
        <SettingsForm
          title="Commerce"
          submit={(data) =>
            save(
              () =>
                adminApi.updateCommerce(api, scope.organizationId, {
                  allowGuestCheckout: checked(data, "allowGuestCheckout"),
                  requireShippingAddress: checked(
                    data,
                    "requireShippingAddress",
                  ),
                  requireBillingAddress: checked(data, "requireBillingAddress"),
                  inventoryReservationMinutes: number(
                    data,
                    "inventoryReservationMinutes",
                  ),
                }),
              "Commerce settings updated.",
            )
          }
        >
          <Checkbox
            name="allowGuestCheckout"
            label="Allow guest checkout"
            defaultChecked={settings.data.commerce.allowGuestCheckout}
          />
          <Checkbox
            name="requireShippingAddress"
            label="Require shipping address"
            defaultChecked={settings.data.commerce.requireShippingAddress}
          />
          <Checkbox
            name="requireBillingAddress"
            label="Require billing address"
            defaultChecked={settings.data.commerce.requireBillingAddress}
          />
          <InputField
            name="inventoryReservationMinutes"
            label="Inventory reservation minutes"
            type="number"
            min={1}
            required
            defaultValue={settings.data.commerce.inventoryReservationMinutes}
          />
        </SettingsForm>
        <SettingsForm
          title="Features"
          submit={(data) =>
            save(
              () =>
                adminApi.updateFeatures(api, scope.organizationId, {
                  commerceEnabled: checked(data, "commerceEnabled"),
                  agentProgramEnabled: checked(data, "agentProgramEnabled"),
                  binaryNetworkEnabled: checked(data, "binaryNetworkEnabled"),
                  binaryPairingEnabled: checked(data, "binaryPairingEnabled"),
                  walletEnabled: checked(data, "walletEnabled"),
                  payoutEnabled: checked(data, "payoutEnabled"),
                }),
              "Features updated.",
            )
          }
        >
          {(
            [
              "commerceEnabled",
              "agentProgramEnabled",
              "binaryNetworkEnabled",
              "binaryPairingEnabled",
              "walletEnabled",
              "payoutEnabled",
            ] as const
          ).map((name) => (
            <Checkbox
              key={name}
              name={name}
              label={label(name)}
              defaultChecked={settings.data!.features[name]}
            />
          ))}
        </SettingsForm>
        <SettingsForm
          title="Network"
          submit={(data) =>
            save(
              () =>
                adminApi.updateNetwork(api, scope.organizationId, {
                  defaultPlacementStrategy: number(
                    data,
                    "defaultPlacementStrategy",
                  ),
                  allowAgentPreferredLeg: checked(
                    data,
                    "allowAgentPreferredLeg",
                  ),
                  maxQueryDepth: number(data, "maxQueryDepth"),
                  autoPlacementEnabled: checked(data, "autoPlacementEnabled"),
                  restrictPlacementChangesAfterActivation: checked(
                    data,
                    "restrictPlacementChangesAfterActivation",
                  ),
                }),
              "Network settings updated.",
            )
          }
        >
          <SelectField
            name="defaultPlacementStrategy"
            label="Default placement strategy"
            defaultValue={settings.data.network.defaultPlacementStrategy}
          >
            <option value="0">Breadth first</option>
            <option value="1">Balanced leg</option>
          </SelectField>
          <Checkbox
            name="allowAgentPreferredLeg"
            label="Allow Agents to choose a preferred leg"
            defaultChecked={settings.data.network.allowAgentPreferredLeg}
          />
          <InputField
            name="maxQueryDepth"
            label="Maximum network query depth"
            type="number"
            min={1}
            max={100}
            defaultValue={settings.data.network.maxQueryDepth}
            required
          />
          <Checkbox
            name="autoPlacementEnabled"
            label="Enable automatic placement"
            defaultChecked={settings.data.network.autoPlacementEnabled}
          />
          <Checkbox
            name="restrictPlacementChangesAfterActivation"
            label="Restrict placement changes after activation"
            defaultChecked={
              settings.data.network.restrictPlacementChangesAfterActivation
            }
          />
        </SettingsForm>
        {wallet.data && (
          <SettingsForm
            title="Wallet availability"
            submit={(data) =>
              save(
                () =>
                  adminApi.updateWallet(api, scope.organizationId, {
                    commissionReleaseTrigger: number(
                      data,
                      "commissionReleaseTrigger",
                    ),
                    releaseDelayDays: number(data, "releaseDelayDays"),
                    returnWindowDays: number(data, "returnWindowDays"),
                    minimumPayoutAmount: number(data, "minimumPayoutAmount"),
                    allowNegativeRecoverableBalance: checked(
                      data,
                      "allowNegativeRecoverableBalance",
                    ),
                    maximumNegativeBalance: number(
                      data,
                      "maximumNegativeBalance",
                    ),
                  }),
                "Wallet settings updated.",
              )
            }
          >
            <InputField
              name="commissionReleaseTrigger"
              label="Release trigger"
              type="number"
              defaultValue={wallet.data.commissionReleaseTrigger}
            />
            <InputField
              name="releaseDelayDays"
              label="Release delay days"
              type="number"
              min={0}
              defaultValue={wallet.data.releaseDelayDays}
            />
            <InputField
              name="returnWindowDays"
              label="Return window days"
              type="number"
              min={0}
              defaultValue={wallet.data.returnWindowDays}
            />
            <InputField
              name="minimumPayoutAmount"
              label="Minimum payout"
              type="number"
              min={0}
              step="0.01"
              defaultValue={wallet.data.minimumPayoutAmount}
            />
            <Checkbox
              name="allowNegativeRecoverableBalance"
              label="Allow recoverable negative balance"
              defaultChecked={wallet.data.allowNegativeRecoverableBalance}
            />
            <InputField
              name="maximumNegativeBalance"
              label="Maximum negative balance"
              type="number"
              min={0}
              step="0.01"
              defaultValue={wallet.data.maximumNegativeBalance}
            />
          </SettingsForm>
        )}
      </div>
      <Card>
        <h2>Domains</h2>
        {settings.data.domains.map((domain) => (
          <div key={domain.id} className="action-row">
            <span>
              <strong>{domain.hostName}</strong>
              <br />
              {domain.isPrimary ? "Primary" : "Secondary"} ·{" "}
              {domain.isVerified
                ? `Verified ${date(domain.verifiedAt)}`
                : "Pending verification"}
            </span>
            <Button
              variant="danger"
              onClick={async () => {
                if (
                  !(await confirm({
                    title: "Remove organization domain?",
                    description: `Remove ${domain.hostName}? Requests using that hostname will stop resolving this organization.`,
                    confirmLabel: "Remove domain",
                  }))
                )
                  return;
                await save(
                  () =>
                    adminApi.removeDomain(api, scope.organizationId, domain.id),
                  "Domain removed.",
                );
              }}
            >
              Remove
            </Button>
          </div>
        ))}
        <form
          className="inline-form"
          onSubmit={async (event) => {
            event.preventDefault();
            const form = event.currentTarget;
            const data = new FormData(form);
            const saved = await domainFeedback.submit(
              () =>
                save(
                  () =>
                    adminApi.configureDomain(
                      api,
                      scope.organizationId,
                      text(data, "hostName"),
                      checked(data, "makePrimary"),
                    ),
                  "Domain configured.",
                ),
              "Domain configured.",
            );
            if (saved) form.reset();
          }}
        >
          <FormErrorSummary
            errors={domainFeedback.fieldErrors}
            generalErrors={domainFeedback.formErrors}
            id={domainFeedback.errorSummaryId}
          />
          <InputField
            name="hostName"
            label="Domain hostname"
            required
            placeholder="shop.example.com"
            error={domainFeedback.fieldError("hostName")}
          />
          <Checkbox name="makePrimary" label="Make primary" />
          <Button
            type="submit"
            isLoading={domainFeedback.isSubmitting}
            disabled={domainFeedback.isSubmitting}
          >
            Add domain
          </Button>
        </form>
      </Card>
    </div>
  );
}

function ReferralSettingsCard() {
  const api = useApiClient();
  const scope = useAdminScope();
  const feedback = useFormSubmission();
  const settings = useApiQuery(
    (client, signal) => adminApi.referralSettings(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  if (settings.isLoading) return <Loading />;
  if (settings.error) return <Failure error={settings.error} retry={settings.reload} />;
  if (!settings.data) return null;
  return (
    <Card>
      <h2>Referral settings</h2>
      <form className="form-grid" onSubmit={async (event) => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        const saved = await feedback.submit(() => adminApi.updateReferralSettings(
          api,
          scope.organizationId,
          {
            attributionWindowDays: Number(data.get("attributionWindowDays")),
            allowReferralOverride: data.get("allowReferralOverride") === "on",
            referralLockAfterFirstPurchase: data.get("referralLockAfterFirstPurchase") === "on",
          },
        ), "Referral settings saved.");
        if (saved) settings.reload();
      }}>
        <FormErrorSummary errors={feedback.fieldErrors} generalErrors={feedback.formErrors} id={feedback.errorSummaryId} />
        <InputField name="attributionWindowDays" label="Attribution window (days)" type="number" min={1} max={365} defaultValue={settings.data.attributionWindowDays} required />
        <Checkbox name="allowReferralOverride" label="Allow referral override" defaultChecked={settings.data.allowReferralOverride} />
        <Checkbox name="referralLockAfterFirstPurchase" label="Lock referral after first purchase" defaultChecked={settings.data.referralLockAfterFirstPurchase} />
        <Button type="submit" isLoading={feedback.isSubmitting}>Save referral settings</Button>
      </form>
    </Card>
  );
}

function SettingsForm({
  title,
  children,
  submit,
}: {
  title: string;
  children: ReactNode;
  submit(data: FormData): Promise<void>;
}) {
  const feedback = useFormSubmission();
  return (
    <Card>
      <h2>{title}</h2>
      <form
        className="form-grid"
        onSubmit={async (event: FormEvent<HTMLFormElement>) => {
          event.preventDefault();
          await feedback.submit(
            () => submit(new FormData(event.currentTarget)),
            `${title} settings saved.`,
          );
        }}
      >
        <FormErrorSummary
          errors={feedback.fieldErrors}
          generalErrors={feedback.formErrors}
          id={feedback.errorSummaryId}
        />
        {children}
        <Button
          type="submit"
          isLoading={feedback.isSubmitting}
          disabled={feedback.isSubmitting}
        >
          Save {title.toLowerCase()}
        </Button>
      </form>
    </Card>
  );
}
const text = (data: FormData, key: string) =>
  String(data.get(key) ?? "").trim();
const optional = (data: FormData, key: string) => text(data, key) || null;
const number = (data: FormData, key: string) => Number(data.get(key));
const checked = (data: FormData, key: string) => data.get(key) === "on";
const label = (value: string) =>
  value
    .replace(/([A-Z])/g, " $1")
    .replace(/^./, (letter) => letter.toUpperCase());
