# JA-31 Add Candidate Profile Skills

## Objective

Extend the JobAssistant Candidate Profile so a user can add, display,
remove, save, and reload skills with years of experience.

By completing this milestone, you will learn how a Blazor component
manages a collection of UI models, validates collection input, maps
collection items to Domain models, and integrates the new data into the
existing Candidate Profile create and update workflow.

------------------------------------------------------------------------

## Prerequisites

Before beginning this tutorial you should understand:

-   Basic C#
-   Blazor components
-   Data binding
-   Button events
-   View Models
-   Component-level state
-   `OnInitializedAsync()`
-   Dependency Injection
-   CandidateProfile Domain model
-   Skill Domain model
-   `ICandidateProfileService`
-   Creating, loading, and updating a Candidate Profile
-   C# records and `with` expressions

JA-31 builds directly on the Candidate Profile behavior established in
JA-25 through JA-27.

------------------------------------------------------------------------

## Concepts Introduced

This milestone introduces:

-   Managing a collection in Blazor component state
-   `List<T>`
-   `foreach`
-   Adding and removing collection items
-   LINQ `Select()`
-   LINQ `Any()`
-   Lambda expressions
-   Case-insensitive string comparison
-   Regular-expression validation
-   Input normalization with `Trim()`
-   Validation feedback
-   Mapping UI collections to Domain collections
-   Restoring Domain collections into UI state
-   Bootstrap tables
-   Scrollable UI regions

------------------------------------------------------------------------

## Step-by-Step Walkthrough

### Step 1 -- Add the Skills Section

Add a Skills section below Professional Summary with inputs for skill
name and years of experience.

``` razor
<h2>Skills</h2>

<div class="mb-3">
    <label class="form-label" for="skillName">Skill</label>
    <input id="skillName"
           class="form-control"
           type="text"
           @bind="_skillModel.Name" />
</div>

<div class="mb-3">
    <label class="form-label" for="skillYearsOfExperience">
        Years of Experience
    </label>
    <input id="skillYearsOfExperience"
           class="form-control"
           type="number"
           min="1"
           @bind="_skillModel.YearsOfExperience" />
</div>
```

`min="1"` communicates the UI constraint that a listed skill must have
at least one year of experience.

Application validation is still required because HTML constraints should
not be the only validation mechanism.

------------------------------------------------------------------------

### Step 2 -- Create the Skill View Model

Create a UI model for the skill currently being entered:

``` csharp
private sealed class SkillModel
{
    public string Name              { get; set; } = string.Empty;
    public int    YearsOfExperience { get; set; }
}
```

Create an instance for the input controls:

``` csharp
private readonly SkillModel _skillModel = new SkillModel();
```

The UI model remains separate from the Domain `Skill` model, following
the View Model pattern already used by Professional Summary.

------------------------------------------------------------------------

### Step 3 -- Maintain a Collection of Skills

A Candidate Profile can contain multiple skills, so the component needs
a collection:

``` csharp
private readonly List<SkillModel> _skills = new List<SkillModel>();
```

The fields have different responsibilities:

``` text
_skillModel
    │
    └── Skill currently being entered

_skills
    │
    └── Skills already added to the Candidate Profile
```

This allows the input fields to be cleared after each successful
addition without losing previously added skills.

------------------------------------------------------------------------

### Step 4 -- Add a Skill

Connect an Add Skill button to an event handler:

``` razor
<button type="button"
        class="btn btn-secondary mb-3"
        @onclick="AddSkill">
    Add Skill
</button>
```

After validation succeeds, create a new `SkillModel` and add it to the
collection:

``` csharp
SkillModel skill = new SkillModel { Name = _skillModel.Name.Trim(), YearsOfExperience = _skillModel.YearsOfExperience };

_skills.Add(skill);
```

`Trim()` removes leading and trailing whitespace before the value is
stored.

Reset the input model:

``` csharp
_skillModel.Name              = string.Empty;
_skillModel.YearsOfExperience = 0;
```

------------------------------------------------------------------------

### Step 5 -- Display Skills in a Table

Render one row for every skill:

