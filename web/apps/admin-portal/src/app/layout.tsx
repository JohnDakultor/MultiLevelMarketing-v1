import type { Metadata, Viewport } from "next";
import { headers } from "next/headers";
import type { ReactNode } from "react";
import "@modular-mlm/design-system/styles.css";
import "./styles.css";
import { AdminPortalProviders } from "./providers";
import { AdminPortalShell } from "../components/AdminPortalShell";

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
  const hostName =
    requestHeaders.get("x-forwarded-host") ??
    requestHeaders.get("host") ??
    "localhost";

  return (
    <html lang="en">
      <body>
        <AdminPortalProviders hostName={hostName}>
          <AdminPortalShell>{children}</AdminPortalShell>
        </AdminPortalProviders>
      </body>
    </html>
  );
}
