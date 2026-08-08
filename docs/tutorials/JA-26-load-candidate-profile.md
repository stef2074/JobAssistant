# JA-26 Load Candidate Profile

## Objective

Load the existing Candidate Profile when the Candidate Profile page is opened.

By completing this milestone, you will learn how Blazor initializes a component, retrieves data from the Application layer, and populates the page before it is displayed.

------------------------------------------------------------------------

## Prerequisites

Before beginning this tutorial you should understand:

-   Basic C#
-   Clean Architecture fundamentals
-   Dependency Injection
-   Blazor routing and navigation
-   View Models
-   Data binding
-   CandidateProfile Domain model
-   ICandidateProfileService

------------------------------------------------------------------------

## Concepts Introduced

-   Component lifecycle
-   `OnInitializedAsync()`
-   Page initialization
-   Loading data from the Application layer
-   Mapping Domain models to View Models

------------------------------------------------------------------------

## Step-by-Step Walkthrough

### Step 1 -- Override `OnInitializedAsync()`

``` csharp
protected override async Task OnInitializedAsync()
{
}
```

Blazor automatically calls `OnInitializedAsync()` when a component is first initialized. This method is the appropriate place to load data required by the page before it is rendered.

### Step 2 -- Retrieve the Candidate Profile

``` csharp
DomainCandidateProfile? candidateProfile = await CandidateProfileService.GetAsync();
```

Retrieve the existing Candidate Profile through the Application layer.

### Step 3 -- Handle the Empty Case

``` csharp
if (candidateProfile is null)
{
    return;
}
```

Return early if a profile does not yet exist.

### Step 4 -- Populate the View Model

``` csharp
_model.Headline = candidateProfile.ProfessionalSummary.Headline;

_model.Summary = candidateProfile.ProfessionalSummary.Summary;

_model.TotalYearsOfExperience = candidateProfile.ProfessionalSummary.TotalYearsOfExperience;
```

The View Model remains the source of truth for the UI.

### Step 5 -- Verify the Behavior

1.  Run the application.
2.  Open Candidate Profile.
3.  Verify the fields are initially empty.
4.  Enter sample values.
5.  Click **Save**.
6.  Navigate away.
7.  Return to Candidate Profile.
8.  Verify the saved values are displayed.

------------------------------------------------------------------------

## Architecture

``` text
Browser
    │
    ▼
CandidateProfile.razor
    │
    ▼
OnInitializedAsync()
    │
    ▼
ICandidateProfileService
    │
    ▼
DomainCandidateProfile
    │
    ▼
ProfessionalSummaryModel
    │
    ▼
Blazor UI
```

------------------------------------------------------------------------

## Common Mistakes

-   Loading data in the constructor instead of `OnInitializedAsync()`.
-   Forgetting to check for `null`.
-   Binding directly to the Domain model instead of the View Model.

------------------------------------------------------------------------

## Debugging Tips

Set a breakpoint on:

``` csharp
DomainCandidateProfile? candidateProfile = await CandidateProfileService.GetAsync();
```

Navigate to the Candidate Profile page. If the breakpoint is hit, Blazor has called `OnInitializedAsync()`.

------------------------------------------------------------------------

## Lessons Learned

-   Blazor automatically calls `OnInitializedAsync()`.
-   Initialization is the correct place to load page data.
-   Map Domain models into View Models.
-   Keep temporary mapping objects as local variables.

------------------------------------------------------------------------

## What We Learned

After completing JA-26 you can:

-   Initialize a Blazor component.
-   Load data through the Application layer.
-   Map a Domain model into a View Model.
-   Display loaded data.

------------------------------------------------------------------------

## Key Takeaways

Pages that display existing data typically:

1.  Initialize.
2.  Load data.
3.  Map to a View Model.
4.  Display the View Model.

------------------------------------------------------------------------

## Looking Ahead

The next milestone will update an existing Candidate Profile by choosing between create and update operations and refining the save workflow.
