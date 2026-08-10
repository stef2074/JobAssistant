# JA-28 Verify App Service Application Logging

## Objective

Verify that JobAssistant .NET application logs are captured by Azure App
Service and forwarded to the central Log Analytics Workspace.

By completing this milestone, you will learn how to manually deploy
JobAssistant to Azure App Service, emit application logs using
`ILogger`, and verify those logs using KQL in Log Analytics.

------------------------------------------------------------------------

## Prerequisites

Before beginning this tutorial you should understand:

-   Basic C#
-   ASP.NET Core and Blazor fundamentals
-   Dependency Injection
-   `ILogger`
-   Azure CLI
-   Azure App Service
-   Azure Log Analytics
-   Basic KQL
-   The JobAssistant manual deployment process

The Azure infrastructure must already exist:

-   Development subscription
-   Resource Group `rg-jobassistant-dev`
-   App Service `app-jobassistant-dev`
-   Central Log Analytics Workspace `law-monitoring`
-   Diagnostic Setting forwarding App Service console logs to the
    workspace

------------------------------------------------------------------------

## Concepts Introduced

-   Manual application publishing
-   ZIP deployment to Azure App Service
-   Deployment artifact management
-   ASP.NET Core `ILogger`
-   Azure App Service console logging
-   Diagnostic Settings
-   Centralized logging
-   `AppServiceConsoleLogs`
-   KQL log verification

------------------------------------------------------------------------

## Step-by-Step Walkthrough

### Step 1 -- Exclude Deployment Artifacts from Source Control

Manual deployment creates generated files that should not be committed
to Git.

Add the following to `.gitignore`:

``` gitignore
# Deployment artifacts
publish/
JobAssistant.zip
```

The `publish/` directory contains generated application binaries.
`JobAssistant.zip` is the deployment package created from those
binaries.

Both artifacts can be reproduced from source and therefore do not belong
in source control.

Verify that Git ignores them:

``` bash
git check-ignore -v publish/
git check-ignore -v JobAssistant.zip
```

### Step 2 -- Add Application Logging

Inject an `ILogger` into the Candidate Profile component:

``` razor
@inject ILogger<CandidateProfile> Logger
```

Then emit an informational message when the page initializes:

``` csharp
protected override async Task OnInitializedAsync()
{
    Logger.LogInformation("Candidate Profile page initialized.");

    // Existing initialization logic...
}
```

`ILogger<T>` provides the standard ASP.NET Core logging abstraction. The
generic type identifies the logging category.

------------------------------------------------------------------------

## Why Use `ILogger`?

Application code uses the .NET logging abstraction rather than writing
directly to a particular logging destination.

``` text
CandidateProfile.razor
        │
        ▼
ILogger
        │
        ▼
ASP.NET Core logging
        │
        ▼
Console output
```

The application does not need to know that Azure Log Analytics will
eventually receive the message. That infrastructure responsibility
remains outside the application.

### Step 3 -- Verify the Application Locally

Build the solution:

``` bash
dotnet build
```

Run the Web application:

``` bash
dotnet run --project src/JobAssistant.Web/JobAssistant.Web.csproj
```

Open the local application and navigate to Candidate Profile. Verify
that the application continues to function correctly.

------------------------------------------------------------------------

## Manual Azure Deployment

JA-28 established a repeatable manual deployment process:

``` text
Source
  │
  ▼
dotnet publish
  │
  ▼
publish/
  │
  ▼
JobAssistant.zip
  │
  ▼
Azure App Service
```

### Step 4 -- Publish JobAssistant

From the repository root:

``` bash
dotnet publish \
  src/JobAssistant.Web/JobAssistant.Web.csproj \
  --configuration Release \
  --output ./publish
```

This creates a deployment-ready version of the Web application in
`publish/`.

### Step 5 -- Package the Published Application

If an old package exists, remove it before creating the new package:

``` bash
rm JobAssistant.zip
```

Then package the **contents** of the publish directory:

``` bash
cd publish
zip -r ../JobAssistant.zip .
cd ..
```

The contents of `publish/` must be at the root of the ZIP.

Correct:

``` text
JobAssistant.zip
├── JobAssistant.Web.dll
├── JobAssistant.Web.runtimeconfig.json
├── appsettings.json
└── wwwroot/
```

Incorrect:

``` text
JobAssistant.zip
└── publish/
    ├── JobAssistant.Web.dll
    └── wwwroot/
```

Inspect the package with:

``` bash
unzip -l JobAssistant.zip | head -30
```

### Step 6 -- Verify the Azure Subscription

Before deployment, confirm which Azure subscription the CLI is using:

``` bash
az account show \
  --query "{Name:name, SubscriptionId:id}" \
  --output table
```

For JobAssistant, the active subscription must be `Development`.

If necessary:

``` bash
az account set --subscription "<Development-subscription-id>"
```

Then verify the active subscription again before deploying.

### Step 7 -- Deploy to Azure App Service

Deploy the ZIP package:

``` bash
az webapp deploy \
  --resource-group rg-jobassistant-dev \
  --name app-jobassistant-dev \
  --src-path JobAssistant.zip \
  --type zip
```