``` razor
@if (_skills.Count > 0)
{
    <div style="max-height: 250px; overflow-y: auto;">
        <table class="table">
            <thead>
                <tr>
                    <th>Skill</th>
                    <th>Years of Experience</th>
                    <th style="width: 1%"></th>
                </tr>
            </thead>
            <tbody>
                @foreach (SkillModel skill in _skills)
                {
                    <tr>
                        <td>@skill.Name</td>
                        <td>@skill.YearsOfExperience</td>
                        <td>
                            <button type="button"
                                    class="btn btn-sm btn-outline-danger"
                                    @onclick="() => RemoveSkill(skill)">
                                Remove
                            </button>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
}
```

`foreach` renders one table row for every item in `_skills`.

The table container is limited to `250px`. When the collection grows
beyond that height, the Skills section receives its own vertical
scrollbar instead of continuously increasing the page height.

------------------------------------------------------------------------

### Step 6 -- Remove a Skill

Each row passes its skill to `RemoveSkill()`:

``` razor
@onclick="() => RemoveSkill(skill)"
```

The lambda expression captures the skill represented by that row.

Remove it from the collection:

``` csharp
private void RemoveSkill(SkillModel skill)
{
    _skills.Remove(skill);
}
```

Blazor re-renders and the row disappears. The removal is persisted the
next time the Candidate Profile is saved.

------------------------------------------------------------------------

### Step 7 -- Validate Skill Input

Before adding a skill, reject:

-   Empty or whitespace-only names
-   Years of experience less than one
-   Unsupported characters
-   Duplicate skills

The regular expression is:

``` csharp
Regex.IsMatch(_skillModel.Name, @"^[a-zA-Z0-9 .+#/-]+$")
```

This permits common technology names such as:

``` text
C#
C++
.NET
CI/CD
Node.js
PL/SQL
Azure DevOps
```

`System.Text.RegularExpressions` can be made available through the Web
project's global usings.

------------------------------------------------------------------------

### Step 8 -- Prevent Duplicate Skills

Use LINQ `Any()`:

``` csharp
_skills.Any(skill => string.Equals(skill.Name, _skillModel.Name.Trim(), StringComparison.OrdinalIgnoreCase))
```

`StringComparison.OrdinalIgnoreCase` makes duplicate detection
case-insensitive.

Therefore:

``` text
Azure
azure
AZURE
```

are treated as the same skill.

Using the trimmed input also prevents leading or trailing whitespace
from bypassing duplicate detection.

------------------------------------------------------------------------

### Step 9 -- Display Validation Feedback

Silently rejecting invalid input does not tell the user what needs to be
corrected.

Maintain a validation message:

``` csharp
private string _skillValidationMessage = string.Empty;
```

Clear the previous message at the start of `AddSkill()`, then set a
specific message for the failed rule:

``` csharp
_skillValidationMessage = string.Empty;

if (string.IsNullOrWhiteSpace(_skillModel.Name))
{
    _skillValidationMessage = "Skill is required.";
    return;
}

if (_skillModel.YearsOfExperience <= 0)
{
    _skillValidationMessage = "Years of experience must be at least 1.";
    return;
}

if (!Regex.IsMatch(_skillModel.Name, @"^[a-zA-Z0-9 .+#/-]+$"))
{
    _skillValidationMessage = "Skill contains invalid characters.";
    return;
}

if (_skills.Any(skill => string.Equals(skill.Name, _skillModel.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
{
    _skillValidationMessage = "Skill already exists.";
    return;
}
```

Display the message near the skill controls:

``` razor
@if (!string.IsNullOrEmpty(_skillValidationMessage))
{
    <div class="text-danger mb-2">
        @_skillValidationMessage
    </div>
}
```

The user can now distinguish between missing input, invalid experience,
unsupported characters, and duplicates.

------------------------------------------------------------------------

### Step 10 -- Map Skills When Creating a Candidate Profile

The page stores `SkillModel` objects, while the Domain model requires
`Skill` objects.

Use LINQ `Select()`:

``` csharp
Skills = _skills.Select(skill => new Skill { Name = skill.Name, YearsOfExperience = skill.YearsOfExperience }).ToList(),
```

Conceptually:

