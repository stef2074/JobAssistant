# JA-27 Update Candidate Profile

## Objective

Update an existing Candidate Profile from the Candidate Profile page.

By completing this milestone, you will learn how a Blazor component can
preserve loaded state, choose between create and update operations, and
use immutable C# records to preserve existing Domain data while applying
changes.

------------------------------------------------------------------------

## Prerequisites

Before beginning this tutorial you should understand:

-   Basic C#
-   Clean Architecture fundamentals
-   Dependency Injection
-   Blazor component lifecycle
-   `OnInitializedAsync()`
-   View Models
-   Data binding
-   CandidateProfile Domain model
-   `ICandidateProfileService`
-   Creating and loading a Candidate Profile

------------------------------------------------------------------------

## Concepts Introduced

This milestone introduces the following concepts:

-   Component-level state
-   Variable scope and lifetime
-   Create versus update behavior
-   C# records
-   Value-based equality
-   `init` properties
-   Immutability
-   `with` expressions
-   Preserving existing Domain data during updates
-   Synchronizing component state after persistence

------------------------------------------------------------------------

## Step-by-Step Walkthrough

### Step 1 -- Preserve the Loaded Candidate Profile

In JA-26, the loaded Candidate Profile was only needed during
`OnInitializedAsync()`, so it could be a local variable.

JA-27 needs the loaded profile later when the user clicks **Save**. Its
lifetime therefore needs to match the lifetime of the component.

Add a class-level field:

``` csharp
private DomainCandidateProfile? _candidateProfile;
```

Then load the Candidate Profile into that field:

``` csharp
protected override async Task OnInitializedAsync()
{
    _candidateProfile = await CandidateProfileService.GetAsync();

    if (_candidateProfile is null)
    {
        return;
    }

    _model.Headline =
        _candidateProfile.ProfessionalSummary.Headline;

    _model.Summary =
        _candidateProfile.ProfessionalSummary.Summary;

    _model.TotalYearsOfExperience =
        _candidateProfile.ProfessionalSummary.TotalYearsOfExperience;
}
```

This demonstrates an important design principle:

> A variable's lifetime should match its responsibility.

In JA-26, the Candidate Profile was needed only during initialization.

In JA-27, the Candidate Profile represents the currently loaded profile
for the lifetime of the component.

------------------------------------------------------------------------

### Step 2 -- Decide Between Create and Update

The Save operation now has two possible behaviors.

``` text
Does a Candidate Profile already exist?
            │
      ┌─────┴─────┐
      │           │
     No          Yes
      │           │
CreateAsync()  UpdateAsync()
```

The `_candidateProfile` field tells the page which operation is
required.

``` csharp
if (_candidateProfile is null)
{
    // Create a new Candidate Profile.
}
else
{
    // Update the existing Candidate Profile.
}
```

The page makes the create-or-update decision, while
`ICandidateProfileService` performs the requested persistence operation.

------------------------------------------------------------------------

### Step 3 -- Create a New Candidate Profile

When `_candidateProfile` is `null`, construct a complete new Domain
model.

``` csharp
DomainCandidateProfile candidateProfile = new DomainCandidateProfile
{
    ProfessionalSummary = new ProfessionalSummary
    {
        Headline               = _model.Headline,
        Summary                = _model.Summary,
        TotalYearsOfExperience = _model.TotalYearsOfExperience
    },

    Skills         = new List<Skill>(),
    WorkExperience = new List<WorkExperience>(),

    JobPreferences = new JobPreferences
    {
        DesiredJobTitles   = new List<string>(),
        PreferredLocations = new List<string>(),
        WorkArrangements   = new List<WorkArrangement>(),
        EmploymentTypes    = new List<EmploymentType>()
    }
};

await CandidateProfileService.CreateAsync(candidateProfile);
```

A new Candidate Profile requires values for all required Domain
properties.

------------------------------------------------------------------------

### Step 4 -- Synchronize Component State After Create

After the profile is successfully created, update the component state.

``` csharp
_candidateProfile = candidateProfile;
```

This is important because the page is still open.

Without this assignment, `_candidateProfile` would remain `null`.
Clicking **Save** a second time without navigating away would
incorrectly attempt another create operation.

After the assignment:

``` text
First Save
    │
_candidateProfile is null
    │
CreateAsync()
    │
_candidateProfile = candidateProfile
    │
Second Save
    │
_candidateProfile is not null
    │
UpdateAsync()
```

The component state now accurately represents the state of the
application.

------------------------------------------------------------------------

## Understanding Classes and Records

During JA-27, the difference between a regular C# class and a record
became important.

### Class Equality

Two different class instances are not equal by default simply because
their properties contain the same values.

``` csharp
ProfessionalSummary first = new ProfessionalSummary
{
    Headline = "Azure Cloud Engineer"
};

ProfessionalSummary second = new ProfessionalSummary
{
    Headline = "Azure Cloud Engineer"
};
```

With ordinary class reference equality:

``` csharp
first == second
```

would be `false` because they are different objects.

However:

``` csharp
first.Headline == second.Headline
```

would be `true` because the two string values are equal.

### Record Equality

Records are designed for value-oriented data.

Two records containing the same values can compare as equal even though
they are separate objects.

This makes records useful for Domain models whose data is more important
than the identity of a particular object instance.

------------------------------------------------------------------------

## Immutability and `init`

The Candidate Profile Domain models use `init` properties.

For example:

``` csharp
public required ProfessionalSummary ProfessionalSummary { get; init; }
```

An `init` property can be assigned while an object is being created, but
it cannot normally be reassigned afterward.

This prevents code from freely mutating Domain objects after
construction.

For example, this approach is not available after initialization:

``` csharp
_candidateProfile.ProfessionalSummary = new ProfessionalSummary();
```

Instead, JA-27 preserves immutability and creates a new record based on
the existing one.

------------------------------------------------------------------------

### Step 5 -- Update Using a `with` Expression

Because `CandidateProfile` is a record, C# provides the `with`
expression.

``` csharp
DomainCandidateProfile updatedCandidateProfile = _candidateProfile with
{
    ProfessionalSummary = new ProfessionalSummary
    {
        Headline               = _model.Headline,
        Summary                = _model.Summary,
        TotalYearsOfExperience = _model.TotalYearsOfExperience
    }
};
```

The `with` expression creates a new Candidate Profile based on the
existing record while replacing only the specified value.

Conceptually:

``` text
Existing CandidateProfile
│
├── ProfessionalSummary ── old
├── Skills ──────────────── preserve
├── WorkExperience ──────── preserve
└── JobPreferences ──────── preserve
             │
             │ with
             ▼
Updated CandidateProfile
│
├── ProfessionalSummary ── new
├── Skills ──────────────── preserved
├── WorkExperience ──────── preserved
└── JobPreferences ──────── preserved
```

This prevents the page from accidentally replacing existing profile data
with empty collections during an update.

------------------------------------------------------------------------

### Step 6 -- Persist and Synchronize the Updated Profile

Send the new record to the Application layer.

``` csharp
await CandidateProfileService.UpdateAsync(updatedCandidateProfile);
```

Then synchronize the component state:

``` csharp
_candidateProfile = updatedCandidateProfile;
```

The component now holds the latest version of the Candidate Profile.

------------------------------------------------------------------------

## Final Save Flow

The completed Save behavior follows this pattern:

``` text
User clicks Save
        │
        ▼
Is _candidateProfile null?
        │
   ┌────┴────┐
   │         │
  Yes        No
   │         │
Create      Create updated record
new         using `with`
profile      │
   │         │
CreateAsync UpdateAsync
   │         │
   └────┬────┘
        │
        ▼
Update _candidateProfile
```

Creation constructs the complete Domain model.

Updating derives a new immutable record from the existing Domain model
and replaces only the data edited by the page.

------------------------------------------------------------------------

## Variable Scope and Lifetime

JA-27 reinforces an important C# design principle.

Use a local variable when a value is needed only within one method.

``` csharp
DomainCandidateProfile candidateProfile = new DomainCandidateProfile
{
    // ...
};
```

The newly created `candidateProfile` is only needed inside the create
branch of `Save()`, so it remains local to that block.

Use a class-level field when state must survive across multiple method
calls.

``` csharp
private DomainCandidateProfile? _candidateProfile;
```

Both `OnInitializedAsync()` and `Save()` need `_candidateProfile`, so it
belongs at the component level.

