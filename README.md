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
