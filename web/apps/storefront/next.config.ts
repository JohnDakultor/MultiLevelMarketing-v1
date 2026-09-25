import type { NextConfig } from "next";
import { frontendSecurityHeaders } from "@modular-mlm/tooling/security-headers";

const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  transpilePackages: [
    "@modular-mlm/api-client",
    "@modular-mlm/auth",
    "@modular-mlm/contracts",
    "@modular-mlm/design-system",
    "@modular-mlm/observability",
    "@modular-mlm/notifications",
    "@modular-mlm/organization-context",
  ],
  async headers() {
    const security = frontendSecurityHeaders();
    const privateResponse = [
      ...security,
      { key: "Cache-Control", value: "private, no-store" },
    ];
    return [
      { source: "/:path*", headers: security },
      { source: "/api/:path*", headers: privateResponse },
      { source: "/account/:path*", headers: privateResponse },
      { source: "/cart", headers: privateResponse },
      { source: "/checkout", headers: privateResponse },
      { source: "/payment-return", headers: privateResponse },
    ];
  },
};

export default nextConfig;