A successful deployment should indicate that the application was
deployed and the site started successfully.

### Step 8 -- Verify the Deployed Application

Open the deployed JobAssistant application and verify that the Blazor
application loads, navigation works, the Candidate Profile page opens,
and existing application behavior works as expected.

Immediately after deployment, Azure App Service may briefly display its
default page. Refresh after the deployment has completed if necessary.

------------------------------------------------------------------------

## Application Logging Flow

The Azure infrastructure forwards App Service console logs to the
central Log Analytics Workspace.

``` text
JobAssistant
    │
    ▼
ILogger.LogInformation()
    │
    ▼
ASP.NET Core logging
    │
    ▼
Azure App Service console output
    │
    ▼
Diagnostic Setting
    │
    ▼
AppServiceConsoleLogs
    │
    ▼
law-monitoring
```

JobAssistant emits the log message. Azure App Service and the existing
Diagnostic Setting handle collection and forwarding.

### Step 9 -- Generate an Application Log

Open the deployed JobAssistant application and navigate to Candidate
Profile.

The component initialization executes:

``` csharp
Logger.LogInformation("Candidate Profile page initialized.");
```

### Step 10 -- Query Log Analytics

Open the `law-monitoring` Log Analytics Workspace in the Management
subscription and run:

``` kusto
AppServiceConsoleLogs
| where TimeGenerated > ago(15m)
| where ResultDescription contains "Candidate Profile page initialized."
| project TimeGenerated, ResultDescription
| order by TimeGenerated desc
```

A matching row confirms that the JobAssistant application log reached
the central workspace.

------------------------------------------------------------------------

## What Was Verified

JA-28 verified the complete path:

``` text
JobAssistant ILogger
        ↓
ASP.NET Core logging
        ↓
Azure App Service
        ↓
AppServiceConsoleLogs
        ↓
Central Log Analytics Workspace
```

This proves that JobAssistant application logs can be centrally
monitored using the existing Azure Landing Zone monitoring
infrastructure.

Application Insights was not required for this initial logging
verification.

------------------------------------------------------------------------

## Common Mistakes

-   **Deploying to the wrong subscription.** Always verify the active
    Azure CLI subscription before deployment.
-   **Zipping the `publish` directory instead of its contents.**
    Application files should be at the root of `JobAssistant.zip`.
-   **Committing deployment artifacts.** Do not commit `publish/` or
    `JobAssistant.zip`.
-   **Leaving test-only logging in the application.** Replace
    deterministic test instrumentation with meaningful operational
    messages after verification.
-   **Expecting logs immediately.** Log Analytics ingestion may take a
    short time.

------------------------------------------------------------------------

## Debugging Tips

If deployment succeeds but the application does not immediately appear:

1.  Refresh the browser after deployment completes.
2.  Confirm App Service reports that the site started successfully.
3.  Verify the deployment package structure.
4.  Verify the configured .NET runtime if startup problems persist.

If application logs do not appear:

1.  Confirm the deployed code emits the expected log.
2.  Trigger the application behavior that generates the log.
3.  Increase the KQL time range.
4.  Confirm the App Service Diagnostic Setting includes console logs.
5.  Confirm the query is running against `law-monitoring`.

------------------------------------------------------------------------

## Lessons Learned

-   `dotnet publish` creates deployment-ready application output.
-   ZIP deployment provides a simple manual App Service deployment
    process.
-   Generated deployment artifacts should remain outside source control.
-   Azure CLI subscription verification is an important deployment
    safety check.
-   ASP.NET Core applications can emit operational logs through
    `ILogger`.
-   Azure App Service can capture application console output.
-   Diagnostic Settings can forward App Service logs to a central Log
    Analytics Workspace.
-   KQL can verify application logging end to end.
-   Application Insights is not required for basic centralized
    application logging.

------------------------------------------------------------------------

## What We Learned

After completing JA-28 you can:

-   Publish JobAssistant for Azure App Service.
-   Create a correctly structured ZIP deployment package.
-   Manually deploy JobAssistant using Azure CLI.
-   Verify the active Azure subscription before deployment.
-   Add application logging using `ILogger`.
-   Generate a predictable application log.
-   Query `AppServiceConsoleLogs` using KQL.
-   Verify that application logs reach a central Log Analytics
    Workspace.

------------------------------------------------------------------------

## Key Takeaways

JA-28 connected application development with cloud operations.

The application remains responsible for emitting meaningful logs:

``` text
JobAssistant → ILogger
```

The Azure infrastructure remains responsible for collecting and
centralizing them:

``` text
App Service
    ↓
Diagnostic Setting
    ↓
Log Analytics
```

This separation allows JobAssistant to use standard .NET logging while
Azure handles the operational logging pipeline.

------------------------------------------------------------------------

## Looking Ahead

The manual deployment and centralized logging workflow now provides a
known-good baseline.

Future milestones can build on this foundation with capabilities such
as:

-   Automated deployment through CI/CD
-   Persistent application data
-   Additional operational logging
-   Structured logging
-   More advanced monitoring
-   Application Insights when deeper application telemetry becomes
    useful
