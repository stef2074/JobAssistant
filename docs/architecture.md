# JobAssistant Architecture

## Overview

JobAssistant is an AI-assisted job discovery and application management platform.

The application is intended to help a user:

1. Import job opportunities from approved sources.
2. Compare each opportunity against a stored professional profile.
3. Rank and explain job matches.
4. Prepare tailored application materials.
5. Track applications through their lifecycle.
6. Assist with application completion while keeping the user in control of final submission.

The initial implementation will be developed locally on Linux and designed for later deployment to Azure App Service.

---

## Architecture Goals

The architecture should:

- Remain simple enough for a single-user MVP.
- Support incremental development and deployment.
- Keep business rules independent of external services.
- Treat AI providers, job sources, storage, and hosting as replaceable dependencies.
- Separate deterministic decisions from AI-generated recommendations.
- Protect personal data and credentials.
- Support automated testing.
- Avoid unnecessary Azure resources and operational cost.
- Allow the application to grow without requiring an early rewrite.

---

## Design Principles

### Maintainability Over Cleverness

Code should be explicit, readable, and easy to troubleshoot.

Abstractions should be introduced only when they provide a clear benefit. The project should not adopt patterns or infrastructure solely because they may be useful in the future.

### Single Responsibility

Each component should have one clear responsibility and one primary reason to change.

Examples include:

- Job importer
- Profile manager
- Job matcher
- Resume generator
- Application tracker
- OpenAI client
- Job repository

### Dependency Direction

Business logic must not depend directly on infrastructure or external services.

Dependencies flow toward the Domain and Application layers.

```text
Web
 └── Application
      └── Domain

Infrastructure
 └── Application

AI
 └── Application
```

The Domain layer has no dependency on the Web, Infrastructure, or AI layers.

### AI Augments Decisions

AI provides analysis, recommendations, explanations, and generated content.

Deterministic application code enforces rules.

Deterministic rules include:

- Minimum salary
- Accepted locations
- Remote-work requirements
- Employment type
- Work authorization
- Required clearance
- Duplicate detection
- Application status transitions

AI-assisted capabilities include:

- Skill and experience extraction
- Semantic job matching
- Match explanations
- Resume tailoring
- Cover-letter generation
- Screening-answer drafts

AI output must be validated before it is stored or used.

### Human Approval Before Submission

The user remains responsible for reviewing and approving application content.

The initial architecture will not depend on unattended LinkedIn automation. Job discovery and application assistance should use approved sources, imported data, user-provided links, job-alert email, or user-controlled browser interaction.

Final application submission remains a user action unless a supported integration explicitly permits automation.

### Replaceable External Services

External systems are accessed through application-defined interfaces.

Examples include:

```text
IJobSource
IJobMatcher
IResumeAnalyzer
IApplicationRepository
IDocumentStorage
IAiClient
```

This allows implementations to change without changing core business logic.

### Configuration Over Hard-Coding

Environment-specific values must be supplied through configuration.

Examples include:

- Database paths
- OpenAI model names
- API endpoints
- storage locations
- feature flags
- match thresholds
- retry settings

Secrets must not be committed to source control.

---

## System Context

```text
┌───────────────────────────┐
│          User             │
└─────────────┬─────────────┘
              │
              ▼
┌───────────────────────────┐
│     JobAssistant Web      │
│  Blazor / ASP.NET Core    │
└───────┬─────────┬─────────┘
        │         │
        │         └──────────────────────┐
        ▼                                ▼
┌───────────────────┐          ┌───────────────────┐
│ Application Layer │          │ External Job Data │
│ Use Cases         │          │ Alerts / Imports  │
└─────────┬─────────┘          └───────────────────┘
          │
    ┌─────┴───────────────┐
    ▼                     ▼
┌───────────────┐   ┌───────────────────┐
│ Domain Layer  │   │ AI Integration    │
│ Rules/Models  │   │ OpenAI API        │
└───────┬───────┘   └───────────────────┘
        │
        ▼
┌───────────────────────────┐
│ Infrastructure Layer      │
│ SQLite / Files / Imports  │
└───────────────────────────┘
```

---

## Solution Structure

```text
JobAssistant/
├── docs/
│   ├── architecture.md
│   ├── engineering-principles.md
│   └── adr/
├── infrastructure/
│   └── terraform/
├── src/
│   ├── JobAssistant.AI/
│   ├── JobAssistant.Application/
│   ├── JobAssistant.Domain/
│   ├── JobAssistant.Infrastructure/
│   └── JobAssistant.Web/
└── tests/
```

