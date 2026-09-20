import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
    ...devices["Desktop Chrome"],
    channel: "msedge",
  },
  projects: [
    {
      name: "storefront",
      use: {
        baseURL: process.env.E2E_STOREFRONT_URL ?? "http://localhost:3100",
      },
    },
    {
      name: "agent-portal",
      use: { baseURL: process.env.E2E_AGENT_URL ?? "http://localhost:3101" },
    },
    {
      name: "admin-portal",
      use: { baseURL: process.env.E2E_ADMIN_URL ?? "http://localhost:3102" },
    },
  ],
  webServer: [
    {
      command:
        "node apps/storefront/.next/standalone/apps/storefront/server.js",
      port: 3100,
      env: { ...process.env, PORT: "3100", HOSTNAME: "127.0.0.1" },
      reuseExistingServer: false,
      timeout: 120_000,
    },
    {
      command:
        "node apps/agent-portal/.next/standalone/apps/agent-portal/server.js",
      port: 3101,
      env: { ...process.env, PORT: "3101", HOSTNAME: "127.0.0.1" },
      reuseExistingServer: false,
      timeout: 120_000,
    },
    {
      command:
        "node apps/admin-portal/.next/standalone/apps/admin-portal/server.js",
      port: 3102,
      env: { ...process.env, PORT: "3102", HOSTNAME: "127.0.0.1" },
      reuseExistingServer: false,
      timeout: 120_000,
    },
  ],
});
