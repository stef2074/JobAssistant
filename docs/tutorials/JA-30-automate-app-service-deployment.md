# JA-30: Automate App Service Deployment

## Overview

JA-30 automates the JobAssistant Development deployment process using GitHub Actions.

The work builds on two previously verified capabilities:

- The manual Azure App Service deployment and application logging process.
- GitHub Actions authentication to Azure using OpenID Connect (OIDC).

The completed workflow restores, builds, and publishes JobAssistant, authenticates to Azure without a client secret or App Service publish profile, verifies the Development subscription, and deploys the application to the Development Azure App Service.

The workflow is defined in:

```text
.github/workflows/deploy-development.yml
```

The deployment target is:

- Subscription: Development
- Resource Group: `rg-jobassistant-dev`
- App Service: `app-jobassistant-dev`
- GitHub environment: `development`

## Prerequisites

Before implementing JA-30, the following must already be available:

- JobAssistant builds successfully with .NET 10.
- The Development Azure App Service is deployed and operational.
- Manual deployment to `app-jobassistant-dev` has been verified.
- The GitHub `development` environment exists.
- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` are configured as GitHub environment variables.
- The Azure application registration contains the federated credential required for the GitHub `development` environment.
- GitHub Actions OIDC authentication to the Development subscription has been verified.

## Implementation

### 1. Create the Development deployment workflow

Create `.github/workflows/deploy-development.yml` with a manually triggered build:

```yaml
name: Deploy Development

on:
  workflow_dispatch:

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore
```

Using `workflow_dispatch` keeps deployment manual while the pipeline is being developed and verified.

### 2. Verify the build workflow

After the workflow is available on `main`, start it manually:

```bash
gh workflow run deploy-development.yml
```

List recent runs:

```bash
gh run list --workflow deploy-development.yml --limit 5
```

Monitor a specific run:

```bash
gh run watch <run-id> --interval 3
```

A successful run verifies checkout, .NET 10 setup, restore, and the Release build before deployment capabilities are introduced.

### 3. Add publishing

Add the Publish step after Build:

```yaml
      - name: Publish
        run: dotnet publish src/JobAssistant.Web/JobAssistant.Web.csproj --configuration Release --no-build --output ./publish
```

The `--no-build` option reuses the successful Release build. Run the workflow again and verify publishing succeeds before continuing.

### 4. Add OIDC permissions and the Development environment

Add:

```yaml
permissions:
  id-token: write
  contents: read
```

Then configure the job to use the existing GitHub environment:

```yaml
jobs:
  build:
    environment: development
    runs-on: ubuntu-latest
```

The `development` environment provides:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

These are environment variables and are referenced through the GitHub Actions `vars` context.

### 5. Add Azure OIDC authentication

After Publish, add:

```yaml
      - name: Log in to Azure
        uses: azure/login@v2
        with:
          client-id: ${{ vars.AZURE_CLIENT_ID }}
          tenant-id: ${{ vars.AZURE_TENANT_ID }}
          subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}
```

No Azure client secret or App Service publish profile is required.

### 6. Verify the Azure subscription

Before deployment, add:

```yaml
      - name: Verify Azure authentication
        run: |
          az account show \
            --query "{Name:name, SubscriptionId:id}" \
            --output table
```

Run the workflow and verify the output identifies the subscription as `Development`.

This establishes that build, publish, and OIDC authentication work together before the deployment step is introduced.

### 7. Add App Service deployment

Add:

```yaml
      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: app-jobassistant-dev
          package: ./publish
```

The completed pipeline is:

```text
Checkout repository
        ↓
Setup .NET 10
        ↓
Restore dependencies
        ↓
Build Release
        ↓
Publish JobAssistant.Web
        ↓
Authenticate to Azure using OIDC
        ↓
Verify Development subscription
        ↓