Prefer the smallest scope that satisfies the variable's responsibility.

------------------------------------------------------------------------

## Architecture

JA-27 extends the Candidate Profile flow:

``` text
CandidateProfile.razor
        │
        ├── OnInitializedAsync()
        │       │
        │       ▼
        │   GetAsync()
        │       │
        │       ▼
        │   _candidateProfile
        │
        └── Save()
                │
          ┌─────┴─────┐
          │           │
       Create       Update
          │           │
     CreateAsync   UpdateAsync
          │           │
          └─────┬─────┘
                │
                ▼
        ICandidateProfileService
```

The Web layer determines the user's intent.

The Application layer exposes the operations used to persist the
Candidate Profile.

The Domain model remains immutable.

------------------------------------------------------------------------

## Common Mistakes

### Keeping the Loaded Profile as a Local Variable

A local variable in `OnInitializedAsync()` cannot be used later by
`Save()`.

When multiple component methods need the same state, use a
component-level field.

### Always Calling `CreateAsync()`

Once a Candidate Profile exists, subsequent saves should update it
rather than create it again.

### Forgetting to Update `_candidateProfile` After Create

If `_candidateProfile` remains `null` after creation, a second Save on
the same page will incorrectly follow the create path again.

### Reconstructing the Entire Profile During Update

Creating a completely new Candidate Profile with empty collections can
discard existing Skills, Work Experience, Job Preferences, or future
Domain data.

Use the existing record as the source of truth and replace only the data
being edited.

### Making Domain Properties Mutable Just to Simplify Updates

Changing `init` properties to `set` would make direct mutation easier,
but it would weaken the immutable Domain model.

Use the record `with` expression instead.

------------------------------------------------------------------------

## Verification

Test both create and update behavior.

### Create

1.  Start the application with an empty in-memory store.
2.  Open Candidate Profile.
3.  Enter profile values.
4.  Click **Save**.

The first Save should call `CreateAsync()`.

### Update Without Navigating Away

1.  Remain on the Candidate Profile page after the first Save.
2.  Change one or more Professional Summary values.
3.  Click **Save** again.

The second Save should call `UpdateAsync()` because `_candidateProfile`
was synchronized after creation.

### Reload

1.  Navigate away from Candidate Profile.
2.  Return to Candidate Profile.
3.  Verify the updated values are displayed.

This confirms the complete flow:

``` text
Create
  ↓
Synchronize component state
  ↓
Update
  ↓
Reload
  ↓
Display updated values
```

------------------------------------------------------------------------

## Lessons Learned

During JA-27 we learned that:

-   Variable lifetime should match variable responsibility.
-   Component-level state is appropriate when multiple lifecycle or
    event methods need the same value.
-   Create and update operations have different semantics.
-   C# records provide value-oriented behavior.
-   `init` properties support immutable Domain models.
-   `with` expressions create updated records without mutating the
    original record.
-   Existing Domain data should be preserved when a page edits only part
    of an aggregate.
-   Component state should be synchronized after successful persistence
    operations.

------------------------------------------------------------------------

## What We Learned

After completing JA-27 you can:

-   Preserve loaded data as Blazor component state.
-   Decide between create and update operations.
-   Understand the practical difference between classes and records.
-   Work with `init`-only Domain properties.
-   Use C# `with` expressions.
-   Update one part of an immutable record while preserving the rest.
-   Keep component state synchronized with persisted application state.

------------------------------------------------------------------------

## Key Takeaways

JA-27 completed the basic Candidate Profile create-read-update flow.

``` text
Create
   ↓
Read
   ↓
Update
```

More importantly, the milestone demonstrated how requirements influence
design.

In JA-26, the loaded Candidate Profile only needed method-level scope.

In JA-27, Save also needed the loaded profile, so it became
component-level state.

The immutable record design then allowed the application to preserve
existing Domain data while safely creating an updated Candidate Profile.

------------------------------------------------------------------------

## Looking Ahead

Future Candidate Profile milestones can build on this foundation by
adding additional profile sections such as:

-   Skills
-   Work Experience
-   Job Preferences
-   Certifications
-   Education

The same principles introduced in JA-27 can be reused as those sections
become editable.
