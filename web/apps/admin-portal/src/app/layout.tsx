import type { Metadata, Viewport } from "next";
import { organizationSelectionCookie } from "@modular-mlm/organization-context";
import { cookies, headers } from "next/headers";
import type { ReactNode } from "react";
import "@modular-mlm/design-system/styles.css";
import "./styles.css";
import { AdminPortalProviders } from "./providers";
import { AdminPortalShell } from "../components/AdminPortalShell";
import { storefrontUrl } from "../lib/serverEnvironment";

export const metadata: Metadata = {
  title: "Admin Portal",
  description: "Organization administration workspace.",
};
export const viewport: Viewport = { width: "device-width", initialScale: 1 };

export default async function AdminPortalLayout({
  children,
}: {
  children: ReactNode;
}) {
  const requestHeaders = await headers();
  const cookieStore = await cookies();
  const hostName =
    requestHeaders.get("x-forwarded-host") ??
    requestHeaders.get("host") ??
    "localhost";

  return (
    <html lang="en">
      <body>
        <AdminPortalProviders
          hostName={hostName}
          organizationSlug={
            cookieStore.get(organizationSelectionCookie)?.value ||
            (process.env.NODE_ENV === "development"
              ? process.env.NEXT_PUBLIC_DEVELOPMENT_ORGANIZATION_SLUG?.trim()
              : undefined)
          }
          storefrontUrl={storefrontUrl()}
        >
          <AdminPortalShell>{children}</AdminPortalShell>
        </AdminPortalProviders>
      </body>
    </html>
  );
}
