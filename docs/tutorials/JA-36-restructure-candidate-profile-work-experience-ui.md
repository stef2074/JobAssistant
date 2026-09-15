# JA-36: Restructure the Candidate Profile Work Experience UI

## Overview

This tutorial explains how the Candidate Profile Work Experience UI was reorganized into **My Skills** and **My Jobs** while preserving the existing Candidate Profile Domain model and persistence behavior.

The primary change is the **Use This Skill** selection mechanism. A candidate adds skills once under My Skills and then selects the skills used by the Job currently being entered. This replaces the separate skill-selection interface that previously appeared in the Jobs section.

The implementation keeps three different kinds of state separate:

- Candidate Profile skills that belong to the profile.
- Temporary skill selections for the Job currently being edited.
- Skills copied into a completed Job when it is added.

This separation prevents contextual UI state from leaking into `SkillModel` or the Domain model.

## Goals

The restructuring was designed to:

- Present Candidate Profile skills under My Skills.
- Present Job entry and Job History under My Jobs.
- Allow existing Candidate Profile skills to be selected for the current Job.
- Remove the separate Skills Used selection from My Jobs.
- Preserve existing Professional Summary, Skill, Job, Save, and restore behavior.
- Avoid changes to the Candidate Profile Domain model.
- Avoid introducing Job Preferences functionality.

## Component Structure

`CandidateProfile.razor` remains the page-level orchestrator. It composes three focused components:

```text
CandidateProfile.razor
├── ProfessionalSummarySection.razor
├── SkillsSection.razor
└── JobsSection.razor
```

Each component owns its local editing behavior, while the page holds the applied state required to save the complete Candidate Profile.

The final user interface uses fieldsets to visually organize the three sections:

- Professional Summary
- My Skills
- My Jobs

Responsibilities, Achievements, and the Add Job button are contained within My Jobs because they all belong to the Job currently being entered.

## State Model

The page maintains three collections related to skills and Jobs:

```csharp
private readonly List<SkillModel> _skillAppliedState    = new List<SkillModel>();
private readonly List<JobModel>   _jobAppliedState      = new List<JobModel>();
private readonly List<SkillModel> _jobSkillAppliedState = new List<SkillModel>();
```

These collections have distinct responsibilities.

### `_skillAppliedState`

This collection contains the skills that belong to the Candidate Profile. Each item retains the existing Skill Name and Years of Experience values.

### `_jobSkillAppliedState`

This collection contains the Candidate Profile skills selected for the Job currently being entered. It is temporary, contextual editing state.

It is deliberately maintained separately instead of adding a property such as `IsSelected` or `UseThisSkill` to `SkillModel`. Selection applies only while editing one Job and is not an intrinsic property of a skill.

### `_jobAppliedState`

This collection contains Jobs that have been added to the Candidate Profile. Each Job has its own Skills collection containing the skills selected when that Job was added.

## Sharing Skill State Between Components

The page passes the Candidate Profile skills and current Job skill selections to My Skills:

```razor
<SkillsSection SkillAppliedState="_skillAppliedState"
               JobSkillAppliedState="_jobSkillAppliedState"
               SkillApplied="OnSkillApplied" />
```

It passes the same state to My Jobs:

```razor
<JobsSection JobAppliedState="_jobAppliedState"
             SkillAppliedState="_skillAppliedState"
             JobSkillAppliedState="_jobSkillAppliedState"
             JobApplied="OnJobApplied" />
```

Both components therefore operate on the same list instances owned by `CandidateProfile.razor`.

`SkillsSection` exposes the corresponding parameters:

```csharp
[Parameter] public List<SkillModel> SkillAppliedState    { get; set; } = new List<SkillModel>();
[Parameter] public List<SkillModel> JobSkillAppliedState { get; set; } = new List<SkillModel>();
[Parameter] public EventCallback    SkillApplied         { get; set; }
```

The `SkillApplied` callback is invoked after adding or removing a Candidate Profile skill. The parent handler is intentionally empty:

```csharp
private void OnSkillApplied()
{}
```

Invoking the callback causes the parent component to participate in the render cycle, allowing the components that share the skill collections to display the current state.

## Adding Use This Skill

My Skills displays a checkbox for every Candidate Profile skill:

```razor
<input type="checkbox"
       class="form-check-input"
       checked="@JobSkillAppliedState.Any(selectedSkill => selectedSkill.Name == skill.Name)"
       @onchange="eventArgs => JobSkillSelectionChanged(skill, eventArgs)" />
```

