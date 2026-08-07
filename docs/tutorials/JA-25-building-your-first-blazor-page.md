# JA-25 Building Your First Blazor Page

## Objective

Create the first interactive page in JobAssistant that allows a user to view and edit their professional summary.

By completing this milestone, you will understand the basic building blocks of Blazor pages and how they interact with the Application and Domain layers.

---

## Prerequisites

Before beginning this tutorial you should understand:

- Basic C#
- The JobAssistant solution structure
- Clean Architecture fundamentals
- Dependency Injection basics
- CandidateProfile domain model
- ICandidateProfileService

---

## Concepts Introduced

This milestone introduces the following Blazor concepts:

- Routing
- Navigation
- Interactive render mode
- View Models
- Data binding
- Button events
- Dependency Injection
- Mapping View Models to Domain models
- Global usings

---

## Step-by-Step Walkthrough

### Step 1 – Create the page

Create:

```
CandidateProfile.razor
```

Every Blazor page begins with a route.

```razor
@page "/candidate-profile"
```

This tells the Blazor router which component should be displayed.

---

### Step 2 – Add navigation

Add a new navigation link.

```razor
<NavLink class="nav-link"
         href="candidate-profile">
    Candidate Profile
</NavLink>
```

Navigation now works like this:

```text
NavMenu
    │
    ▼
candidate-profile
    │
    ▼
CandidateProfile.razor
```

---

### Step 3 – Enable interactivity

A Razor component can render HTML without being interactive.

Enable interactivity.

```razor
@rendermode InteractiveServer
```

Without this directive:

- Button clicks do not execute.
- @bind does not update.
- Blazor event handlers never fire.

---

### Step 4 – Build the page

Create the first version of the page.

- Headline
- Professional Summary
- Years of Experience
- Save button

Initially these are plain HTML controls.

The goal is to build the UI before introducing Blazor behavior.

---

### Step 5 – Create a View Model

The page owns its own data.

```csharp
private sealed class ProfessionalSummaryModel
{
    ...
}
```

The page creates one instance.

```csharp
private readonly ProfessionalSummaryModel _model =
    new ProfessionalSummaryModel();
```

The View Model represents the state of the page.

---

### Step 6 – Bind the controls

Connect each control to the View Model.

```razor
<input @bind="_model.Headline" />
```

```text
Textbox
    ⇅
_model.Headline
```

Blazor automatically synchronizes the UI and C#.

---

### Step 7 – Handle button clicks

Connect the Save button.

```razor
<button @onclick="Save">
```

```csharp
private async Task Save()
{
}
```

The browser now executes C# code without JavaScript.

---

### Step 8 – Inject the Application service

The page does not create services.

Instead it requests one.

```razor
@inject ICandidateProfileService CandidateProfileService
```

The page communicates only with the Application layer.

---

### Step 9 – Map the View Model

Convert the View Model into the Domain model.

```text
ProfessionalSummaryModel
            │
            ▼
CandidateProfile
```

The UI never exposes Domain objects directly.

---

### Step 10 – Save the Candidate Profile

Build the Domain model.

Call the Application service.

```text
Browser
    │
    ▼
CandidateProfile.razor
    │
    ▼
ProfessionalSummaryModel
    │
    ▼
CandidateProfile
    │
    ▼
ICandidateProfileService
    │
    ▼
InMemoryCandidateProfileService
```

At this point the page can save a profile.

---

## Architecture

JA-25 introduced the first complete vertical slice through the application.

```text
Browser
    │
    ▼
Blazor UI
    │
    ▼
Application Layer
    │
    ▼
Infrastructure Layer
    │
    ▼
Domain Model
```

Each layer has a single responsibility.

---

## Common Mistakes

### Forgetting InteractiveServer

Symptoms

- Page renders
- Save button does nothing
- @bind appears broken

Solution

```razor
@rendermode InteractiveServer
```

---

### CandidateProfile name collision

Symptoms

The compiler uses the Razor component instead of the Domain model.

Solution

Use:

```csharp
DomainCandidateProfile
```

---

### Binding appears not to update

Remember that HTML controls update on the `change` event by default.

Press **Tab** or click outside the control.

Alternatively use:

```razor
@bind:event="oninput"
```

---

## Debugging Tips

Verify each milestone before moving to the next.

- Build after every step.
- Run after every successful build.
- Test one feature at a time.
- Keep the page working throughout development.

---

## Lessons Learned

During JA-25 we discovered several important lessons.

- A Razor page can render without being interactive.
- InteractiveServer is required for Blazor events.
- View Models simplify UI development.
- The UI should communicate only with the Application layer.
- Mapping keeps the UI independent from the Domain.
- Global usings reduce clutter.
- Small incremental changes make Blazor much easier to learn.

---

## What We Learned

After completing JA-25 you can:

- Build a Blazor page.
- Add navigation.
- Bind controls.
- Handle button events.
- Inject services.
- Call the Application layer.
- Map UI models to Domain models.

---

## Key Takeaways

JA-25 introduced the foundation for every future interactive page.

Every page in JobAssistant will reuse these patterns:

- Routing
- Navigation
- View Models
- Data binding
- Event handling
- Dependency Injection
- Application services

Mastering these concepts now makes the remainder of the UI much easier to understand.

---

## Looking Ahead

The next milestone builds upon JA-25.

Future tutorials will introduce:

- EditForm
- Validation
- Loading existing data
- Updating existing data
- Reusable components
- Entity Framework
- Authentication
- AI integration

Each tutorial will extend the concepts introduced here rather than replacing them.
