# JA-32 Add Candidate Profile Work Experience

## Objective

Extend the JobAssistant Candidate Profile so a user can add, display,
remove, save, and reload Work Experience entries.

Each Work Experience entry captures employer information, structured
location, official job title, employment dates, responsibilities,
achievements, and the Candidate Profile Skills used in the role.

By completing this milestone, you will learn how a Blazor component
manages a more complex collection-based UI model, applies conditional
input behavior, validates related fields, maps UI models to Domain
models, and restores persisted Domain data back into component state.

------------------------------------------------------------------------

## Prerequisites

Before beginning this tutorial you should understand:

-   Basic C#
-   Blazor components
-   Data binding
-   Button and checkbox events
-   View Models
-   Component-level state
-   `List<T>`
-   LINQ `Select()`
-   `OnInitializedAsync()`
-   Dependency Injection
-   C# records and `with` expressions
-   `DateTime` and `DateOnly`
-   CandidateProfile Domain model
-   WorkExperience Domain model
-   Skill Domain model
-   `ICandidateProfileService`
-   Creating, loading, and updating a Candidate Profile

JA-32 builds directly on the Candidate Profile behavior established in
JA-25 through JA-27 and the Candidate Profile Skills implementation from
JA-31.

------------------------------------------------------------------------

## Concepts Introduced

This milestone introduces:

-   Managing nested collections in Blazor component state
-   Structured Work Experience input
-   Conditional fields using Remote and Current Position
-   Month/year HTML inputs
-   Mapping `DateTime` UI values to `DateOnly` Domain values
-   Nullable Domain values
-   Adding and removing responsibilities
-   Adding and removing achievements
-   Selecting Work Experience skills from existing Candidate Profile
    Skills
-   Validation across related fields
-   Displaying multiple validation messages in a modal
-   Mapping nested UI models to Domain models
-   Restoring nested Domain collections into UI state
-   Displaying Work Experience history

------------------------------------------------------------------------

## Step-by-Step Walkthrough

### Step 1 -- Extend the Work Experience Domain Model

The Work Experience Domain model represents employment history
independently of the Blazor UI.

The location is represented using City, State, and an explicit Remote
flag rather than a single location string:

``` csharp
public sealed record WorkExperience
{
    public required string Employer { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public required bool IsRemote { get; init; }
    public required string OfficialJobTitle { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public required IReadOnlyList<string> Responsibilities { get; init; }
    public required IReadOnlyList<string> Achievements { get; init; }
    public required IReadOnlyList<Skill> Skills { get; init; }
}
```

`City` and `State` are nullable because a remote position does not
require a physical location. `EndDate` is nullable because a current
position does not have an employment end date.

------------------------------------------------------------------------

### Step 2 -- Create the Work Experience UI Model

``` csharp
private sealed class WorkExperienceModel
{
    public string       Employer          { get; set; } = string.Empty;
    public string       City              { get; set; } = string.Empty;
    public string       State             { get; set; } = string.Empty;
    public bool         IsRemote          { get; set; }
    public string       OfficialJobTitle  { get; set; } = string.Empty;
    public DateTime?    StartDate         { get; set; }
    public DateTime?    EndDate           { get; set; }
    public bool         IsCurrentPosition { get; set; }
    public string       Responsibility    { get; set; } = string.Empty;
    public string       Achievement       { get; set; } = string.Empty;
    public List<string> Responsibilities  { get; set; } = new List<string>();
    public List<string> Achievements      { get; set; } = new List<string>();
    public List<SkillModel> Skills        { get; set; } = new List<SkillModel>();
}
```

The UI uses nullable `DateTime` values for month inputs. Conversion to
Domain `DateOnly` values occurs when the Candidate Profile is saved.

------------------------------------------------------------------------

### Step 3 -- Maintain Work Experience Component State

``` csharp
private readonly WorkExperienceModel       _workExperienceModel              = new WorkExperienceModel();
private readonly List<WorkExperienceModel> _workExperiences                  = new List<WorkExperienceModel>();
private readonly List<string>              _workExperienceValidationMessages = new List<string>();
private bool                               _showWorkExperienceValidationModal = false;
```

The editor model represents the entry currently being entered.
`_workExperiences` contains completed entries.

------------------------------------------------------------------------

### Step 4 -- Add the Basic Work Experience Fields

The editor captures:

-   Employer
-   City
-   State
-   Remote
-   Official Job Title
-   Start Date
-   End Date
-   Current Position

City uses free text, State uses a U.S. state/DC dropdown, and Remote and
Current Position are explicit Boolean choices.

------------------------------------------------------------------------

### Step 5 -- Handle Remote Positions

``` csharp
private void RemoteChanged()
{
    if (_workExperienceModel.IsRemote)
    {
        _workExperienceModel.City  = string.Empty;
        _workExperienceModel.State = string.Empty;
    }
}
```

