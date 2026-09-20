"use client";

import { NotificationCenter } from "@modular-mlm/notifications";
import { useOrganization } from "@modular-mlm/organization-context";

export default function AdminNotificationsPage() {
  const { organization } = useOrganization();
  return organization ? (
    <NotificationCenter organizationId={organization.id} />
  ) : null;
}
