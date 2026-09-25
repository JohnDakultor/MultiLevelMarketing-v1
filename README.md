# Modular Marketplace with Binary MLM

This repository keeps the conventions and dependency flow of Jason Taylor's Clean Architecture solution while naming its business features from `modular_marketplace_binary_mlm_sdd(1).md`.

## Solution

- `src/Domain`: conventional `Common`, `Constants`, `Entities`, `Enums`, `Events`, `Exceptions`, `Services`, and `ValueObjects` folders containing marketplace names from the SDD.
- `src/Application`: vertical feature folders with Commands and Queries, plus the original Common behaviors and interfaces.
- `src/Infrastructure`: the conventional `Data`, `Identity`, and provider implementation folders.
- `src/Web`: REST API endpoints for public catalog, organizations, agents, network, wallet, and payouts.
- `src/AppHost`: .NET Aspire development orchestration.

## Run

```powershell
dotnet run --project .\src\AppHost
```

The development seed creates the `default` organization and an administrator account at `administrator@localhost`.

## Verify

```powershell
dotnet build
dotnet test
```

The domain stays independent of HTTP, Entity Framework, payment providers, and deployment infrastructure.

## Azure Blob object storage

The Aspire AppHost provisions an Azure Storage account and the
`marketplace-assets` blob container. Locally, the same resource runs through a
persistent Azurite container. The Web API receives the container connection and
uses managed identity after deployment to Azure.

Set a public asset origin separately in each deployed environment:

```text
ObjectStorage__AzureBlob__Enabled=true
ObjectStorage__AzureBlob__PublicBaseUrl=https://<public-asset-host>/<container-or-path>
```

`PublicBaseUrl` can be an Azure Front Door/CDN address or the blob-container URL
when anonymous blob access is intentionally enabled. Keep the container private
when using a private CDN origin. The Web API's managed identity requires the
`Storage Blob Data Contributor` role; Aspire assigns the storage data role when
the referenced resource is deployed.

When deploying the Web project without its AppHost, provide a connection named
`marketplace-assets` using the Aspire Azure Blob client configuration. Prefer a
service URI plus managed identity in Azure; use a connection string only for
local development.

## Local integration secrets

The Web project uses .NET user-secrets. Keep PayMongo and SMTP credentials out of tracked
`appsettings.json` files:

```powershell
dotnet user-secrets set --project src/Web "PayMongo:SecretKey" "<secret-key>"
dotnet user-secrets set --project src/Web "PayMongo:WebhookSecret" "<webhook-secret>"
dotnet user-secrets set --project src/Web "PayMongo:WalletAccountNumber" "<wallet-number>"
dotnet user-secrets set --project src/Web "PayMongo:WalletAccountName" "<wallet-name>"
dotnet user-secrets set --project src/Web "Email:Smtp:Host" "<smtp-host>"
dotnet user-secrets set --project src/Web "Email:Smtp:FromAddress" "<sender-address>"
dotnet user-secrets set --project src/Web "Email:Smtp:UserName" "<smtp-user>"
dotnet user-secrets set --project src/Web "Email:Smtp:Password" "<smtp-password>"
```