City and State are disabled for remote positions. Domain mapping later
stores both values as null.

------------------------------------------------------------------------

### Step 6 -- Handle Current Positions

``` csharp
private void CurrentPositionChanged()
{
    if (_workExperienceModel.IsCurrentPosition)
    {
        _workExperienceModel.EndDate = null;
    }
}
```

The End Date input is disabled for a current position. The Domain
represents current employment with a null `EndDate`.

------------------------------------------------------------------------

### Step 7 -- Use Month/Year Employment Dates

``` razor
<input id="startDate"
       class="form-control"
       type="month"
       @bind="_workExperienceModel.StartDate"
       @bind:format="yyyy-MM" />

<input id="endDate"
       class="form-control"
       type="month"
       disabled="@_workExperienceModel.IsCurrentPosition"
       @bind="_workExperienceModel.EndDate"
       @bind:format="yyyy-MM" />
```

The selected month is normalized to day 1 when converted to Domain
`DateOnly`.

------------------------------------------------------------------------

### Step 8 -- Add Responsibilities

``` csharp
private void AddResponsibility()
{
    if (string.IsNullOrWhiteSpace(_workExperienceModel.Responsibility))
    {
        return;
    }

    _workExperienceModel.Responsibilities.Add(_workExperienceModel.Responsibility.Trim());
    _workExperienceModel.Responsibility = string.Empty;
}

private void RemoveResponsibility(string responsibility)
{
    _workExperienceModel.Responsibilities.Remove(responsibility);
}
```

At least one responsibility is required.

------------------------------------------------------------------------

### Step 9 -- Add Achievements

``` csharp
private void AddAchievement()
{
    if (string.IsNullOrWhiteSpace(_workExperienceModel.Achievement))
    {
        return;
    }

    _workExperienceModel.Achievements.Add(_workExperienceModel.Achievement.Trim());
    _workExperienceModel.Achievement = string.Empty;
}

private void RemoveAchievement(string achievement)
{
    _workExperienceModel.Achievements.Remove(achievement);
}
```

Achievements are optional.

------------------------------------------------------------------------

### Step 10 -- Select Skills Used in the Role

Skills Used are selected only from existing Candidate Profile Skills.

``` razor
@if (_skills.Count == 0)
{
    <p class="text-muted">
        Add Candidate Profile Skills before selecting skills used in this role.
    </p>
}
else
{
    @foreach (SkillModel skill in _skills)
    {
        <div class="form-check">
            <input class="form-check-input"
                   type="checkbox"
                   id="@($"workExperienceSkill-{skill.Name}")"
                   checked="@_workExperienceModel.Skills.Contains(skill)"
                   @onchange="eventArgs => WorkExperienceSkillChanged(skill, eventArgs)" />

            <label class="form-check-label"
                   for="@($"workExperienceSkill-{skill.Name}")">
                @skill.Name
            </label>
        </div>
    }
}
```

``` csharp
private void WorkExperienceSkillChanged(SkillModel skill, ChangeEventArgs eventArgs)
{
    bool isSelected = eventArgs.Value is true;

    if (isSelected)
    {
        if (!_workExperienceModel.Skills.Contains(skill))
        {
            _workExperienceModel.Skills.Add(skill);
        }
    }
    else
    {
        _workExperienceModel.Skills.Remove(skill);
    }
}
```

This prevents Work Experience from becoming a second skill-entry
workflow.

------------------------------------------------------------------------

### Step 11 -- Validate the Work Experience Entry

JA-32 validates:

``` text
Employer                    required
City                        required when not remote
State                       required when not remote
Official Job Title          required
Start Date                  required
End Date                    required unless current position
End Date >= Start Date
Responsibilities            at least one required
Achievements                optional
Skills Used                 at least one required
```

For example:

``` csharp
if (_workExperienceModel.Skills.Count == 0)
{
    _workExperienceValidationMessages.Add(
        "At least one skill used in this role is required.");
}
```

Multiple failures are collected so they can be presented together.

------------------------------------------------------------------------

### Step 12 -- Display Validation in a Modal

``` razor
@if (_showWorkExperienceValidationModal)
{
    <div class="modal fade show d-block"
         tabindex="-1"
         style="background-color: rgba(0, 0, 0, 0.5);">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Unable to Add Work Experience</h5>

                    <button type="button"
                            class="btn-close"
                            aria-label="Close"
                            @onclick="CloseWorkExperienceValidationModal">
                    </button>
                </div>

                <div class="modal-body">
                    <p>Please correct the following:</p>

                    <ul class="mb-0">
                        @foreach (string message in _workExperienceValidationMessages)
                        {
                            <li>@message</li>
                        }
                    </ul>
                </div>

                <div class="modal-footer">
                    <button type="button"
                            class="btn btn-primary"
                            @onclick="CloseWorkExperienceValidationModal">
                        OK
                    </button>
                </div>
            </div>
        </div>
    </div>
}
```

