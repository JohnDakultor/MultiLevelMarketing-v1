import type { NextConfig } from "next";
import { frontendSecurityHeaders } from "@modular-mlm/tooling/security-headers";

const backendOrigin = (
  process.env.BACKEND_API_BASE_URL ?? "http://localhost:5154"
).replace(/\/$/, "");

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
  async rewrites() {
    return [
      { source: "/api/:path*", destination: `${backendOrigin}/api/:path*` },
    ];
  },
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
