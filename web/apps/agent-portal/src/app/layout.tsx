import type { Metadata, Viewport } from "next";
import { organizationSelectionCookie } from "@modular-mlm/organization-context";
import { cookies, headers } from "next/headers";
import type { ReactNode } from "react";
import "@modular-mlm/design-system/styles.css";
import "@xyflow/react/dist/style.css";
import "./styles.css";
import { AgentPortalProviders } from "./providers";
import { AgentPortalShell } from "../components/AgentPortalShell";
import { storefrontUrl } from "../lib/serverEnvironment";

export const metadata: Metadata = {
  title: "Agent Portal",
  description: "Agent sales and network workspace.",
};
export const viewport: Viewport = { width: "device-width", initialScale: 1 };

export default async function AgentPortalLayout({
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
        <AgentPortalProviders
          hostName={hostName}
          organizationSlug={
            cookieStore.get(organizationSelectionCookie)?.value ||
            (process.env.NODE_ENV === "development"
              ? process.env.NEXT_PUBLIC_DEVELOPMENT_ORGANIZATION_SLUG?.trim()
              : undefined)
          }
          storefrontUrl={storefrontUrl()}
        >
          <AgentPortalShell>{children}</AgentPortalShell>
        </AgentPortalProviders>
      </body>
    </html>
  );
}