``` csharp
private void CloseWorkExperienceValidationModal()
{
    _showWorkExperienceValidationModal = false;
}
```

------------------------------------------------------------------------

### Step 13 -- Add a Work Experience Entry

After validation succeeds, copy the editor state into a completed entry:

``` csharp
WorkExperienceModel workExperience = new WorkExperienceModel
{
    Employer          = _workExperienceModel.Employer.Trim(),
    City              = _workExperienceModel.IsRemote ? string.Empty : _workExperienceModel.City.Trim(),
    State             = _workExperienceModel.IsRemote ? string.Empty : _workExperienceModel.State,
    IsRemote          = _workExperienceModel.IsRemote,
    OfficialJobTitle  = _workExperienceModel.OfficialJobTitle.Trim(),
    StartDate         = _workExperienceModel.StartDate,
    EndDate           = _workExperienceModel.EndDate,
    IsCurrentPosition = _workExperienceModel.IsCurrentPosition,
    Responsibilities  = new List<string>(_workExperienceModel.Responsibilities),
    Achievements      = new List<string>(_workExperienceModel.Achievements),
    Skills            = new List<SkillModel>(_workExperienceModel.Skills)
};
```

Copying the collections prevents the completed entry from sharing
mutable editor collections.

Reset the editor after adding the entry.

------------------------------------------------------------------------

### Step 14 -- Display Work Experience History

Each history entry displays:

-   Official job title
-   Employer
-   Location
-   Employment dates
-   Responsibilities
-   Achievements when present
-   Skills Used

Remote positions display `Remote`. Current positions display `Present`.

------------------------------------------------------------------------

### Step 15 -- Remove Work Experience

``` csharp
private void RemoveWorkExperience(WorkExperienceModel workExperience)
{
    _workExperiences.Remove(workExperience);
}
```

Save the Candidate Profile after modifying the collection to persist the
change.

------------------------------------------------------------------------

### Step 16 -- Map Work Experience to the Domain

``` csharp
WorkExperience = _workExperiences.Select(workExperience => new WorkExperience
{
    Employer         = workExperience.Employer,
    City             = workExperience.IsRemote ? null : workExperience.City,
    State            = workExperience.IsRemote ? null : workExperience.State,
    IsRemote         = workExperience.IsRemote,
    OfficialJobTitle = workExperience.OfficialJobTitle,

    StartDate = new DateOnly(
        workExperience.StartDate!.Value.Year,
        workExperience.StartDate.Value.Month,
        1),

    EndDate = workExperience.IsCurrentPosition ||
              workExperience.EndDate is null
        ? null
        : new DateOnly(
            workExperience.EndDate.Value.Year,
            workExperience.EndDate.Value.Month,
            1),

    Responsibilities = new List<string>(workExperience.Responsibilities),
    Achievements     = new List<string>(workExperience.Achievements),

    Skills = workExperience.Skills
        .Select(skill => new Skill
        {
            Name              = skill.Name,
            YearsOfExperience = skill.YearsOfExperience
        })
        .ToList()
}).ToList()
```

Important transformations:

``` text
Remote             → City = null, State = null
Current Position   → EndDate = null
UI DateTime        → Domain DateOnly
SkillModel         → Domain Skill
```

------------------------------------------------------------------------

### Step 17 -- Restore Saved Work Experience

``` csharp
_workExperiences.Clear();

foreach (WorkExperience workExperience in _candidateProfile.WorkExperience)
{
    _workExperiences.Add(new WorkExperienceModel
    {
        Employer          = workExperience.Employer,
        City              = workExperience.City ?? string.Empty,
        State             = workExperience.State ?? string.Empty,
        IsRemote          = workExperience.IsRemote,
        OfficialJobTitle  = workExperience.OfficialJobTitle,

        StartDate = new DateTime(
            workExperience.StartDate.Year,
            workExperience.StartDate.Month,
            1),

        EndDate = workExperience.EndDate is null
            ? null
            : new DateTime(
                workExperience.EndDate.Value.Year,
                workExperience.EndDate.Value.Month,
                1),

        IsCurrentPosition = workExperience.EndDate is null,

        Responsibilities = new List<string>(workExperience.Responsibilities),
        Achievements     = new List<string>(workExperience.Achievements),

        Skills = workExperience.Skills
            .Select(skill => new SkillModel
            {
                Name              = skill.Name,
                YearsOfExperience = skill.YearsOfExperience
            })
            .ToList()
    });
}
```

A null Domain `EndDate` restores the UI entry as a current position.