---

## Project Responsibilities

### JobAssistant.Domain

The Domain project contains the core business model and rules.

It may contain:

- Entities
- Value objects
- Enumerations
- Domain services
- Domain exceptions
- Application-status transition rules

Initial domain concepts may include:

```text
CandidateProfile
Skill
Job
Resume
JobListing
JobRequirement
JobMatch
ApplicationRecord
ApplicationDocument
ScreeningQuestion
```

The Domain project must not reference:

- Entity Framework Core
- ASP.NET Core
- OpenAI libraries
- Azure SDKs
- Browser automation libraries
- File-system implementations

### JobAssistant.Application

The Application project defines use cases and coordinates domain behavior.

It may contain:

- Service interfaces
- Commands and queries
- Use-case handlers
- Validators
- Data-transfer models
- Mapping logic
- Workflow orchestration

Example use cases include:

```text
ImportJob
AnalyzeJob
RankJobs
ApproveJob
GenerateResume
GenerateCoverLetter
RecordApplication
UpdateApplicationStatus
```

The Application project can reference the Domain project.

It must not depend directly on concrete database, AI, email, browser, or cloud implementations.

### JobAssistant.Infrastructure

The Infrastructure project implements persistence and non-AI external integrations.

Initial responsibilities include:

- SQLite persistence
- Entity Framework Core configuration
- Repository implementations
- Local document storage
- Job-alert email import
- Job-description import
- Application data export
- System clock and identifier implementations

Later implementations may include:

- Azure Blob Storage
- Azure-hosted databases
- Queue processing
- Additional job-source integrations

### JobAssistant.AI

The AI project implements AI-specific application interfaces.

Initial responsibilities include:

- OpenAI API communication
- Prompt construction
- Structured response models
- Resume analysis
- Job requirement extraction
- Semantic matching
- Match explanations
- Resume-tailoring drafts
- Cover-letter drafts

The AI project must not contain application workflow or persistence logic.

All AI responses should use structured output where practical and must be validated before use.

### JobAssistant.Web

The Web project is the application entry point and composition root.

Initial responsibilities include:

- Blazor user interface
- ASP.NET Core hosting
- Dependency injection
- Configuration
- Authentication when introduced
- User input validation
- Application workflow presentation
- Logging configuration
- Health endpoints when deployed

The Web project may reference Application, Infrastructure, and AI to register concrete implementations.

Business rules must not be implemented in UI components.

---

## Data Flow

### Job Import and Analysis

```text
Job source or manual entry
          │
          ▼
Normalize job data
          │
          ▼
Check for duplicate
          │
          ▼
Apply deterministic filters
          │
          ▼
Request AI analysis
          │
          ▼
Validate AI response
          │
          ▼
Calculate final match result
          │
          ▼
Store and display recommendation
```

### Application Preparation

```text
User approves job
        │
        ▼
Select base resume
        │
        ▼
Generate tailored draft
        │
        ▼
Generate optional cover letter
        │
        ▼
User reviews documents and answers
        │
        ▼
Open application workflow
        │
        ▼
User submits application
        │
        ▼
Record submission
```

---

## Matching Architecture

The matching process uses both deterministic rules and AI-assisted analysis.

### Deterministic Evaluation

Deterministic code evaluates facts that should not be inferred.

Examples include:

- Location compatibility
- Remote or hybrid preference
- Salary threshold
- Employment type
- Work authorization
- Required security clearance
- Required travel
- Duplicate posting status

A failed mandatory rule may reject a job before an AI request is made.

### AI Evaluation

AI evaluates information that benefits from semantic interpretation.

Examples include:

- Equivalent technologies
- Transferable experience
- Similar responsibilities
- Seniority alignment
- Industry relevance
- Resume evidence for required skills
- Missing qualifications

### Match Result

A match result should include:

```text
Overall score
Required-skills score
Preferred-skills score
Experience score
Location result
Hard-rule result
Recommendation
Strengths
Gaps
Explanation
Model and prompt version
Analysis timestamp
```

The final score calculation must remain visible and testable. AI must not return an unexplained final decision that bypasses application rules.

---

## Persistence

### Initial Development

The initial implementation will use:

- SQLite for structured application data
- Local file storage for resumes and generated documents

SQLite supports local development without requiring a database server.

The database file and user documents must not be committed to source control.

### Future Azure Deployment

The first Azure deployment may continue using a simple architecture:

```text
Azure App Service
Azure Storage account
OpenAI API
```

