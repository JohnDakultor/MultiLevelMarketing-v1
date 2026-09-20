# Frontend release gate

Use Node.js 22.13 or newer. The repository pins the minimum local version in
`.nvmrc` and `package.json`.

The three Next.js applications deploy independently:

| Application  | Workspace                   | Default local URL       | Suggested production host  |
| ------------ | --------------------------- | ----------------------- | -------------------------- |
| Storefront   | `@modular-mlm/storefront`   | `http://localhost:3000` | Organization-owned domain  |
| Agent portal | `@modular-mlm/agent-portal` | `http://localhost:3001` | `agents.<platform-domain>` |
| Admin portal | `@modular-mlm/admin-portal` | `http://localhost:3002` | `admin.<platform-domain>`  |

Each application build prepares a self-contained server under
`apps/<application>/.next/standalone/apps/<application>`. Deploy that standalone
tree and start its `server.js` with the deployment's `PORT` and `HOSTNAME`.

Each deployment must provide the server-only `BACKEND_API_BASE_URL`. Browser-visible telemetry is optional and uses `NEXT_PUBLIC_TELEMETRY_ENDPOINT` and `NEXT_PUBLIC_RELEASE`. Backend credentials, provider keys, SMTP credentials, and database connection strings must never use the `NEXT_PUBLIC_` prefix.

Run the local quality gate from `web/`:

```powershell
npm run release:check
```

Run browser accessibility and responsive checks with Microsoft Edge installed:

```powershell
npm run test:e2e
```

The browser tests start isolated copies of all three applications on ports 3100–3102 so existing development sessions cannot affect the result. Set `E2E_STOREFRONT_URL`, `E2E_AGENT_URL`, and `E2E_ADMIN_URL` to target already-running deployments instead. Authenticated critical-path tests require disposable users and Organization data provisioned through the real Web API and PostgreSQL test environment; they must not use production credentials or mocked backend behavior.

## Real-stack browser gate

Set `E2E_REAL_STACK=1` and provide these disposable test-stack values:

- `E2E_CUSTOMER_EMAIL`, `E2E_CUSTOMER_PASSWORD`
- `E2E_AGENT_EMAIL`, `E2E_AGENT_PASSWORD`
- `E2E_ADMIN_EMAIL`, `E2E_ADMIN_PASSWORD`
- `E2E_REFERRAL_CODE`, `E2E_PRODUCT_SLUG`
- `E2E_PAID_ORDER_ID`, `E2E_CANCELLABLE_ORDER_ID`, `E2E_REFUNDABLE_ORDER_ID`
- `E2E_PAYOUT_AMOUNT`, `E2E_PENDING_AGENT_CODE`
- `E2E_ADMIN_REFUNDABLE_ORDER_NUMBER`, `E2E_ADMIN_PAYMENT_REFUND_AMOUNT`
- `E2E_UNPLACED_AGENT_CODE`, `E2E_PLACEMENT_PARENT_AGENT_CODE`
- `E2E_VERIFY_PAYOUT_REQUEST_PREFIX`, `E2E_REJECT_PAYOUT_REQUEST_PREFIX`
- `E2E_FOREIGN_ORGANIZATION_ID`

The product must be published and in stock, the Customer must have a default
address, the Agent must have a verified payout account and sufficient available
balance, and all order/application fixtures must be in the lifecycle state named by
the variable. The tests intentionally mutate this disposable data and must never run
against production.

```powershell
npm.cmd run build:apps
npm.cmd run test:e2e:real
```

Before promotion, verify the Web API origin/cookie/CORS configuration for all three public origins, apply pending database migrations, run the backend PostgreSQL functional suite, and confirm PayMongo and email provider health in the target environment.
