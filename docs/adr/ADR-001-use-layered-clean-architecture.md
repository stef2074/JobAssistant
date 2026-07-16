# ADR-001: Use Layered Clean Architecture

## Status

Accepted

---

## Context

JobAssistant is expected to evolve from a simple local application into a cloud-deployed solution that integrates with AI services, document storage, job sources, and Azure resources.

Without clear architectural boundaries, business rules can become tightly coupled to user interface code, persistence, or external service implementations. This increases maintenance cost, reduces testability, and makes future changes more difficult.

The architecture must support incremental development while keeping the core business logic independent of infrastructure concerns.

---

## Decision

JobAssistant will adopt a layered architecture consisting of five projects:

```text
JobAssistant.Web
        │
        ▼
JobAssistant.Application
        │
        ▼
JobAssistant.Domain

JobAssistant.Infrastructure
        │
        └────────────► Application

JobAssistant.AI
        │
        └────────────► Application
```

### Layer Responsibilities

**JobAssistant.Domain**

Contains business entities, value objects, enumerations, and domain rules.

The Domain project must not depend on any external framework or service.

**JobAssistant.Application**

Contains use cases, interfaces, orchestration, validation, and application workflows.

The Application project defines contracts for infrastructure and AI services but does not implement them.

**JobAssistant.Infrastructure**

Implements persistence and external system integration, including SQLite, repositories, document storage, and future cloud services.

**JobAssistant.AI**

Implements AI-specific services, including OpenAI communication, prompt construction, semantic matching, resume analysis, and document generation.

**JobAssistant.Web**

Provides the Blazor user interface, dependency injection, configuration, and application startup.

### Dependency Rules

- Dependencies always point toward the Domain and Application layers.
- The Domain layer has no knowledge of infrastructure, AI, or UI.
- Business rules must never be implemented in the UI.
- External services must be accessed through application-defined interfaces.

---

## Consequences

### Positive

- Clear separation of responsibilities.
- Improved unit testing.
- Business logic remains independent of infrastructure.
- AI providers and storage implementations can be replaced with minimal impact.
- Supports incremental growth without major restructuring.
- Aligns with the architecture documented in `docs/architecture.md`.

### Negative

- More projects than a single-project application.
- Additional interfaces and dependency injection.
- Slightly higher initial setup effort.

---

## Alternatives Considered

### Single Project

Rejected because business logic, infrastructure, and presentation would become tightly coupled as the application grows.

### Feature Folder Architecture

Rejected because it provides less separation between business logic and infrastructure concerns for this project's expected complexity.

### Onion Architecture

Considered but rejected for the MVP because it introduces additional abstraction that is not currently justified.

### Vertical Slice Architecture

Considered but rejected because the initial application is expected to benefit more from clearly separated business, infrastructure, and AI layers than from feature-based organization.

---

## Rationale

A layered architecture provides the best balance between simplicity, maintainability, and long-term flexibility.

It establishes clear boundaries while avoiding unnecessary complexity, allowing the project to evolve incrementally as new features and Azure resources are introduced.
