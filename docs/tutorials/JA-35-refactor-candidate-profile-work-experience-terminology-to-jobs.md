# JA-35: Refactor Work Experience to Job/Jobs Terminology

## Overview

This tutorial explains how the Candidate Profile implementation was refactored so an individual employment record is represented as a `Job` and the collection of employment records is represented as `Jobs`.

Before JA-35, the application used `WorkExperience` terminology across the Domain and Web layers. Although that name described the general résumé concept, it made implementation identifiers unnecessarily long and did not align with the planned My Jobs interface.

JA-35 is strictly a terminology and structural refactor. It changes names throughout the application without changing Candidate Profile behavior, validation rules, persisted information, or the visible organization of the user interface.

The separate My Skills and My Jobs layout and the Use This Skill checkbox belong to JA-36 and are intentionally excluded from this work.

## Goals

The refactor was designed to:

- Represent one employment record with the singular term `Job`.
- Represent the employment-history collection with the plural term `Jobs`.
- Apply the terminology consistently across Domain and Web code.
- Update mappings, validation, component communication, and documentation.
- Preserve existing Candidate Profile behavior and persisted data.
- Prepare the implementation for the planned My Jobs UI.

## Terminology Map

The refactor follows a consistent singular/plural convention:

| Previous terminology | New terminology |
| --- | --- |
| `WorkExperience` | `Job` |
| `WorkExperiences` or Work Experience collection | `Jobs` |
| `WorkExperienceModel` | `JobModel` |
| `WorkExperienceValidation` | `JobValidation` |
| `WorkExperienceSection` | `JobsSection` |
| work-experience editing state | job editing state |
| work-experience applied state | job applied state |
| add/remove Work Experience methods | add/remove Job methods |

The word **Work Experience** may remain in user-facing text when it describes the overall résumé section. References to a single employment record use **Job**.

## Files Involved

The production-code refactor affected the following areas:

```text
src/JobAssistant.Domain/CandidateProfiles/
├── CandidateProfile.cs
└── WorkExperience.cs → Job.cs

src/JobAssistant.Web/CandidateProfiles/
├── Components/
│   └── WorkExperienceSection.razor → JobsSection.razor
├── Models/
│   └── WorkExperienceModel.cs → JobModel.cs
├── Validation/
│   └── WorkExperienceValidation.cs → JobValidation.cs
└── Candidates/Pages/
    └── CandidateProfile.razor
```

Relevant documentation and repository structure references must also use the new filenames and terminology.

## Renaming the Domain Type

The Domain type representing one employment record changes from `WorkExperience` to `Job`:

```csharp
public sealed record Job
{
    // Existing employment-record properties remain unchanged.
}
```

Only the type name changes. The existing properties continue to represent:

- Employer
- Location
- Remote status
- Official job title
- Start and end dates
- Responsibilities
- Achievements
- Skills used

The Candidate Profile collection is renamed to `Jobs`:

```csharp
public IReadOnlyList<Job> Jobs { get; init; } = new List<Job>();
```

This establishes a clear relationship:

```text
CandidateProfile
└── Jobs
    └── Job
```

No new Domain properties or behaviors are introduced.

## Renaming the Web Model

The Blazor editing model changes from `WorkExperienceModel` to `JobModel`:

```csharp
public sealed class JobModel
{
    // Existing UI editing properties remain unchanged.
}
```

The model continues to hold the existing Job editing state, including temporary inputs for Responsibilities and Achievements. The refactor does not add a Use This Skill property or any other new state.

References throughout the Web project must use `JobModel`, including:

- Component parameters
- Local variables
- Applied-state collections
- `foreach` loop variables
- Add and remove methods
- Domain-to-Web restore mappings
- Web-to-Domain save mappings

For example:

```csharp
private readonly List<JobModel> _jobAppliedState = new List<JobModel>();
```

## Renaming Validation

The validation type changes from `WorkExperienceValidation` to `JobValidation`:

```csharp
private readonly JobValidation _jobValidation = new JobValidation();
```

The validator continues to enforce the existing rules. JA-35 does not relax, strengthen, or otherwise change Job validation.

Validation fields and methods adopt the same terminology:

```csharp
private readonly List<string> _jobValidationMessages = new List<string>();
private bool _jobValidationModal = false;

private void CloseJobValidationModal()
{ _jobValidationModal = false; }
```

User-facing validation messages referring to one record should use **Job**. General résumé-section wording may continue to use **Work Experience**.

## Renaming the Blazor Component

The component is renamed from:

```text
WorkExperienceSection.razor
```

to:

```text
JobsSection.razor
```

The parent page composes the renamed component and supplies Job-based parameters and callbacks:

```razor
<JobsSection JobAppliedState="_jobAppliedState"
             SkillAppliedState="_skillAppliedState"
             JobApplied="OnJobApplied" />
```

Within `JobsSection.razor`, implementation identifiers follow the new terminology:

```csharp
[Parameter] public List<JobModel> JobAppliedState { get; set; } = new List<JobModel>();
[Parameter] public EventCallback JobApplied { get; set; }

private readonly JobModel _jobEditState = new JobModel();
```