------------------------------------------------------------------------

### Step 18 -- Verify the Behavior

Run:

``` bash
dotnet watch --project src/JobAssistant.Web/JobAssistant.Web.csproj
```

Verify a normal completed position and a remote current position.

Confirm:

-   Required-field validation works.
-   Non-remote positions require City and State.
-   Remote positions do not require City or State.
-   Completed positions require End Date.
-   Current positions do not require End Date.
-   End Date cannot precede Start Date.
-   At least one Responsibility is required.
-   At least one Skill Used is required.
-   Achievements remain optional.
-   Multiple Work Experience entries can be displayed.
-   Saved Work Experience is restored after reload.
-   Professional Summary and Candidate Profile Skills remain intact.

Finally run:

``` bash
dotnet build JobAssistant.slnx
```

The build should complete successfully.

------------------------------------------------------------------------

## Architecture

JA-32 extends the existing Candidate Profile vertical slice:

``` text
Browser
    │
    ▼
CandidateProfile.razor
    │
    ├── ProfessionalSummaryModel
    ├── List<SkillModel>
    └── List<WorkExperienceModel>
              │
              ├── Responsibilities
              ├── Achievements
              └── Skills Used
                       │
                       ▼
                    Mapping
                       │
                       ▼
CandidateProfile Domain Model
    │
    ├── ProfessionalSummary
    ├── List<Skill>
    └── List<WorkExperience>
              │
              ▼
ICandidateProfileService
```

The Web layer owns UI state, interaction, and input validation. The
Domain layer defines Candidate Profile, Skill, and Work Experience data.
The existing Application service remains the persistence boundary.

------------------------------------------------------------------------

## Common Mistakes

### Binding the UI Directly to the Domain Model

The UI has editing concerns that do not belong in the Domain, including
temporary Responsibility and Achievement values and the
`IsCurrentPosition` checkbox.

### Storing Current Position as Duplicate Domain State

A null `EndDate` already represents current employment in the Domain.

### Leaving Location Values on Remote Positions

Remote positions should not retain stale City and State values.

### Treating Month Inputs as Exact Employment Dates

The UI collects month/year. Mapping normalizes the value to day 1 for
the Domain `DateOnly`.

### Sharing Mutable Collections

Completed entries should receive copies of responsibilities,
achievements, and skills rather than sharing the editor collections.

### Creating Skills Inside Work Experience

Skills Used must come from Candidate Profile Skills.

### Forgetting Existing Candidate Profile Data During Save

Saving Work Experience must preserve Professional Summary and Candidate
Profile Skills.

------------------------------------------------------------------------

## Lessons Learned

During JA-32 we learned that:

-   Complex Blazor forms benefit from a UI model separate from the
    Domain model.
-   Explicit UI state can be useful even when the Domain represents the
    same state more simply.
-   Nullable Domain values cleanly represent remote locations and
    current employment.
-   HTML month inputs can use `DateTime` while the Domain preserves
    `DateOnly`.
-   Mapping is the correct place to normalize UI-specific date
    representations.
-   Nested collections should be copied when moving data from an editor
    into a completed entry.
-   Validation can evaluate related fields and present multiple failures
    together.
-   Work Experience can reuse Candidate Profile Skills without creating
    a second skill-entry workflow.
-   Existing Candidate Profile persistence can support new sections
    without a separate service.

------------------------------------------------------------------------

## What We Learned

After completing JA-32 you can:

-   Build a multi-part Work Experience editor in Blazor.
-   Manage nested UI collections.
-   Implement conditional form behavior.
-   Validate related fields together.
-   Display validation messages in a modal.
-   Bind month/year inputs.
-   Convert between UI `DateTime` and Domain `DateOnly`.
-   Represent current employment with a nullable End Date.
-   Represent remote employment with nullable location values.
-   Select data from an existing Candidate Profile collection.
-   Map nested UI data into Domain models.
-   Restore nested Domain data into Blazor component state.
-   Persist Work Experience through an existing application service.

------------------------------------------------------------------------

## Key Takeaways

JA-32 expands Candidate Profile from summary-and-skill data into
structured employment history.

``` text
Candidate Profile
    │
    ├── Professional Summary
    ├── Skills
    └── Work Experience
            │
            ├── Employer / Location / Title
            ├── Employment Dates
            ├── Responsibilities
            ├── Achievements
            └── Skills Used
```

The implementation keeps UI editing concerns in the Web layer, preserves
the Domain representation, and reuses the existing Candidate Profile
service for persistence.

Work Experience Skills are selected from Candidate Profile Skills,
ensuring that the Candidate Profile remains the source of skill data.

A future refactoring can extract the Candidate Profile UI models and
their validation responsibilities from `CandidateProfile.razor` into
separate classes.
