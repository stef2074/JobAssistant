# JA-37 Refactor Job Responsibilities and Achievements into Highlights

## Objective

Refactor the Candidate Profile Job implementation to replace separate Responsibilities and Achievements concepts with one Highlights concept.

Each Highlight represents a résumé-ready statement describing meaningful work, technical ownership, project delivery, business impact, measurable accomplishments, or leadership associated with a Job.

The refactor preserves Professional Summary behavior, Candidate Profile Skills, contextual Skill selection, Job details, Job History, Candidate Profile Save behavior, and browser-refresh restoration.

## Prerequisites

- The JA-35 terminology refactor from Work Experience to Jobs is complete.
- The JA-36 Candidate Profile Work Experience UI restructuring is complete.
- The Candidate Profile Domain and Web models are understood.
- The application can be built with the .NET SDK used by `JobAssistant.slnx`.
- Existing Candidate Profile behavior has been reviewed before making structural changes.

## Concepts Introduced

### Unified employment Highlights

Responsibilities and Achievements both represented résumé content associated with a Job. The candidate's résumé presents that content as one ordered list, so the Domain and Web models now use a single `Highlights` collection.

### Separate editing and applied state

`JobModel.Highlight` stores the Highlight currently being entered. `JobModel.Highlights` stores the ordered collection of Highlights already added to the Job being composed.

### Deterministic validation

`JobValidation` requires at least one Highlight before a Job can be added. The Highlight Add action also provides immediate validation when its input is empty.

### Shared contextual validation modal

The existing Job validation modal now identifies whether the failed action was adding a Job or adding a Highlight. This preserves a consistent error experience without introducing a second modal.

## Step-by-Step Walkthrough

### 1. Update the Domain Job model

The Domain model previously stored two collections:

```csharp
public required IReadOnlyList<string> Responsibilities { get; init; }
public required IReadOnlyList<string> Achievements     { get; init; }
```

They were replaced with one collection:

```csharp
public required IReadOnlyList<string> Highlights { get; init; }
```

The Domain continues to represent business data without depending on Web editing state or persistence details.

### 2. Update the Web Job model

The separate Responsibility and Achievement editing properties and collections were replaced with:

```csharp
public string       Highlight  { get; set; } = string.Empty;
public List<string> Highlights { get; set; } = new List<string>();
```

The singular property holds the current input. The plural collection preserves Highlights in the order in which they were added.

Highlight state was not added to `SkillModel`, because Highlights and Skills remain separate concepts.

### 3. Update Job validation

`JobValidation` now requires at least one Highlight:

```csharp
if (model.Highlights.Count == 0)
{validationMessages.Add("At least one highlight is required.");}
```

The existing requirement for at least one selected Skill remains unchanged.

### 4. Consolidate the My Jobs entry interface

The separate Responsibilities and Achievements fieldsets were replaced by one Highlights fieldset.

The fieldset contains:

- One Highlight input.
- An Add button.
- An ordered list of added Highlights.
- A Remove button for each Highlight.
- A scrollable list when the number of Highlights exceeds the visible area.

The input and list use the singular and plural Web-model properties directly:

```razor
@bind="_jobEditState.Highlight"
```

```razor
@foreach (string highlight in _jobEditState.Highlights)
```

### 5. Add and remove Highlights

`AddHighlight` trims valid input, appends it to the collection, and clears the input:

```csharp
_jobEditState.Highlights.Add(_jobEditState.Highlight.Trim());
_jobEditState.Highlight = string.Empty;
```

`RemoveHighlight` removes the selected item from the Job currently being entered:

```csharp
private void RemoveHighlight(string highlight)
{ _jobEditState.Highlights.Remove(highlight); }
```

Clicking Add with empty or whitespace-only input opens the shared validation modal with:

```text
Unable to Add Highlight
Highlight is required.
```

### 6. Update Job creation and edit-state clearing

When a Job is added, its Highlights are copied into a new list:

```csharp
Highlights = new List<string>(_jobEditState.Highlights),
```

After the Job is added, both forms of Highlight editing state are cleared:

```csharp
_jobEditState.Highlight = string.Empty;
_jobEditState.Highlights.Clear();
```

Selected Candidate Profile Skills are still copied into the Job, and the existing callback still clears contextual Use This Skill selections afterward.

### 7. Update Job History

Job History now displays one Highlights section:

```razor
<h5 class="mt-3">Highlights</h5>

<ul class="mb-0">
    @foreach (string highlight in job.Highlights)
    {
        <li>@highlight</li>
    }
</ul>
```

Job History is grouped in a nested fieldset within My Jobs. Individual Job cards continue to display the employer, location or remote status, official title, dates, Highlights, and Skills used.

### 8. Update Candidate Profile mappings

Candidate Profile create and update mappings now copy `JobModel.Highlights` into the Domain model:

```csharp
Highlights = new List<string>(job.Highlights),
```

The restore mapping copies Domain Highlights back into the Web model:

```csharp
Highlights = new List<string>(job.Highlights),
```

These mappings preserve Candidate Profile Save behavior and browser-refresh restoration through the existing Candidate Profile service.