``` text
List<SkillModel>
       │
       ▼
    Select()
       │
       ▼
List<Skill>
       │
       ▼
CandidateProfile
```

The lambda expression defines how each `SkillModel` becomes a Domain
`Skill`.

------------------------------------------------------------------------

### Step 11 -- Preserve Skills During Updates

JA-27 established the immutable Candidate Profile update pattern using a
`with` expression.

JA-31 extends that pattern:

``` csharp
DomainCandidateProfile updatedCandidateProfile = _candidateProfile with
{
    ProfessionalSummary = new ProfessionalSummary { Headline = _model.Headline, Summary = _model.Summary, TotalYearsOfExperience = _model.TotalYearsOfExperience },
    Skills              = _skills.Select(skill => new Skill { Name = skill.Name, YearsOfExperience = skill.YearsOfExperience }).ToList()
};
```

Persist and synchronize component state:

``` csharp
await CandidateProfileService.UpdateAsync(updatedCandidateProfile);

_candidateProfile = updatedCandidateProfile;
```

Skills use the existing Candidate Profile service; no separate
persistence service is required.

------------------------------------------------------------------------

### Step 12 -- Restore Saved Skills

When the Candidate Profile loads, map Domain Skills back into UI models:

``` csharp
_skills.Clear();

foreach (Skill skill in _candidateProfile.Skills)
{
    _skills.Add(new SkillModel { Name = skill.Name, YearsOfExperience = skill.YearsOfExperience });
}
```

The mapping direction is:

``` text
Save

SkillModel
    ↓
Skill
    ↓
CandidateProfile
    ↓
ICandidateProfileService

Load

ICandidateProfileService
    ↓
CandidateProfile
    ↓
Skill
    ↓
SkillModel
    ↓
Blazor UI
```

Previously saved skills now reappear when the Candidate Profile page is
loaded.

------------------------------------------------------------------------

### Step 13 -- Verify the Behavior

Run:

``` bash
dotnet watch --project src/JobAssistant.Web/JobAssistant.Web.csproj
```

Verify valid skills such as:

``` text
Azure
Terraform
C#
C++
.NET
CI/CD
Node.js
PL/SQL
Azure DevOps
Microsoft 365
```

Verify invalid cases:

``` text
Blank skill       → rejected
Whitespace only   → rejected
0 years           → rejected
Negative years    → rejected
Azure!             → rejected
Azure@             → rejected
Duplicate Azure   → rejected
azure after Azure → rejected
AZURE after Azure → rejected
```

Verify a name with leading or trailing spaces is trimmed.

Add enough skills to confirm that the table becomes vertically
scrollable.

Then verify persistence:

1.  Add multiple skills.
2.  Click **Save**.
3.  Navigate away.
4.  Return to Candidate Profile.
5.  Verify the saved skills are restored.
6.  Remove a skill.
7.  Click **Save**.
8.  Navigate away.
9.  Return again.
10. Verify the removed skill does not return.

Also verify that Professional Summary continues to save and load
correctly.

Finally run:

``` bash
dotnet build
```

The build should complete successfully.

------------------------------------------------------------------------

## Architecture

JA-31 extends the existing Candidate Profile vertical slice:

``` text
Browser
    │
    ▼
CandidateProfile.razor
    │
    ├── ProfessionalSummaryModel
    │
    └── List<SkillModel>
             │
             ▼
          Mapping
             │
             ▼
CandidateProfile Domain Model
    │
    ├── ProfessionalSummary
    │
    └── List<Skill>
             │
             ▼
ICandidateProfileService
```

The Web layer owns UI state and interaction. The Domain layer defines
Candidate Profile and Skill data. The existing Application service
remains the persistence boundary.

------------------------------------------------------------------------

## Common Mistakes

### Binding Directly to the Domain Skill

Continue using UI models rather than exposing Domain objects directly to
the controls.

### Reusing the Input Object in the Collection

Create a new `SkillModel` before adding it to `_skills`, then reset the
input fields.

### Allowing Empty Skill Names

Use `string.IsNullOrWhiteSpace()` so values containing only spaces are
rejected.

### Forgetting to Normalize the Skill Name

Use `Trim()` before storing and comparing skill names.

### Using Case-Sensitive Duplicate Detection