The `checked` expression reflects whether the skill is in the current Job selection. The change handler updates the contextual list:

```csharp
private void JobSkillSelectionChanged(SkillModel skill, ChangeEventArgs eventArgs)
{
    bool isSelected = eventArgs.Value is true;

    if (isSelected)
    {
        if (!JobSkillAppliedState.Contains(skill))
        { JobSkillAppliedState.Add(skill); }
    }
    else
    { JobSkillAppliedState.Remove(skill); }
}
```

This mechanism reuses Candidate Profile skills and avoids a second skill-entry workflow inside My Jobs.

## Validating and Adding a Job

Before validating a Job, `JobsSection` synchronizes the Job edit state with the selected skills:

```csharp
_jobEditState.Skills.Clear();
_jobEditState.Skills.AddRange(JobSkillAppliedState);
```

The existing Job validation then checks `_jobEditState`, including the rule that requires at least one skill.

After validation succeeds, the selected skills are copied into the new Job:

```csharp
JobModel job = new JobModel
{
    Employer          = _jobEditState.Employer.Trim(),
    City              = _jobEditState.IsRemote ? string.Empty : _jobEditState.City.Trim(),
    State             = _jobEditState.IsRemote ? string.Empty : _jobEditState.State,
    IsRemote          = _jobEditState.IsRemote,
    OfficialJobTitle  = _jobEditState.OfficialJobTitle.Trim(),
    StartDate         = _jobEditState.StartDate,
    EndDate           = _jobEditState.EndDate,
    IsCurrentPosition = _jobEditState.IsCurrentPosition,
    Responsibilities  = new List<string>(_jobEditState.Responsibilities),
    Achievements      = new List<string>(_jobEditState.Achievements),
    Skills            = new List<SkillModel>(JobSkillAppliedState)
};
```

Creating a new list is important. Each added Job retains the selection made for that Job instead of sharing the temporary selection collection.

After the Job is added, `JobsSection` invokes `JobApplied`:

```csharp
await JobApplied.InvokeAsync();
```

The parent clears the temporary selection:

```csharp
private void OnJobApplied()
{ _jobSkillAppliedState.Clear(); }
```

This resets the Use This Skill checkboxes for the next Job without changing the Skills collection already copied into the added Job.

## Preserving Persistence Behavior

JA-36 does not change the Candidate Profile Domain model. The existing mapping in `CandidateProfile.razor` continues to convert each `JobModel` into a Domain `Job` and maps the Job's selected skills into Domain `Skill` objects:

```csharp
Skills = job.Skills
    .Select(skill => new Skill
    {
        Name              = skill.Name,
        YearsOfExperience = skill.YearsOfExperience
    })
    .ToList()
```

When a Candidate Profile is restored, each Domain Job's Skills collection is mapped back into that Job's `JobModel.Skills` collection. The contextual `_jobSkillAppliedState` collection is not restored because it represents an unfinished Job selection rather than persisted Candidate Profile data.

The following behavior remains unchanged:

- Professional Summary add, validation, save, and restore.
- Candidate Profile Skill add, remove, validation, save, and restore.
- Job validation and the requirement for at least one skill.
- Employer, location, Remote Position, Official Job Title, dates, and Current Position behavior.
- Responsibilities and Achievements.
- Adding and removing Jobs.
- Job History display.
- Candidate Profile create and update behavior.

## Manual Regression Test

Use the following sequence to verify the completed restructuring:

1. Add a valid Professional Summary.
2. Add several Candidate Profile skills with different Years of Experience values.
3. Select multiple skills using Use This Skill.
4. Enter Employer, location, Official Job Title, dates, Responsibilities, and Achievements.
5. Add the Job and verify it appears in Job History with the selected skills.
6. Confirm the Use This Skill selections are cleared for the next Job.
7. Add a remote Job and verify City and State are cleared and disabled.
8. Add a current position and verify End Date is cleared and disabled.
9. Attempt to add a Job without a selected skill and verify validation rejects it.
10. Remove a skill and a Job and verify the displayed collections update.
11. Save the Candidate Profile.
12. Refresh the page and verify the Professional Summary, Candidate Profile skills, Jobs, and each Job's Skills collection are restored.

Finally, verify the solution:

```bash
dotnet build JobAssistant.slnx
dotnet test
```

## Result

The Candidate Profile now provides one clear skill-entry workflow and a contextual mechanism for associating those skills with individual Jobs. The design keeps UI-only selection state out of `SkillModel`, preserves each Job's Skills collection, and leaves the Domain model and persistence behavior unchanged.
