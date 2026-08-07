# Engineering Principles

## Purpose

This document defines the engineering standards for the JobAssistant project. These principles are intended to keep the codebase consistent, maintainable, testable, and easy to evolve.

When a trade-off exists between writing clever code and writing understandable code, choose the solution that is easiest to read, maintain, and debug.

---

# Core Principles

## Readability Over Cleverness

Code is read far more often than it is written.

Prefer:

- Clear method names
- Descriptive variables
- Small methods
- Explicit control flow

Avoid:

- Clever one-liners
- Unnecessary abstraction
- Deeply nested logic
- Hidden side effects

If additional comments are required to explain the implementation, consider rewriting the code instead.

---

## Single Responsibility

Every class should have one clear responsibility.

Good examples:

- JobImporter
- JobMatcher
- ResumeAnalyzer
- CoverLetterGenerator
- ApplicationTracker

Avoid classes that perform unrelated responsibilities.

---

## Explicit Dependencies

Dependencies must be provided through dependency injection.

Prefer constructor injection for classes.

Example:

```csharp
public sealed class JobMatcher
{
    private readonly IAiClient _aiClient;

    public JobMatcher(IAiClient aiClient)
    {
        _aiClient = aiClient;
    }
}
```

Use the Blazor `@inject` directive for Razor components.

Example:

```razor
@inject ICandidateProfileService CandidateProfileService
```

Avoid service locators, global state, and static dependencies where practical.

---

## Interface-Driven Design

Depend on abstractions rather than implementations.

Examples:

```text
IAiClient
IJobRepository
IResumeRepository
IJobSource
IDocumentStorage
```

Interfaces should represent business capabilities rather than technical implementations.

---

## Keep Layers Independent

Business rules belong in the Domain and Application layers.

The Web layer should coordinate requests.

Infrastructure should provide implementations.

The AI layer should communicate with external AI services only.

---

# Naming Conventions

## Projects

Projects should be named using the pattern:

```text
JobAssistant.Domain
JobAssistant.Application
JobAssistant.Infrastructure
JobAssistant.AI
JobAssistant.Web
```

## Classes

Class names should describe business purpose.

Good:

```text
JobAnalyzer
JobMatcher
ResumeParser
ApplicationTracker
```

Avoid:

```text
Helper
Manager
Utilities
Misc
Processor2
```

## Methods

Methods should be verbs.

Examples:

```text
AnalyzeJob()
GenerateResume()
CalculateMatch()
ImportJob()
SaveApplication()
```

## Variables

Use meaningful names.

Prefer:

```text
jobListing
candidateProfile
matchResult
```

Avoid:

```text
obj
data
temp
x
```

---

# Error Handling

Handle expected failures explicitly.

Retry only transient failures.

Do not swallow exceptions.

Log failures once retries are exhausted.

User-facing messages should be clear and actionable.

---

# Logging

Use Microsoft.Extensions.Logging throughout the application.

Logs should include:

- Operation
- Result
- Duration
- Correlation identifier
- Retry count
- Error category

Never log:

- API keys
- Authentication tokens
- Full resumes
- Sensitive personal information

---

# Configuration

Configuration belongs in:

- appsettings.json
- appsettings.Development.json
- Environment variables
- .NET User Secrets

Do not hard-code:

- API keys
- URLs
- Database paths
- Model names
- Retry counts

---

# AI Usage

AI is an assistant—not the source of truth.

Deterministic code validates AI output before use.

AI should:

- Explain recommendations
- Generate drafts
- Extract structured information

AI should not make irreversible decisions without user approval.

---

# Testing

Business logic should be unit tested.

Infrastructure should be integration tested.

Tests should be:

- Independent
- Repeatable
- Fast

Avoid tests that require live external services unless explicitly marked as integration tests.

---

# Source Control

Commit one logical change per commit.

Commit messages should explain why the change was made.

Examples:

```text
JA-3: Create solution structure

JA-7: Add SQLite persistence

JA-9: Implement AI job matching
```

Do not commit:

- Secrets
- Generated files
- Database files
- Build artifacts

## Branch Naming

Branch names should follow the pattern:

```text
<type>/JA-<number>-<jira-title>
```

Where:

- `<type>` is one of:
  - `feature`
  - `docs`
  - `bugfix`
  - `refactor`
  - `chore`
- `<jira-title>` matches the Jira issue title, converted to lowercase with words separated by hyphens.

Examples:

```text
feature/JA-22-import-resume

docs/JA-21-define-product-vision-and-user-workflow

bugfix/JA-35-fix-job-matching-score

refactor/JA-48-simplify-job-matching-service
```

Using the Jira title keeps Jira issues, Git branches, commit messages, and pull requests aligned and easy to trace.

---

# Documentation

Update documentation whenever architectural or behavioral changes are introduced.

Major design decisions should be recorded as Architecture Decision Records in:

```text
docs/adr/
```

## Tutorials

Create a tutorial for each Jira work item that introduces one or more new concepts, architectural patterns, or significant milestones.

Tutorials should be stored in:

```text
docs/tutorials/
```

### Tutorial Template

Each tutorial should contain the following sections:

- Objective
- Prerequisites
- Concepts Introduced
- Step-by-Step Walkthrough
- Architecture
- Common Mistakes
- Debugging Tips
- Lessons Learned
- What We Learned
- Key Takeaways
- Looking Ahead

### Tutorial Naming

The tutorial title (H1) should follow the format:

```text
# JA-25 Building Your First Blazor Page
```

The filename is derived directly from the title by:

- Preserving the Jira key (for example, `JA-25`) in uppercase.
- Converting the remaining text to lowercase.
- Replacing spaces with hyphens.
- Appending the `.md` extension.

Example:

```text
Title:
# JA-25 Building Your First Blazor Page

Filename:
JA-25-building-your-first-blazor-page.md
```

---

# Simplicity

Build only what is required today.

Future extensibility is valuable, but speculative complexity is not.

Prefer evolving the design incrementally rather than anticipating every possible future requirement.

---

# Professional Standards

Every pull request or commit should improve at least one of the following:

- Readability
- Maintainability
- Reliability
- Testability
- Documentation
- User experience

The project should be approachable by a developer who has never seen the code before.