Methods are renamed consistently:

```csharp
private async Task AddJob()
{
    // Existing add behavior remains unchanged.
}

private void RemoveJob(JobModel job)
{ JobAppliedState.Remove(job); }
```

The Job History loop also uses the singular `job` variable:

```razor
@foreach (JobModel job in JobAppliedState)
{
    <!-- Existing Job History markup remains unchanged. -->
}
```

## Updating Save Mappings

Candidate Profile Save continues to translate Web editing models into Domain models. The mapping changes its type and collection names while preserving every value:

```csharp
Jobs = _jobAppliedState.Select(job => new Job
{
    Employer         = job.Employer,
    City             = job.IsRemote ? null : job.City,
    State            = job.IsRemote ? null : job.State,
    IsRemote         = job.IsRemote,
    OfficialJobTitle = job.OfficialJobTitle,
    StartDate        = new DateOnly(job.StartDate!.Value.Year, job.StartDate.Value.Month, 1),
    EndDate          = job.IsCurrentPosition || job.EndDate is null
        ? null
        : new DateOnly(job.EndDate.Value.Year, job.EndDate.Value.Month, 1),
    Responsibilities = new List<string>(job.Responsibilities),
    Achievements     = new List<string>(job.Achievements),
    Skills           = job.Skills.Select(skill => new Skill
    {
        Name              = skill.Name,
        YearsOfExperience = skill.YearsOfExperience
    }).ToList()
}).ToList()
```

The mapping remains responsible for:

- Converting empty remote location fields to `null`.
- Storing dates at month precision using the first day of the month.
- Representing a current position with a null end date.
- Copying Responsibilities, Achievements, and Skills.

## Updating Restore Mappings

When the Candidate Profile loads, each Domain `Job` is restored into a `JobModel`:

```csharp
foreach (Job job in _candidateProfile.Jobs)
{
    _jobAppliedState.Add(new JobModel
    {
        Employer          = job.Employer,
        City              = job.City ?? string.Empty,
        State             = job.State ?? string.Empty,
        IsRemote          = job.IsRemote,
        OfficialJobTitle  = job.OfficialJobTitle,
        StartDate         = new DateTime(job.StartDate.Year, job.StartDate.Month, 1),
        EndDate           = job.EndDate is null
            ? null
            : new DateTime(job.EndDate.Value.Year, job.EndDate.Value.Month, 1),
        IsCurrentPosition = job.EndDate is null,
        Responsibilities  = new List<string>(job.Responsibilities),
        Achievements      = new List<string>(job.Achievements),
        Skills            = job.Skills.Select(skill => new SkillModel
        {
            Name              = skill.Name,
            YearsOfExperience = skill.YearsOfExperience
        }).ToList()
    });
}
```

This is a type and identifier rename only. Previously persisted employment-history data continues to populate the same UI fields.

## Preserving Behavior

The refactor must leave these behaviors unchanged:

- Professional Summary add, validation, persistence, and restore.
- Candidate Profile Skill add, remove, validation, persistence, and restore.
- Employer and location editing.
- Remote Position behavior.
- Official Job Title editing.
- Start Date, End Date, and Current Position behavior.
- Responsibility and Achievement management.
- The existing Skills Used behavior.
- Job validation.
- Adding and removing employment-history records.
- Job History display.
- Candidate Profile create and update behavior.
- Candidate Profile restoration after a page refresh.

JA-35 must not add the Use This Skill checkbox, visually reorganize My Skills and My Jobs, or introduce Job Preferences.

## Finding Stale Terminology

After completing the rename, search the source tree for stale implementation identifiers:

```bash
rg -n \
  --glob '!bin/**' \
  --glob '!obj/**' \
  'WorkExperience|workExperience|work_experience' \
  src
```

Review every result rather than replacing all occurrences blindly. A user-facing **Work Experience** heading may remain because it describes the résumé section rather than the implementation type.

Also verify the renamed files are tracked correctly:

```bash
git status --short
git diff --stat
```

## Manual Regression Test

Use the following sequence to confirm that the terminology refactor did not change behavior:

1. Add and apply a valid Professional Summary.
2. Add multiple Candidate Profile skills.
3. Add Responsibilities and Achievements to an employment record.
4. Select at least one existing skill using the original Skills Used interface.
5. Add a non-remote Job with City and State.
6. Add a remote Job and verify its location behavior.
7. Add a current position and verify its End Date behavior.
8. Verify all added Jobs appear in Job History.
9. Remove a Job and confirm the remaining history is correct.
10. Save the Candidate Profile.
11. Refresh the page and verify the Professional Summary, Skills, Jobs, Responsibilities, Achievements, and Job Skills are restored.

Finally, verify the solution:

```bash
dotnet build JobAssistant.slnx
dotnet test
```

## Result

The Candidate Profile now uses concise and consistent `Job` and `Jobs` terminology throughout its implementation. The refactor prepares the code for the planned My Jobs interface while preserving the existing Domain data, validation rules, persistence mappings, restore behavior, and user-facing functionality.
