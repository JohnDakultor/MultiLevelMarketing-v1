import type { Metadata, Viewport } from "next";
import { headers } from "next/headers";
import type { ReactNode } from "react";
import "@modular-mlm/design-system/styles.css";
import "./styles.css";
import { AgentPortalProviders } from "./providers";
import { AgentPortalShell } from "../components/AgentPortalShell";

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
  const hostName =
    requestHeaders.get("x-forwarded-host") ??
    requestHeaders.get("host") ??
    "localhost";

  return (
    <html lang="en">
      <body>
        <AgentPortalProviders hostName={hostName}>
          <AgentPortalShell>{children}</AgentPortalShell>
        </AgentPortalProviders>
      </body>
    </html>
  );
}
