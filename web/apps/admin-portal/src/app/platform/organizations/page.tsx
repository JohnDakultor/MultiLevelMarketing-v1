"use client";

import {
  Button,
  Card,
  InputField,
  PageHeader,
} from "@modular-mlm/design-system";
import { organizationSelectionUrl } from "@modular-mlm/organization-context";
import { useState, type FormEvent } from "react";

export default function PlatformOrganizationsPage() {
  const [slug, setSlug] = useState("");

  function selectOrganization(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    window.location.assign(organizationSelectionUrl(slug));
  }

  return (
    <div className="content-stack">
      <PageHeader
        eyebrow="Platform administration"
        title="Organizations"
        description="Open any existing organization without changing deployment configuration or restarting the portals."
      />
      <Card>
        <h2>Open an organization workspace</h2>
        <p>
          Enter the permanent organization slug created during provisioning.
          Your selection remains active in this browser until you choose another
          organization.
        </p>
        <form className="form-grid" onSubmit={selectOrganization}>
          <InputField
            id="platform-organization-slug"
            name="organizationSlug"
            label="Organization slug"
            hint="For example: aurevia"
            pattern="[a-z0-9]+(?:-[a-z0-9]+)*"
            required
            value={slug}
            onChange={(event) => setSlug(event.currentTarget.value)}
          />
          <div>
            <Button type="submit">Open organization</Button>
          </div>
        </form>
      </Card>
    </div>
  );
}
