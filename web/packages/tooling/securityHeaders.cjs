function frontendSecurityHeaders() {
  const scriptSources = ["'self'", "'unsafe-inline'"];
  if (process.env.NODE_ENV !== "production") scriptSources.push("'unsafe-eval'");

  const connectSources = ["'self'"];
  const telemetryEndpoint = process.env.NEXT_PUBLIC_TELEMETRY_ENDPOINT;
  if (telemetryEndpoint) {
    try {
      connectSources.push(new URL(telemetryEndpoint).origin);
    } catch {
      throw new Error("NEXT_PUBLIC_TELEMETRY_ENDPOINT must be an absolute URL.");
    }
  }

  const contentSecurityPolicy = [
    "default-src 'self'",
    "base-uri 'self'",
    "frame-ancestors 'none'",
    "object-src 'none'",
    "form-action 'self'",
    "img-src 'self' data: blob: https:",
    "font-src 'self' data:",
    "style-src 'self' 'unsafe-inline'",
    `script-src ${scriptSources.join(" ")}`,
    `connect-src ${connectSources.join(" ")}`,
  ].join("; ");

  return [
    { key: "Content-Security-Policy", value: contentSecurityPolicy },
    { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
    { key: "X-Content-Type-Options", value: "nosniff" },
    { key: "X-Frame-Options", value: "DENY" },
    {
      key: "Permissions-Policy",
      value: "camera=(), microphone=(), geolocation=(), payment=(self)",
    },
    { key: "Cross-Origin-Opener-Policy", value: "same-origin-allow-popups" },
  ];
}

module.exports = { frontendSecurityHeaders };
