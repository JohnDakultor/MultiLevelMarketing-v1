"use client";

import { useApiClient, useFormSubmission } from "@modular-mlm/api-client";
import {
  Alert,
  Button,
  Card,
  FormErrorSummary,
  InputField,
  PageHeader,
  TextareaField,
} from "@modular-mlm/design-system";
import { useState, type FormEvent } from "react";
import { adminApi } from "../../../../features/api/adminApi";

type ProvisioningStep =
  "idle" | "creating" | "assets" | "branding" | "publishing" | "complete";

export default function NewOrganizationPage() {
  const api = useApiClient();
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [slugWasEdited, setSlugWasEdited] = useState(false);
  const [organizationId, setOrganizationId] = useState<string | null>(null);
  const [createdSlug, setCreatedSlug] = useState<string | null>(null);
  const [step, setStep] = useState<ProvisioningStep>("idle");
  const feedback = useFormSubmission();

  async function provision(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);

    const completed = await feedback.submit(async () => {
      let id = organizationId;
      if (!id) {
        setStep("creating");
        id = await adminApi.createOrganization(api, {
          name: requiredText(form, "name"),
          slug: requiredText(form, "slug"),
          currencyCode: requiredText(form, "currencyCode").toUpperCase(),
          timeZone: requiredText(form, "timeZone"),
          locale: requiredText(form, "locale"),
        });
        setOrganizationId(id);
        setCreatedSlug(requiredText(form, "slug"));
      }

      setStep("assets");
      const logo = optionalFile(form, "logo");
      const favicon = optionalFile(form, "favicon");
      const [storedLogo, storedFavicon] = await Promise.all([
        logo ? adminApi.uploadBrandingAsset(api, id, "Logo", logo) : null,
        favicon
          ? adminApi.uploadBrandingAsset(api, id, "Favicon", favicon)
          : null,
      ]);

      setStep("branding");
      await adminApi.updateBranding(api, id, {
        storeTitle: requiredText(form, "storeTitle"),
        supportEmail: requiredText(form, "supportEmail"),
        primaryColor: requiredText(form, "primaryColor"),
        secondaryColor: requiredText(form, "secondaryColor"),
        accentColor: requiredText(form, "accentColor"),
        logoUrl: storedLogo?.url ?? optionalText(form, "logoUrl"),
        faviconUrl: storedFavicon?.url ?? optionalText(form, "faviconUrl"),
        supportPhone: optionalText(form, "supportPhone"),
        footerText: optionalText(form, "footerText"),
      });

      setStep("publishing");
      await adminApi.publishBranding(api, id);
      setStep("complete");
    }, "Organization created and branding published.");
    if (!completed) setStep("idle");
  }

  return (
    <div className="content-stack platform-onboarding">
      <PageHeader
        eyebrow="Platform administration"
        title="Create a branded organization"
        description="Provision the tenant, store its branding assets, and publish the first storefront configuration in one workflow."
      />

      {(feedback.formErrors.length > 0 ||
        Object.keys(feedback.fieldErrors).length > 0) && (
        <Alert title="Provisioning stopped" tone="danger">
          <FormErrorSummary
            errors={feedback.fieldErrors}
            generalErrors={feedback.formErrors}
            id={feedback.errorSummaryId}
          />
          {organizationId && (
            <p>
              The organization was already created with ID{" "}
              <code>{organizationId}</code>. Correct the form and retry; it will
              not create a duplicate tenant.
            </p>
          )}
        </Alert>
      )}

      {step === "complete" ? (
        <>
          <div
            id={feedback.successId}
            className="ds-sr-only"
            role="status"
            tabIndex={-1}
          >
            {feedback.successMessage}
          </div>
          <Card className="platform-onboarding__success">
            <h2>Organization is published</h2>
            <p>
              <strong>{createdSlug}</strong> is ready for tenant resolution. The
              organization ID is <code>{organizationId}</code>.
            </p>
            <p>
              For local development, set{" "}
              <code>NEXT_PUBLIC_DEVELOPMENT_ORGANIZATION_SLUG</code> to{" "}
              <code>{createdSlug}</code> in the portal environment files and
              restart the Next.js apps.
            </p>
            <Button
              onClick={() => {
                setName("");
                setSlug("");
                setSlugWasEdited(false);
                setOrganizationId(null);
                setCreatedSlug(null);
                setStep("idle");
                feedback.clear();
              }}
            >
              Create another organization
            </Button>
          </Card>
        </>
      ) : (
        <form className="content-stack" onSubmit={provision}>
          <Card>
            <h2>Organization identity</h2>
            <div className="form-grid">
              <InputField
                name="name"
                label="Organization name"
                error={feedback.fieldError("name")}
                required
                maxLength={200}
                value={name}
                disabled={Boolean(organizationId)}
                onChange={(event) => {
                  const nextName = event.currentTarget.value;
                  setName(nextName);
                  if (!slugWasEdited) setSlug(slugify(nextName));
                }}
              />
              <InputField
                name="slug"
                label="Organization slug"
                error={feedback.fieldError("slug")}
                hint="Lowercase letters, numbers, and single hyphens only. This cannot be changed after creation."
                required
                pattern="[a-z0-9]+(?:-[a-z0-9]+)*"
                maxLength={100}
                value={slug}
                disabled={Boolean(organizationId)}
                onChange={(event) => {
                  setSlugWasEdited(true);
                  setSlug(slugify(event.currentTarget.value));
                }}
              />
              <InputField
                name="currencyCode"
                label="Currency code"
                error={feedback.fieldError("currencyCode")}
                hint="Three-letter ISO code, such as PHP."
                required
                minLength={3}
                maxLength={3}
                defaultValue="PHP"
                disabled={Boolean(organizationId)}
              />
              <InputField
                name="timeZone"
                label="Time zone"
                error={feedback.fieldError("timeZone")}
                required
                defaultValue="Asia/Manila"
                disabled={Boolean(organizationId)}
              />
              <InputField
                name="locale"
                label="Locale"
                error={feedback.fieldError("locale")}
                required
                defaultValue="en-PH"
                disabled={Boolean(organizationId)}
              />
            </div>
          </Card>

          <Card>
            <h2>Store branding</h2>
            <div className="form-grid">
              <InputField
                name="storeTitle"
                label="Store title"
                error={feedback.fieldError("storeTitle")}
                required
                maxLength={200}
              />
              <InputField
                name="supportEmail"
                label="Support email"
                error={feedback.fieldError("supportEmail")}
                type="email"
                required
                maxLength={320}
              />
              <InputField
                name="primaryColor"
                label="Primary color"
                error={feedback.fieldError("primaryColor")}
                type="color"
                defaultValue="#14532D"
                required
              />
              <InputField
                name="secondaryColor"
                label="Secondary color"
                error={feedback.fieldError("secondaryColor")}
                type="color"
                defaultValue="#F0FDF4"
                required
              />
              <InputField
                name="accentColor"
                label="Accent color"
                error={feedback.fieldError("accentColor")}
                type="color"
                defaultValue="#22C55E"
                required
              />
              <InputField
                name="supportPhone"
                label="Support phone"
                maxLength={50}
              />
            </div>
            <TextareaField
              name="footerText"
              label="Footer text"
              maxLength={500}
              rows={3}
            />
          </Card>

          <Card>
            <h2>Brand assets</h2>
            <p>
              Upload a file or provide an existing absolute URL. An uploaded
              file takes precedence.
            </p>
            <div className="form-grid">
              <InputField
                name="logo"
                label="Logo file"
                type="file"
                accept="image/png,image/jpeg,image/webp,image/svg+xml"
              />
              <InputField name="logoUrl" label="Existing logo URL" type="url" />
              <InputField
                name="favicon"
                label="Favicon file"
                type="file"
                accept="image/png,image/x-icon,image/svg+xml"
              />
              <InputField
                name="faviconUrl"
                label="Existing favicon URL"
                type="url"
              />
            </div>
          </Card>

          <div className="platform-onboarding__actions">
            <Button
              type="submit"
              disabled={feedback.isSubmitting}
              isLoading={feedback.isSubmitting}
            >
              {buttonLabel(step, Boolean(organizationId))}
            </Button>
            <p role="status" aria-live="polite">
              {statusLabel(step)}
            </p>
          </div>
        </form>
      )}
    </div>
  );
}

function requiredText(data: FormData, name: string): string {
  const value = data.get(name);
  if (typeof value !== "string" || !value.trim())
    throw new Error(`${name} is required.`);
  return value.trim();
}

function optionalText(data: FormData, name: string): string | null {
  const value = data.get(name);
  return typeof value === "string" && value.trim() ? value.trim() : null;
}

function optionalFile(data: FormData, name: string): File | null {
  const value = data.get(name);
  return value instanceof File && value.size > 0 ? value : null;
}

function slugify(value: string): string {
  return value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

function buttonLabel(step: ProvisioningStep, created: boolean): string {
  if (step === "creating") return "Creating organization…";
  if (step === "assets") return "Uploading assets…";
  if (step === "branding") return "Saving branding…";
  if (step === "publishing") return "Publishing…";
  return created ? "Retry branding setup" : "Create and publish organization";
}

function statusLabel(step: ProvisioningStep): string {
  if (step === "idle" || step === "complete") return "";
  return buttonLabel(step, false);
}
