# JA-29 Configure GitHub Actions OIDC Authentication

## Objective

Configure GitHub Actions to authenticate JobAssistant to Azure using OpenID Connect (OIDC) without storing a long-lived Azure client secret or App Service publish profile in GitHub.

By completing this milestone, you will learn how GitHub Actions requests an OIDC token, how Azure validates that token through a federated credential, and how to verify that the workflow authenticated to the correct Azure subscription.

---

## Prerequisites

Before beginning this tutorial you should understand:

- GitHub Actions fundamentals
- GitHub environments
- Azure CLI
- Microsoft Entra application registrations
- Azure subscriptions
- OpenID Connect at a conceptual level
- The JobAssistant Development environment
- The Azure federated credential configured for JobAssistant

The Azure-side federation must already exist before the GitHub Actions workflow can authenticate successfully.

---

## Concepts Introduced

This milestone introduces:

- GitHub Actions OIDC authentication
- GitHub `workflow_dispatch`
- GitHub environment variables
- `id-token: write`
- `azure/login@v2`
- Microsoft Entra workload identity federation
- Federated credential subject matching
- GitHub immutable OIDC subject identifiers
- Azure subscription verification

---

## Step-by-Step Walkthrough

### Step 1 – Create an Authentication Test Workflow

Create:

```text
.github/workflows/azure-oidc-test.yml
```

The initial workflow should do only one thing: authenticate to Azure and verify the resulting subscription context.

```yaml
name: Azure OIDC Authentication Test

on:
  workflow_dispatch:

permissions:
  id-token: write
  contents: read

jobs:
  authenticate:
    environment: development
    runs-on: ubuntu-latest

    steps:
      - name: Log in to Azure
        uses: azure/login@v2
        with:
          client-id: ${{ vars.AZURE_CLIENT_ID }}
          tenant-id: ${{ vars.AZURE_TENANT_ID }}
          subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}

      - name: Verify Azure authentication
        run: |
          az account show \
            --query "{Name:name, SubscriptionId:id}" \
            --output table
```

This workflow deliberately excludes build, publish, and deployment steps.

The goal is to prove authentication independently before introducing additional CI/CD behavior.

---

## Why `workflow_dispatch`?

The workflow uses:

```yaml
on:
  workflow_dispatch:
```

This allows the workflow to be triggered manually.

During initial authentication setup, manual execution is useful because it keeps testing controlled and avoids running the workflow automatically on every push.

Once the authentication workflow is committed to the default branch, it can be started with:

```bash
gh workflow run azure-oidc-test.yml
```

---

## GitHub OIDC Permissions

GitHub Actions must be allowed to request an OIDC token.

The workflow grants:

```yaml
permissions:
  id-token: write
  contents: read
```

`id-token: write` allows the workflow to request a short-lived OIDC token from GitHub.

`contents: read` provides the standard repository read permission.

No long-lived Azure credential is stored in the workflow.

---

## GitHub Environment

The authentication job targets the GitHub `development` environment:

```yaml
jobs:
  authenticate:
    environment: development
```

