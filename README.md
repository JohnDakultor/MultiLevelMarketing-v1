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