### 9. Refine the My Profile interface

The visible page-level My Profile heading was removed because the selected navigation item already identifies the page. The browser title remains `My Profile`.

The navigation label was changed from Candidate Profile to My Profile while the existing route and internal Candidate Profile terminology remain unchanged.

### 10. Update documentation

The Candidate Profile domain-model documentation now lists Highlights beneath Jobs and defines their business purpose.

The README now describes:

- Job Highlights instead of Responsibilities and Achievements.
- Highlight entry, removal, validation, ordering, and scrolling.
- The requirement for at least one Highlight and one Skill.
- Job History placement within My Jobs.
- The My Profile user-facing navigation label.

Historical tutorials retain the terminology that accurately describes the implementation at the time each ticket was completed.

## Architecture

The refactor preserves the existing Candidate Profile flow:

```text
JobsSection.razor
        │
        ▼
JobModel.Highlight / JobModel.Highlights
        │
        ▼
CandidateProfile.razor mappings
        │
        ▼
Domain Job.Highlights
        │
        ▼
ICandidateProfileService
```

Responsibilities remain separated by layer:

- Domain defines the Job and its ordered Highlights.
- Web models hold Blazor editing state.
- `JobValidation` validates the Web Job model.
- `JobsSection.razor` coordinates Job-entry and Job History behavior.
- `CandidateProfile.razor` maps between Web and Domain models and coordinates persistence.
- The existing service retains the Candidate Profile during the application process lifetime.

No database or serialization migration was introduced because the current service stores the Candidate Profile in memory rather than in durable persistence.

## Common Mistakes

### Updating only the UI

Changing labels without changing the Domain model, Web model, validation, and mappings leaves stale concepts and causes build failures.

### Using a global text replacement without review

Responsibilities and Achievements appeared in markup, methods, state, mappings, and validation. Each use needed to be evaluated in context rather than renamed blindly.

### Merging Highlights with Skills

Highlights describe work performed and impact delivered. Skills identify capabilities used in the role. Combining them would weaken the domain model.

### Adding Highlight state to SkillModel

Highlight editing belongs to `JobModel`. Adding it to `SkillModel` would mix unrelated responsibilities and incorrectly persist contextual UI state.

### Changing historical tutorials

Historical tutorials document earlier implementation states. Their original Responsibility and Achievement terminology must remain intact.

### Assuming durable persistence exists

The current implementation uses an in-memory Candidate Profile service. Adding database or legacy-data migration infrastructure would have expanded the ticket beyond the verified architecture.

## Debugging Tips

### Search for stale identifiers

Use ripgrep to find old implementation terminology:

```bash
rg -n \
  --glob '!bin/**' \
  --glob '!obj/**' \
  'Responsibility|Responsibilities|Achievement|Achievements' \
  src tests
```

A clean implementation returns no matches.

### Verify staged whitespace

Check staged changes before committing:

```bash
git diff --cached --check
```

If Git reports trailing whitespace on an apparently blank line, reveal invisible characters with:

```bash
sed -n '328,334l' \
  src/JobAssistant.Web/CandidateProfiles/Components/JobsSection.razor
```

### Verify the modal target

If the modal title is incorrect, confirm that `_jobValidationTarget` is assigned before `_jobValidationModal` is set to `true` in both `AddJob` and `AddHighlight`.

### Verify restore behavior

If Highlights disappear after a browser refresh, inspect all three Candidate Profile mappings: create, update, and restore.

### Verify contextual Skills

If selected Skills remain checked after adding a Job, confirm that the existing Job-applied callback still clears the contextual selection collection.

## Lessons Learned

- Persistence requirements must be based on the current implementation rather than the planned technology stack.
- A complete terminology refactor requires reviewing the Domain model, Web model, validation, UI, mappings, and documentation together.
- Singular editing state and plural applied state make the input workflow explicit.
- Small visual refinements should be validated with realistic lists and multiple Jobs.
- Shared validation UI can remain consistent when its action target is represented explicitly.

## What We Learned

The previous split between Responsibilities and Achievements did not match the résumé structure used by the candidate. Modeling the résumé concept directly produced a simpler Domain model and a clearer interface.

The work also demonstrated that browser-refresh restoration is not the same as durable persistence. The Candidate Profile survives refresh while the in-memory service remains alive, but it does not survive an application restart or deployment.

## Key Takeaways

- `Job.Highlights` is the single Domain collection for employment Highlights.
- `JobModel.Highlight` and `JobModel.Highlights` keep current input separate from the ordered collection.
- Each Job requires at least one Highlight and one selected Skill.
- Candidate Profile create, update, and restore mappings all copy Highlights.
- Highlights remain distinct from Skills.
- Job History presents Highlights in entry order.
- Professional Summary, Skills, Job details, Save, and restore behavior remain intact.

## Looking Ahead

Future Candidate Profile work may divide My Profile into focused pages for Professional Summary, Technical Skills, Jobs, Education, and Certificates, with a separate View Resume experience. That navigation and page decomposition should be planned and implemented through dedicated Jira work rather than added to the focused JA-37 refactor.
