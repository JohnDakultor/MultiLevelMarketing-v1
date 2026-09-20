# Modular MLM frontend workspace

The frontend is an npm workspace with three independently deployable Next.js App
Router applications and shared platform packages.

```text
apps/
  storefront/       public marketplace and customer account
  agent-portal/     Agent network, sales, wallet, and payouts
  admin-portal/     Organization administration and operations
packages/
  api-client/       transport, Problem Details, timeout, auth, antiforgery
  auth/             current identity and capability checks
  contracts/        shared API request/response types
  design-system/    accessible visual primitives
  observability/    safe client telemetry abstractions
  organization-context/ host and development-slug resolution
  test-support/     typed test fixtures
  tooling/          shared strict TypeScript configuration
```

The retired root `src` application has been removed. Production frontend code now
lives exclusively in `apps/*`, with stable shared concerns in `packages/*`.

## Local development

Start the full backend stack with AppHost, or start the Web API directly:

```powershell
dotnet run --project src/AppHost
```

```powershell
dotnet run --project src/Web --launch-profile http
```

Install workspace dependencies once:

```powershell
cd web
npm.cmd install
```

Copy the appropriate `.env.example` to `.env.local` inside each application. The
development slug is a localhost fallback only; deployed applications resolve the
Organization from the request hostname.

Run an application:

```powershell
npm.cmd run dev:storefront # http://localhost:3000
npm.cmd run dev:agent      # http://localhost:3001
npm.cmd run dev:admin      # http://localhost:3002
```

The three applications include responsive branded shells, route guards, and the
Phase 12.4-12.6 Storefront, Agent, and Organization Administration workflows. Agent
and Admin routes redirect anonymous visitors to their own `/sign-in` flow and show a
separate access-denied state to authenticated users without the required tenant role.
Unsupported workflows are identified as API gaps rather than accepting unsafe raw
resource identifiers or displaying fake data.

## Verification

```powershell
npm.cmd run format:check
npm.cmd run typecheck:all
npm.cmd run test:workspaces
npm.cmd run build:apps
npm.cmd run check:architecture
npm.cmd run check:budgets
```

Each application also exposes its own `dev`, `typecheck`, `test`, `build`, and `start`
scripts through npm workspaces.

The critical-path browser suite never substitutes mocked production behavior. Start
AppHost against the disposable PostgreSQL test database, provision the identities
and records listed in `RELEASE.md`, set `E2E_REAL_STACK=1`, build the applications,
and run `npm.cmd run test:e2e:real`.

## Environment rules

- `BACKEND_API_BASE_URL` is server-only and controls the Next.js `/api` rewrite.
- Keep `NEXT_PUBLIC_API_BASE_URL` empty for same-origin browser requests.
- `NEXT_PUBLIC_DEVELOPMENT_ORGANIZATION_SLUG` is optional and used only on localhost
  when host resolution returns 404.
- Never store PayMongo, SMTP, database, signing, or other provider secrets in a
  frontend environment file.

See [API_ANALYSIS.md](./API_ANALYSIS.md) for the current endpoint, ownership, role, and
API-gap inventory.