The environment contains these variables:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
```

Because these values are configured as environment variables, the workflow reads them through the `vars` context:

```yaml
client-id: ${{ vars.AZURE_CLIENT_ID }}
tenant-id: ${{ vars.AZURE_TENANT_ID }}
subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}
```

Using the `secrets` context for environment variables would return empty values.

---

## Initial Authentication Failure

The first version of the workflow referenced:

```yaml
${{ secrets.AZURE_CLIENT_ID }}
${{ secrets.AZURE_TENANT_ID }}
${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

The workflow failed because those values were not repository secrets.

The GitHub environment contained variables instead.

The Azure login action reported that required values such as `client-id` and `tenant-id` were missing.

The fix was:

1. Target the `development` environment.
2. Change the workflow from `secrets` to `vars`.

---

## Federated Credential Subject Matching

After the GitHub variable issue was fixed, GitHub successfully issued an OIDC token.

Azure still rejected the login because the OIDC token subject did not match the federated credential configured in Microsoft Entra ID.

Azure expected:

```text
repo:stef2074/JobAssistant:environment:development
```

GitHub presented an immutable-ID subject containing owner and repository IDs:

```text
repo:stef2074@51519112/JobAssistant@1303297643:environment:development
```

Federated credential subjects must match exactly.

The Azure federated credential was updated to trust the subject GitHub actually presented.

This was an Azure infrastructure change and remained in the AzureLandingZone project.

---

## Why Immutable IDs Matter

OIDC federation depends on identity claims, not display names alone.

GitHub may include immutable owner and repository identifiers in the OIDC subject claim.

The practical lesson is:

> Always compare the actual OIDC subject GitHub sends with the federated credential subject Azure expects.

Do not assume the subject format.

---

## Authentication Flow

The final authentication path is:

```text
GitHub Actions
      │
      ▼
development environment
      │
      ▼
GitHub environment variables
      │
      ▼
OIDC token request
      │
      ▼
GitHub OIDC token
      │
      ▼
Microsoft Entra federated credential
      │
      ▼
azure/login@v2
      │
      ▼
Azure CLI authenticated session
      │
      ▼
Development subscription
```

No Azure client secret is required.

---

## Verify the Workflow

Trigger the workflow:

```bash
gh workflow run azure-oidc-test.yml
```

List recent runs:

```bash
gh run list \
  --workflow azure-oidc-test.yml \
  --limit 5
```

Watch a specific run:

```bash
gh run watch <run-id>
```

Inspect the workflow logs:

```bash
gh run view <run-id> --log
```

---

## Verify the Azure Subscription

The workflow runs:

```bash
az account show \
  --query "{Name:name, SubscriptionId:id}" \
  --output table
```

The successful JA-29 run returned the Development subscription.

This proves that:

- GitHub obtained an OIDC token.
- Azure accepted the token.
- `azure/login@v2` authenticated successfully.
- The workflow authenticated to the intended Development subscription.

---

## Common Mistakes

### Using `secrets` for GitHub Environment Variables

If the values are configured as environment variables, use:

```yaml
${{ vars.AZURE_CLIENT_ID }}
```

not:

```yaml
${{ secrets.AZURE_CLIENT_ID }}
```

### Forgetting the GitHub Environment

Environment-level variables are available only when the job targets that environment.

Use:

```yaml
environment: development
```

### Missing `id-token: write`

Without:

```yaml
id-token: write
```

GitHub Actions cannot request the OIDC token required for workload identity federation.

### Federated Credential Subject Mismatch

Azure rejects the token if the federated credential subject does not exactly match the subject GitHub presents.

Inspect the GitHub Actions login logs and compare the actual `subject claim` with the Azure federated credential.

### Assuming Authentication Means the Correct Subscription

A successful Azure login is not enough.

Always verify the active subscription with:

```bash
az account show
```

---

## Debugging Tips

If `azure/login` reports missing `client-id` or `tenant-id`:

1. Confirm the GitHub environment contains the expected values.
2. Confirm the job targets the correct environment.
3. Confirm the workflow uses `vars` for environment variables.

If Azure reports:

```text
AADSTS700213
No matching federated identity record found
```

compare:

```text
GitHub token subject
```

with:

```text
Azure federated credential subject
```

They must match exactly.

Useful Azure CLI command:

```bash
az ad app federated-credential list \
  --id <client-id> \
  --query "[].{Name:name,Subject:subject}" \
  --output table
```

Useful GitHub command:

```bash
gh run view <run-id> --log-failed
```

---

## Lessons Learned

During JA-29 we learned that:

- OIDC eliminates the need for a long-lived Azure client secret in GitHub.
- GitHub Actions requires `id-token: write` to obtain an OIDC token.
- GitHub environment variables use the `vars` context.
- Environment variables are only available to jobs targeting that environment.
- Azure federated credentials validate the exact OIDC subject claim.
- GitHub may use immutable owner and repository identifiers in the subject.
- Authentication should be tested independently before adding deployment steps.
- A successful login should always be followed by subscription verification.

---

## What We Learned

After completing JA-29 you can:

- Create a manually triggered GitHub Actions workflow.
- Request a GitHub OIDC token.
- Authenticate to Azure using `azure/login@v2`.
- Use GitHub environment variables in workflows.
- Understand the role of a Microsoft Entra federated credential.
- Diagnose OIDC subject mismatches.
- Verify the authenticated Azure subscription.
- Authenticate GitHub Actions to Azure without storing a client secret.

---

## Key Takeaways

JA-29 established the trust relationship between GitHub Actions and Azure.

GitHub proves the identity of the workflow:

```text
GitHub Actions
    ↓
OIDC token
```

Azure validates that identity through the federated credential:

```text
OIDC token
    ↓
Microsoft Entra ID
    ↓
Federated credential
```

After successful validation, the workflow receives an authenticated Azure CLI session scoped to the Development subscription.

This provides the authentication foundation required for future automated JobAssistant deployment workflows.

---

## Looking Ahead

The OIDC authentication baseline is now proven.

Future GitHub Actions milestones can safely build on this foundation with:

- .NET restore and build
- Automated tests
- `dotnet publish`
- Artifact packaging
- Azure App Service deployment
- Post-deployment verification
- Deployment logging and monitoring