A managed relational database may be added only when persistence, scale, backup, or multi-user requirements justify it.

The architecture does not require Azure SQL, Service Bus, Functions, Container Apps, or Kubernetes for the MVP.

---

## AI Integration

The OpenAI API is an external dependency and must be isolated behind an application interface.

```text
Application
    │
    ▼
IAiClient
    │
    ▼
OpenAiClient
```

Requirements include:

- API keys loaded from secure configuration
- No API keys committed to Git
- Structured request and response models
- Request timeouts
- Retry handling for transient failures
- Token and request usage logging without storing secrets
- Prompt versioning
- Response validation
- Protection against unsupported or fabricated application claims

AI-generated content is always considered a draft until approved by the user.

---

## Security and Privacy

JobAssistant may process sensitive personal and employment information.

The architecture must:

- Store only information required by the application.
- Never log API keys, access tokens, resumes, or sensitive screening answers.
- Keep secrets outside source control.
- Restrict document and database access to the application user.
- Encrypt traffic when deployed.
- Use managed identity for Azure resources when practical.
- Avoid storing LinkedIn credentials.
- Require explicit approval before sending or submitting user information.
- Make generated and imported data removable.

During local development, the OpenAI API key should be stored with .NET user secrets or environment variables.

---

## Logging and Observability

The application will use `ILogger<T>` for structured logging.

Logs should record:

- Operation name
- Result
- Duration
- Correlation identifier
- External service name
- Retry attempts
- Error category

Logs must not contain:

- API keys
- Authentication tokens
- Full resumes
- Full job descriptions unless explicitly enabled for local debugging
- Sensitive screening responses
- Personally identifiable data not needed for troubleshooting

Detailed cloud monitoring will be added only when the application is deployed and the operational need justifies it.

---

## Error Handling and Resilience

External operations may fail and must be handled explicitly.

The application should:

- Use bounded retries for transient failures.
- Avoid retrying validation and authorization failures.
- Apply timeouts to external requests.
- Preserve imported jobs when later analysis fails.
- Allow failed analysis to be retried.
- Prevent duplicate application submission records.
- Display actionable errors to the user.
- Log the final failure after retries are exhausted.

---

## Testing Strategy

Testing should focus on business behavior.

### Unit Tests

Unit tests should cover:

- Deterministic filters
- Match-score calculations
- Application-status transitions
- Duplicate detection
- Validation
- AI response parsing and validation

### Integration Tests

Integration tests should cover:

- SQLite repositories
- OpenAI client boundaries using test doubles or recorded fixtures
- Import workflows
- Document storage
- Application composition

Automated tests must not depend on live AI calls unless explicitly marked as external integration tests.

---

## Deployment Architecture

### Local Development

```text
Fedora Linux
├── ASP.NET Core / Blazor
├── SQLite
├── Local document storage
└── OpenAI API
```

### Initial Azure Deployment

```text
Azure Resource Group
├── Azure App Service
├── Azure Storage account
└── Application configuration

External
└── OpenAI API
```

Terraform will define Azure infrastructure after the local MVP is functional.

Application infrastructure should remain separate from the Azure Landing Zone foundation. The landing zone establishes the environment; the JobAssistant repository owns the workload resources it deploys into that environment.

---

## Architectural Boundaries

The MVP does not include:

- Unattended LinkedIn scraping
- Storage of LinkedIn credentials
- Automatic submission without user review
- Multiple microservices
- Kubernetes
- Virtual machines
- Enterprise messaging infrastructure
- Multi-region deployment
- Premature database or cloud-service complexity

These may be reconsidered only when a verified requirement justifies them.

---

## Architecture Decision Records

Significant decisions should be documented under:

```text
docs/adr/
```

Potential initial records include:

```text
ADR-001-use-layered-clean-architecture.md
ADR-002-use-sqlite-for-local-persistence.md
ADR-003-use-openai-api.md
ADR-004-use-blazor-web-app.md
ADR-005-require-user-approval-before-submission.md
```

Architecture Decision Records should describe:

- Context
- Decision
- Consequences
- Alternatives considered

---

## Evolution

The architecture will evolve incrementally.

New resources, abstractions, projects, and external services should be added only when they solve an existing requirement.

The preferred sequence is:

1. Build and test the core workflow locally.
2. Validate job matching and user review.
3. Add reliable job ingestion.
4. Add application document generation.
5. Deploy the proven application to Azure.
6. Add cloud services only where operational requirements justify them.
