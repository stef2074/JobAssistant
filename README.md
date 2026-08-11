# JobAssistant

JobAssistant is an AI-powered application that helps users discover relevant job opportunities, analyze job matches, generate tailored application materials, and manage the job application process using modern .NET, OpenAI, and Azure technologies.

The project is being developed as a production-quality portfolio application that emphasizes clean architecture, engineering best practices, and cloud-native development.

---

## Goals

JobAssistant is designed to:

- Discover relevant job opportunities from supported sources.
- Analyze job requirements using AI.
- Compare opportunities against a candidate profile and resume.
- Generate tailored application materials.
- Track applications throughout the job search process.
- Demonstrate modern software engineering and cloud development practices.

---

## Planned Features

- AI-assisted job matching
- Resume analysis
- Tailored resume generation
- Cover letter generation
- Application tracking
- Job search dashboard
- AI-generated match explanations
- Azure deployment
- Infrastructure as Code using Terraform

---

## Technology Stack

### Backend

- C#
- .NET
- ASP.NET Core
- Blazor

### AI

- OpenAI API

### Data

- SQLite
- Entity Framework Core

### Cloud

- Azure App Service
- Azure Storage
- Terraform

### Development

- Git
- GitHub
- Jira
- Visual Studio Code
- Fedora Linux

---

## Repository Structure

```text
JobAssistant/
├── docs/
│   ├── adr/
│   ├── architecture.md
│   └── engineering-principles.md
│
├── infrastructure/
│   └── terraform/
│
├── src/
│   ├── JobAssistant.AI/
│   ├── JobAssistant.Application/
│   ├── JobAssistant.Domain/
│   ├── JobAssistant.Infrastructure/
│   └── JobAssistant.Web/
│
└── tests/
```

---

## Architecture

JobAssistant follows a layered architecture that separates business logic from infrastructure and external services.

```text
Web
    │
    ▼
Application
    │
    ▼
Domain

Infrastructure ─────────► Application

AI ─────────────────────► Application
```

Additional architectural details are available in the project documentation.

---

## Documentation

- [Architecture](docs/architecture.md)
- [Engineering Principles](docs/engineering-principles.md)
- [Architecture Decision Records](docs/adr/README.md)

---

## Development Workflow

Development follows a structured engineering workflow:

1. Create a Jira task.
2. Create a Git feature branch.
3. Implement the change.
4. Commit using the Jira issue key.
5. Complete the Jira task.
6. Merge into the main branch.

This approach provides complete traceability between planning, implementation, and source control.

---

## Manual Azure Deployment

JobAssistant is deployed to Azure App Service in the Development subscription.

The initial deployment process is intentionally manual so the application deployment workflow can be understood and verified before introducing CI/CD.

### Publish

From the repository root:

```bash
dotnet publish \
  src/JobAssistant.Web/JobAssistant.Web.csproj \
  --configuration Release \
  --output ./publish
```

### Package

Package the contents of the publish directory:

```bash
cd publish
zip -r ../JobAssistant.zip .
cd ..
```

### Verify Azure Subscription

Confirm that the Azure CLI is using the Development subscription:

```bash
az account show \
  --query "{Name:name, SubscriptionId:id}" \
  --output table
```

### Deploy

Deploy the package to Azure App Service:

```bash
az webapp deploy \
  --resource-group rg-jobassistant-dev \
  --name app-jobassistant-dev \
  --src-path JobAssistant.zip \
  --type zip
```

### Verify Application

After deployment completes, open the JobAssistant App Service and verify that the application loads and functions correctly.

### Verify Application Logging

JobAssistant application logs are forwarded from Azure App Service to the central Log Analytics Workspace.

To verify application logging:

1. Open the deployed JobAssistant application.
2. Navigate to the Candidate Profile page to generate an application log entry.
3. Open the `law-monitoring` Log Analytics Workspace in the Management subscription.
4. Run the following query:

```kusto
AppServiceConsoleLogs
| where TimeGenerated > ago(15m)
| where ResultDescription contains "Candidate Profile page initialized."
| project TimeGenerated, ResultDescription
| order by TimeGenerated desc
```

A matching result confirms that JobAssistant `ILogger` output is being captured by Azure App Service and forwarded to the central Log Analytics Workspace.

---

## GitHub Actions Azure Authentication

JobAssistant uses OpenID Connect (OIDC) to authenticate GitHub Actions to Azure without storing an Azure client secret or App Service publish profile in GitHub.

Azure authentication is scoped to the GitHub `development` environment and the Azure Development subscription.

### GitHub Environment