Deploy to app-jobassistant-dev
```

## Run the Complete Deployment

Start the workflow:

```bash
gh workflow run deploy-development.yml
```

Find the run:

```bash
gh run list --workflow deploy-development.yml --limit 5
```

Monitor it:

```bash
gh run watch <run-id> --interval 3
```

The completed workflow should report `success`.

## Verify the Live Application

After deployment:

1. Open the Development JobAssistant App Service.
2. Verify JobAssistant loads successfully.
3. Navigate to the Candidate Profile page.
4. Verify the page behaves normally.

The application can take several seconds to respond while the App Service starts or warms up.

## Verify Application Logging

Navigating to Candidate Profile generates the application log entry used for verification.

In the central `law-monitoring` Log Analytics Workspace in the Management subscription, run:

```kusto
AppServiceConsoleLogs
| where TimeGenerated > ago(15m)
| where ResultDescription contains "Candidate Profile page initialized."
| project TimeGenerated, ResultDescription
| order by TimeGenerated desc
```

A matching result confirms that the GitHub Actions-deployed application is producing its expected `ILogger` output and that Azure App Service continues forwarding the logs to the central Log Analytics Workspace.

If the specific message does not appear immediately, temporarily broaden the query:

```kusto
AppServiceConsoleLogs
| where TimeGenerated > ago(1h)
| project TimeGenerated, ResultDescription
| order by TimeGenerated desc
```

This can distinguish Log Analytics ingestion delay from an overly restrictive filter.

## Verification Results

JA-30 was verified incrementally.

### Build

The GitHub Actions workflow successfully checked out the repository, installed .NET 10, restored dependencies, and built JobAssistant in Release configuration.

### Publish

The workflow successfully published `src/JobAssistant.Web/JobAssistant.Web.csproj` to `./publish`.

### Azure Authentication

The workflow successfully authenticated using OIDC and verified that the authenticated Azure subscription was Development. No Azure client secret or App Service publish profile was used.

### App Service Deployment

The completed workflow successfully deployed JobAssistant to `app-jobassistant-dev`. The live application loaded and functioned correctly.

### Application Logging

After navigating to Candidate Profile, `AppServiceConsoleLogs` contained fresh entries including:

```text
Candidate Profile page initialized.
```

This verified the complete operational path:

```text
GitHub Actions
        ↓
Restore and Build
        ↓
Publish
        ↓
OIDC Authentication
        ↓
Development Subscription
        ↓
Azure App Service Deployment
        ↓
JobAssistant
        ↓
ILogger
        ↓
AppServiceConsoleLogs
        ↓
law-monitoring
```

## Key Lessons

### Verify the pipeline incrementally

JA-30 was implemented and verified in stages:

1. Restore and build.
2. Publish.
3. OIDC authentication and subscription verification.
4. App Service deployment.
5. Live application verification.
6. Application logging verification.

This made failures easier to isolate.

### Establish authentication independently

JA-29 established OIDC authentication independently before JA-30 depended on it. JA-30 then verified authentication inside the deployment workflow before adding App Service deployment.

### Use the `vars` context for environment variables

The Azure identifiers in the GitHub `development` environment are environment variables:

```yaml
${{ vars.AZURE_CLIENT_ID }}
${{ vars.AZURE_TENANT_ID }}
${{ vars.AZURE_SUBSCRIPTION_ID }}
```

### OIDC removes long-lived Azure deployment credentials

The deployment workflow does not require an Azure client secret or App Service publish profile. GitHub Actions uses a short-lived OIDC token that Microsoft Entra ID validates against the configured federated credential.

### Successful deployment is not the final verification

A successful workflow confirms the deployment operation completed, but JA-30 also verifies the live application and centralized application logging.

## Result

JA-30 established a verified automated Development deployment pipeline for JobAssistant.

JobAssistant can now be restored, built, published, authenticated to Azure using OIDC, and deployed to `app-jobassistant-dev` from GitHub Actions without storing long-lived Azure deployment credentials.

The workflow remains manually triggered with `workflow_dispatch`, providing a controlled deployment process while the project's CI/CD capabilities continue to evolve.