Without `StringComparison.OrdinalIgnoreCase`, `Azure` and `azure` could
be added as separate skills.

### Making the Regular Expression Too Restrictive

Technology names legitimately contain characters such as `#`, `+`, `.`,
and `/`.

### Saving Professional Summary but Not Skills

Both create and update paths must map `_skills` into the Candidate
Profile.

### Saving Skills but Not Loading Them

`OnInitializedAsync()` must map saved Domain Skills back into `_skills`.

### Silently Rejecting Invalid Input

Display a specific validation message instead of only returning from
`AddSkill()`.

------------------------------------------------------------------------

## Debugging Tips

If Add Skill does nothing:

1.  Check `_skillValidationMessage`.
2.  Verify the skill name is not empty.
3.  Verify years of experience is at least one.
4.  Verify the skill name matches the allowed-character regular
    expression.
5.  Verify the skill is not already in `_skills`.

If a skill appears but disappears after navigation:

1.  Confirm the Candidate Profile was saved.
2.  Verify both create and update mappings assign `Skills`.
3.  Set a breakpoint in `OnInitializedAsync()`.
4.  Inspect `_candidateProfile.Skills`.
5.  Verify Domain Skills are copied into `_skills`.

If removing a skill appears to work but it returns later:

1.  Confirm Save was clicked after removal.
2.  Verify the update mapping uses the current `_skills` collection.
3.  Verify `UpdateAsync()` receives the updated Candidate Profile.

If the table makes the page too long, confirm the fixed-height container
uses `max-height: 250px` and `overflow-y: auto`.

------------------------------------------------------------------------

## Lessons Learned

During JA-31 we learned that:

-   Blazor component state can contain collections as well as individual
    View Models.
-   `List<T>` provides a simple way to manage editable UI collections.
-   `foreach` can render one UI row for each collection item.
-   Lambda expressions can pass the current item to an event handler.
-   LINQ `Any()` is useful for duplicate detection.
-   `StringComparison.OrdinalIgnoreCase` provides case-insensitive
    matching.
-   Regular expressions can enforce controlled character rules.
-   Input should be normalized before it is stored.
-   Validation feedback is more useful than silently rejecting input.
-   LINQ `Select()` can map UI collections to Domain collections.
-   Loading requires the reverse mapping from Domain models back into UI
    models.
-   Existing create and update workflows can be extended without
    creating a separate persistence path.
-   Scrollable UI regions prevent large collections from dominating the
    page.

------------------------------------------------------------------------

## What We Learned

After completing JA-31 you can:

-   Add an editable collection to a Blazor component.
-   Bind controls to an item input model.
-   Add and remove collection items.
-   Render a collection with `foreach`.
-   Validate collection input.
-   Detect duplicates with LINQ.
-   Perform case-insensitive string comparisons.
-   Validate strings with regular expressions.
-   Display validation feedback.
-   Map a collection of View Models to Domain models.
-   Restore Domain collection data into component state.
-   Extend an immutable Candidate Profile update with additional data.
-   Build a scrollable Bootstrap table for a growing collection.

------------------------------------------------------------------------

## Key Takeaways

JA-31 extends the Candidate Profile from a single editable Professional
Summary into a profile that also manages a collection of Skills.

``` text
Enter Skill
    ↓
Validate
    ↓
Add to List<SkillModel>
    ↓
Display in table
    ↓
Save
    ↓
Map to List<Skill>
    ↓
CandidateProfile
    ↓
ICandidateProfileService
```

Loading reverses the mapping:

``` text
ICandidateProfileService
    ↓
CandidateProfile
    ↓
List<Skill>
    ↓
List<SkillModel>
    ↓
Skills table
```

The feature reuses the architecture established by the earlier Candidate
Profile milestones instead of introducing a separate persistence
mechanism.

------------------------------------------------------------------------

## Looking Ahead

The Candidate Profile now supports Professional Summary and Skills.

Future Candidate Profile milestones can reuse the same collection and
mapping patterns for:

-   Work Experience
-   Job Preferences
-   Certifications
-   Education

Those sections can build on the same principles introduced here:
component state, validation, Domain mapping, persistence, and
restoration.