The `development` GitHub environment defines the following environment variables:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

These values identify the Azure application registration, Microsoft Entra tenant, and Development subscription used by the workflow.

### OIDC Authentication

The GitHub Actions workflow requires permission to request an OIDC token:

```yaml
permissions:
  id-token: write
  contents: read
```

The authentication job targets the GitHub `development` environment:

```yaml
jobs:
  authenticate:
    environment: development
    runs-on: ubuntu-latest
```

Azure authentication uses `azure/login` with the environment variables:

```yaml
- name: Log in to Azure
  uses: azure/login@v2
  with:
    client-id: ${{ vars.AZURE_CLIENT_ID }}
    tenant-id: ${{ vars.AZURE_TENANT_ID }}
    subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}
```

The Azure application registration contains a federated credential that trusts the JobAssistant GitHub repository and `development` environment.

No Azure client secret is required because GitHub obtains a short-lived OIDC token and exchanges it with Microsoft Entra ID for Azure authentication.

### Verify Authentication

The authentication test workflow can be started manually:

```bash
gh workflow run azure-oidc-test.yml
```

List recent workflow runs:

```bash
gh run list \
  --workflow azure-oidc-test.yml \
  --limit 5
```

The workflow verifies the authenticated Azure subscription using:

```bash
az account show \
  --query "{Name:name, SubscriptionId:id}" \
  --output table
```

A successful authentication test reports the Development subscription, confirming that GitHub Actions can authenticate to Azure using OIDC.

---

## Automated Azure Deployment

JobAssistant is automatically built, published, and deployed to the Development Azure App Service using GitHub Actions.

The deployment workflow is defined in:

```text
.github/workflows/deploy-development.yml
```

The workflow builds on the previously verified manual deployment process and GitHub Actions OIDC authentication configuration.

### Deployment Workflow

The Development deployment workflow performs the following stages:

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
Deploy to Azure App Service
```

The workflow currently uses `workflow_dispatch` so deployments are started manually while the deployment process is being developed and verified.

### Build and Publish

The workflow restores and builds the solution in Release configuration:

```bash
dotnet restore
dotnet build --configuration Release --no-restore
```

The JobAssistant Web application is then published:

```bash
dotnet publish \
  src/JobAssistant.Web/JobAssistant.Web.csproj \
  --configuration Release \
  --no-build \
  --output ./publish
```

### Azure Authentication

The deployment job uses the GitHub `development` environment and authenticates to Azure using the existing OIDC configuration.

No Azure client secret or App Service publish profile is required.

Before deployment, the workflow verifies that the authenticated Azure subscription is Development.

### App Service Deployment

The published application is deployed using `azure/webapps-deploy`:

```yaml
- name: Deploy to Azure App Service
  uses: azure/webapps-deploy@v3
  with:
    app-name: app-jobassistant-dev
    package: ./publish
```

The deployment target is:

- Subscription: Development
- Resource Group: `rg-jobassistant-dev`
- App Service: `app-jobassistant-dev`

### Run the Deployment

Start the Development deployment workflow manually:

```bash
gh workflow run deploy-development.yml
```

List recent workflow runs:

```bash
gh run list \
  --workflow deploy-development.yml \
  --limit 5
```

A specific workflow run can be monitored using:

```bash
gh run watch <run-id> --interval 3
```

### Verify Deployment

After the workflow completes successfully:

1. Open the Development JobAssistant App Service.
2. Verify that the application loads successfully.
3. Navigate to the Candidate Profile page.
4. Verify that the application behaves as expected.

### Verify Application Logging

Application logging can be verified in the central `law-monitoring` Log Analytics Workspace using:

```kusto
AppServiceConsoleLogs
| where TimeGenerated > ago(15m)
| where ResultDescription contains "Candidate Profile page initialized."
| project TimeGenerated, ResultDescription
| order by TimeGenerated desc
```

A matching result confirms that the GitHub Actions-deployed application is running successfully and that JobAssistant `ILogger` output continues to flow through Azure App Service to the central Log Analytics Workspace.

---

## Project Status

JobAssistant is currently under active development.

The current focus is establishing the project foundation, including:

- Repository structure
- Architecture
- Engineering standards
- Solution structure
- Core domain model

---

## Roadmap

High-level development phases:

- Repository Foundation
- Solution Foundation
- Domain Model
- Infrastructure
- AI Integration
- User Interface
- Azure Deployment

---

## Contributing

This project is currently maintained by the repository owner.

---

## License

This project is licensed under the MIT License.

See the [LICENSE](LICENSE) file for details.
