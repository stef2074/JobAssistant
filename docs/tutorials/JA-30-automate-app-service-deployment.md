# JA-30 Automate App Service Deployment

## Objective

Automate the JobAssistant Development deployment process using GitHub
Actions.

By completing this milestone, you will learn how to extend the verified
manual App Service deployment and GitHub Actions OIDC authentication
baseline into a controlled deployment workflow that restores, builds,
publishes, authenticates, verifies the Development subscription, and
deploys JobAssistant to Azure App Service.

## Prerequisites

Before implementing JA-30, the following must already be available:

-   JobAssistant builds successfully with .NET 10.
-   The Development Azure App Service is deployed and operational.
-   Manual deployment to `app-jobassistant-dev` has been verified.
-   The GitHub `development` environment exists.
-   `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID`
    are configured as GitHub environment variables.
-   The Azure application registration contains the federated credential
    required for the GitHub `development` environment.
-   GitHub Actions OIDC authentication to the Development subscription
    has been verified.

## Concepts Introduced

This milestone introduces:

-   Automated .NET restore and build
-   Automated `dotnet publish`
-   GitHub Actions deployment workflows
-   Incremental CI/CD verification
-   Reusing GitHub Actions OIDC authentication
-   Azure subscription verification inside a deployment workflow
-   `azure/webapps-deploy`
-   Post-deployment application verification
-   Post-deployment logging verification

## Step-by-Step Walkthrough

### Step 1 -- Create the Development Deployment Workflow

Create `.github/workflows/deploy-development.yml` with a manually
triggered build:

``` yaml
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

Using `workflow_dispatch` keeps deployment manual while the pipeline is
being developed and verified.

### Step 2 -- Verify the Build Workflow

After the workflow is available on `main`, start it manually:

``` bash
gh workflow run deploy-development.yml
```

List recent runs:

``` bash
gh run list --workflow deploy-development.yml --limit 5
```

Monitor a specific run:

``` bash
gh run watch <run-id> --interval 3
```

A successful run verifies checkout, .NET 10 setup, restore, and the
Release build before deployment capabilities are introduced.

### Step 3 -- Add Publishing

Add the Publish step after Build:

``` yaml
      - name: Publish
        run: dotnet publish src/JobAssistant.Web/JobAssistant.Web.csproj --configuration Release --no-build --output ./publish
```

The `--no-build` option reuses the successful Release build. Run the
workflow again and verify publishing succeeds before continuing.

### Step 4 -- Add OIDC Permissions and the Development Environment

Add:

``` yaml
permissions:
  id-token: write
  contents: read
```

Then configure the job to use the existing GitHub environment:

``` yaml
jobs:
  build:
    environment: development
    runs-on: ubuntu-latest
```

The `development` environment provides:

-   `AZURE_CLIENT_ID`
-   `AZURE_TENANT_ID`
-   `AZURE_SUBSCRIPTION_ID`

These are environment variables and are referenced through the GitHub
Actions `vars` context.

### Step 5 -- Add Azure OIDC Authentication

After Publish, add:

``` yaml
      - name: Log in to Azure
        uses: azure/login@v2
        with:
          client-id: ${{ vars.AZURE_CLIENT_ID }}
          tenant-id: ${{ vars.AZURE_TENANT_ID }}
          subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}
```

No Azure client secret or App Service publish profile is required.

### Step 6 -- Verify the Azure Subscription

Before deployment, add:

``` yaml
      - name: Verify Azure authentication
        run: |
          az account show \
            --query "{Name:name, SubscriptionId:id}" \
            --output table
```

Run the workflow and verify the output identifies the subscription as
`Development`.

This establishes that build, publish, and OIDC authentication work
together before the deployment step is introduced.

### Step 7 -- Add App Service Deployment

Add:

``` yaml
      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: app-jobassistant-dev
          package: ./publish
```

The completed pipeline is:

``` text
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

### Step 8 -- Run the Complete Deployment

Start the workflow:

``` bash
gh workflow run deploy-development.yml
```

Find the run:

``` bash
gh run list --workflow deploy-development.yml --limit 5
```

Monitor it:

``` bash
gh run watch <run-id> --interval 3
```

The completed workflow should report `success`.

### Step 9 -- Verify the Live Application

After deployment:

1.  Open the Development JobAssistant App Service.
2.  Verify JobAssistant loads successfully.
3.  Navigate to the Candidate Profile page.
4.  Verify the page behaves normally.

The application can take several seconds to respond while the App
Service starts or warms up.

### Step 10 -- Verify Application Logging

Navigating to Candidate Profile generates the application log entry used
for verification.

In the central `law-monitoring` Log Analytics Workspace in the
Management subscription, run:

``` kusto
AppServiceConsoleLogs
| where TimeGenerated > ago(15m)
| where ResultDescription contains "Candidate Profile page initialized."
| project TimeGenerated, ResultDescription
| order by TimeGenerated desc
```

A matching result confirms that the GitHub Actions-deployed application
is producing its expected `ILogger` output and that Azure App Service
continues forwarding the logs to the central Log Analytics Workspace.

If the specific message does not appear immediately, temporarily broaden
the query:

``` kusto
AppServiceConsoleLogs
| where TimeGenerated > ago(1h)
| project TimeGenerated, ResultDescription
| order by TimeGenerated desc
```

This can distinguish Log Analytics ingestion delay from an overly
restrictive filter.

------------------------------------------------------------------------

## Architecture

JA-30 combines the deployment baseline established by JA-28 with the
OIDC authentication baseline established by JA-29.

``` text
GitHub Actions
        │
        ▼
Restore and Build
        │
        ▼
Publish
        │
        ▼
GitHub OIDC Token
        │
        ▼
Microsoft Entra Federated Credential
        │
        ▼
Development Subscription
        │
        ▼
Azure App Service
        │
        ▼
JobAssistant
        │
        ▼
ILogger
        │
        ▼
AppServiceConsoleLogs
        │
        ▼
law-monitoring
```

The workflow builds and deploys the application. GitHub OIDC and
Microsoft Entra ID provide authentication without a long-lived Azure
deployment credential. Azure App Service hosts the application, and the
existing monitoring infrastructure continues to collect application
logs.

------------------------------------------------------------------------

## Common Mistakes

### Adding Deployment Before Verifying Earlier Stages

Introducing restore, build, publish, authentication, and deployment at
the same time makes failures harder to isolate.

Verify each stage before adding the next one.

### Forgetting the Development Environment

Environment-level variables are available only when the job targets the
environment that contains them.

Use:

``` yaml
environment: development
```

### Using `secrets` Instead of `vars`

The Azure identifiers are GitHub environment variables.

Use:

``` yaml
${{ vars.AZURE_CLIENT_ID }}
${{ vars.AZURE_TENANT_ID }}
${{ vars.AZURE_SUBSCRIPTION_ID }}
```

### Forgetting `id-token: write`

Without:

``` yaml
id-token: write
```

GitHub Actions cannot request the OIDC token required for Azure
authentication.

### Skipping Subscription Verification

A successful Azure login does not prove that the workflow is using the
intended subscription.

Verify the Azure context before deployment.

### Treating Workflow Success as Complete Verification

A successful deployment action should be followed by live application
verification and logging verification.

------------------------------------------------------------------------

## Debugging Tips

If the workflow fails during build or publish:

1.  Identify the failed step with `gh run view <run-id>`.
2.  Verify the same .NET commands work locally.
3.  Confirm the workflow is using .NET 10.

If Azure authentication fails:

1.  Confirm the job targets `development`.
2.  Confirm the environment variables exist.
3.  Confirm the workflow uses the `vars` context.
4.  Confirm `id-token: write` is present.
5.  Reuse the OIDC troubleshooting established in JA-29.

If deployment succeeds but the application does not immediately appear:

1.  Wait several seconds for App Service startup.
2.  Refresh the application.
3.  Confirm the deployment workflow completed successfully.
4.  Verify the App Service is running.

If application logs do not appear:

1.  Trigger Candidate Profile initialization.
2.  Increase the KQL time range.
3.  Confirm App Service console logs are still forwarded to
    `law-monitoring`.

------------------------------------------------------------------------

## Lessons Learned

During JA-30 we learned that:

-   Deployment pipelines are easier to troubleshoot when built
    incrementally.
-   A verified manual deployment provides a useful baseline for
    automation.
-   Authentication should be proven independently before deployment
    depends on it.
-   GitHub Actions can restore, build, and publish .NET applications.
-   Existing OIDC authentication can be reused by deployment workflows.
-   Subscription verification is an important deployment safety check.
-   `azure/webapps-deploy` can deploy the published application directly
    to Azure App Service.
-   Long-lived Azure deployment credentials are unnecessary when OIDC is
    configured.
-   Successful deployment should be followed by live application and
    logging verification.

------------------------------------------------------------------------

## What We Learned

After completing JA-30 you can:

-   Create a manually triggered deployment workflow.
-   Restore and build JobAssistant with GitHub Actions.
-   Publish JobAssistant.Web for deployment.
-   Authenticate a deployment workflow to Azure using OIDC.
-   Use GitHub environment variables through the `vars` context.
-   Verify the authenticated Azure subscription.
-   Deploy JobAssistant to Azure App Service.
-   Verify the deployed application.
-   Verify centralized application logging after deployment.

------------------------------------------------------------------------

## Key Takeaways

JA-30 transformed the previously verified manual deployment process into
a repeatable GitHub Actions workflow.

``` text
Source
  ↓
GitHub Actions
  ↓
Restore
  ↓
Build
  ↓
Publish
  ↓
OIDC Authentication
  ↓
Development Subscription
  ↓
Azure App Service
```

The workflow remains manually triggered with `workflow_dispatch`,
providing a controlled deployment process while JobAssistant's CI/CD
capabilities continue to evolve.

------------------------------------------------------------------------

## Looking Ahead

The automated Development deployment baseline is now established.

Future milestones can build on this foundation with:

-   Automated tests in the deployment pipeline
-   Automatic deployment triggers
-   Deployment approvals
-   Additional deployment environments
-   Post-deployment health checks
-   More advanced application monitoring
