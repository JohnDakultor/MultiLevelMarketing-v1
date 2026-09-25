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
    return [
      {
        source: "/:path*",
        headers: [
          ...frontendSecurityHeaders(),
          { key: "Cache-Control", value: "private, no-store" },
        ],
      },
    ];
  },
};

export default nextConfig;
