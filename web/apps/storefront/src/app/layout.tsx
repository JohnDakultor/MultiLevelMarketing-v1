import type { Metadata, Viewport } from "next";
import { organizationSelectionCookie } from "@modular-mlm/organization-context";
import { cookies, headers } from "next/headers";
import type { ReactNode } from "react";
import "@modular-mlm/design-system/styles.css";
import "./styles.css";
import { StorefrontProviders } from "./providers";
import { StorefrontShell } from "../components/StorefrontShell";

export const metadata: Metadata = {
  title: "Shop",
  description: "Browse products, place orders, and manage your account.",
};

export const viewport: Viewport = { width: "device-width", initialScale: 1 };

export default async function StorefrontLayout({
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
        <StorefrontProviders
          hostName={hostName}
          organizationSlug={
            cookieStore.get(organizationSelectionCookie)?.value ||
            (process.env.NODE_ENV === "development"
              ? process.env.NEXT_PUBLIC_DEVELOPMENT_ORGANIZATION_SLUG?.trim()
              : undefined)
          }
        >
          <StorefrontShell>{children}</StorefrontShell>
        </StorefrontProviders>
      </body>
    </html>
  );
}
