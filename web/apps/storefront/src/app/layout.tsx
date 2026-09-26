import type { Metadata, Viewport } from "next";
import { headers } from "next/headers";
import type { ReactNode } from "react";
import "@modular-mlm/design-system/styles.css";
import "./styles.css";
import { StorefrontProviders } from "./providers";
import { StorefrontShell } from "../components/StorefrontShell";

export const metadata: Metadata = {
  title: "Marketplace",
  description: "Organization storefront and customer account.",
};

export const viewport: Viewport = { width: "device-width", initialScale: 1 };

export default async function StorefrontLayout({
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
        <StorefrontProviders
          hostName={hostName}
          organizationSlug={
            process.env.ORGANIZATION_SLUG?.trim() ||
            process.env.NEXT_PUBLIC_DEVELOPMENT_ORGANIZATION_SLUG?.trim()
          }
        >
          <StorefrontShell>{children}</StorefrontShell>
        </StorefrontProviders>
      </body>
    </html>
  );
}
