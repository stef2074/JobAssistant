# JA-34 Tutorial: Refactor Candidate Profile UI into Blazor Components

## Overview

JA-34 refactored the Candidate Profile page so its three major UI sections are implemented as focused Blazor components instead of keeping all markup and UI behavior in `CandidateProfile.razor`.

The refactor introduced:

- `ProfessionalSummarySection.razor`
- `SkillsSection.razor`
- `WorkExperienceSection.razor`
- `CandidateProfiles/_Imports.razor`

`CandidateProfile.razor` remains the page-level orchestrator and persistence owner.

This was intentionally a **structural UI refactor**. Existing Professional Summary, Skills, Work Experience, validation, persistence, and restore behavior had to remain unchanged.

---

## 1. Why This Refactor Was Needed

Before JA-34, `CandidateProfile.razor` handled page routing, dependency injection, all three feature sections, validation modal markup, loading, persistence, and UI-to-Domain mapping.

JA-33 had already separated Web UI models and validation. JA-34 continued that separation by extracting the UI sections:

```text
CandidateProfiles/
├── Components/
│   ├── ProfessionalSummarySection.razor
│   ├── SkillsSection.razor
│   └── WorkExperienceSection.razor
├── Models/
│   ├── ProfessionalSummaryModel.cs
│   ├── SkillModel.cs
│   └── WorkExperienceModel.cs
├── Validation/
│   ├── ProfessionalSummaryValidation.cs
│   ├── SkillValidation.cs
│   └── WorkExperienceValidation.cs
└── _Imports.razor
```

The page now describes the high-level composition:

```razor
<ProfessionalSummarySection ProfessionalSummaryEditState="_professionalSummaryEditState"
                            ProfessionalSummaryAppliedState="_professionalSummaryAppliedState"
                            ProfessionalSummaryApplied="OnProfessionalSummaryApplied" />

<SkillsSection SkillAppliedState="_skillAppliedState"
               SkillApplied="OnSkillApplied" />

<WorkExperienceSection WorkExperienceAppliedState="_workExperienceAppliedState"
                       Skills="_skillAppliedState" />
```

---

## 2. Component Responsibilities

The final responsibility split is:

```text
CandidateProfile.razor
├── page routing and dependency injection
├── Candidate Profile loading
├── Candidate Profile persistence
├── Domain mapping
├── cross-component applied state
└── component orchestration

ProfessionalSummarySection.razor
├── Professional Summary markup
├── validation
├── Apply behavior
└── validation modal

SkillsSection.razor
├── Skills markup
├── Skill edit state
├── validation
├── Add/Remove behavior
└── validation modal

WorkExperienceSection.razor
├── Work Experience markup
├── Work Experience edit state
├── validation
├── Responsibilities
├── Achievements
├── Skills Used selection
├── Add/Remove behavior
└── validation modal
```

The page remains an **orchestrator** rather than an empty wrapper or a monolithic UI component.

---

## 3. Parent and Child Components

`CandidateProfile.razor` is the parent:

```text
CandidateProfile.razor
        │
        ├── ProfessionalSummarySection.razor
        ├── SkillsSection.razor
        └── WorkExperienceSection.razor
```

Communication has two directions:

```text
Parent → Child
    [Parameter]

Child → Parent
    EventCallback / EventCallback<T>
```

---

## 4. `[Parameter]`: Parent-to-Child State

Professional Summary exposes:

```csharp
[Parameter]
public ProfessionalSummaryModel ProfessionalSummaryEditState { get; set; } = new ProfessionalSummaryModel();

[Parameter]
public ProfessionalSummaryModel ProfessionalSummaryAppliedState { get; set; } = new ProfessionalSummaryModel();
```

The parent supplies:

```razor
<ProfessionalSummarySection ProfessionalSummaryEditState="_professionalSummaryEditState"
                            ProfessionalSummaryAppliedState="_professionalSummaryAppliedState"
                            ProfessionalSummaryApplied="OnProfessionalSummaryApplied" />
```

The mapping is:

```text
CandidateProfile.razor                  ProfessionalSummarySection.razor

_professionalSummaryEditState       →   ProfessionalSummaryEditState
_professionalSummaryAppliedState    →   ProfessionalSummaryAppliedState
```

The child can bind directly to the supplied edit state:

```razor
<input id="Headline"
       class="form-control"
       type="text"
       @bind="ProfessionalSummaryEditState.Headline" />
```

Because these models are reference types, the parameter refers to the same object supplied by the parent rather than an automatic independent copy.

---

## 5. Mutable Reference Types

The same principle applies to `List<T>`.

The parent owns:

```csharp
private readonly List<SkillModel> _skillAppliedState = new List<SkillModel>();
```

The child receives:

```csharp
[Parameter]
public List<SkillModel> SkillAppliedState { get; set; } = new List<SkillModel>();
```

Conceptually:

```text
CandidateProfile._skillAppliedState
                │
                └───────────────┐
                                ▼
                       List<SkillModel>
                                ▲
                                │
SkillsSection.SkillAppliedState ┘
```

When the child mutates `SkillAppliedState`, the parent already sees the changed collection.

However, **shared data and UI rerendering are not the same thing**.

That distinction exposed an important JA-34 bug.

---

## 6. The Skills Synchronization Bug

After Skills was extracted, adding a Skill immediately updated `SkillsSection`, but Work Experience did not immediately show that Skill under **Skills Used**.

The structure was:

```text
CandidateProfile
├── SkillsSection
│      └── mutates shared _skillAppliedState
│
└── WorkExperienceSection
       └── consumes _skillAppliedState
```

The shared list contained the new Skill, but only the child that handled the event rerendered. The parent had not gone through another render cycle, so the sibling UI remained stale.

This demonstrated an important Blazor concept:

> Mutating shared reference state does not by itself guarantee that unrelated parent or sibling markup will rerender.

---

## 7. `EventCallback`: Child-to-Parent Notification

`SkillsSection` exposes:

```csharp
[Parameter]
public EventCallback SkillApplied { get; set; }
```

After Add or Remove:

```csharp
await SkillApplied.InvokeAsync();
```

The parent wires it:

```razor
<SkillsSection SkillAppliedState="_skillAppliedState"
               SkillApplied="OnSkillApplied" />
```

with:

```csharp
private void OnSkillApplied()
{
}
```

The empty method is intentional. Invoking the callback causes the parent event/render cycle:

```text
Add/Remove Skill
      ↓
SkillsSection mutates shared list
      ↓
SkillApplied.InvokeAsync()
      ↓
CandidateProfile event cycle
      ↓
CandidateProfile rerenders
      ↓
WorkExperienceSection receives current Skills
```

The callback therefore communicates that shared state changed and parent-dependent UI must refresh.

---

## 8. `EventCallback<T>`: Returning Applied Data

Professional Summary uses:

```csharp
[Parameter]
public EventCallback<ProfessionalSummaryModel> ProfessionalSummaryApplied { get; set; }
```

After successful Apply:

```csharp
await ProfessionalSummaryApplied.InvokeAsync(ProfessionalSummaryAppliedState);
```

The parent receives the model:

```csharp
private void OnProfessionalSummaryApplied(ProfessionalSummaryModel professionalSummary)
{
    _professionalSummaryAppliedState.Headline               = professionalSummary.Headline;
    _professionalSummaryAppliedState.Summary                = professionalSummary.Summary;
    _professionalSummaryAppliedState.TotalYearsOfExperience = professionalSummary.TotalYearsOfExperience;
}
```

This callback has semantic meaning:

> The user successfully applied a Professional Summary.

Unlike the Skills notification, it carries a `ProfessionalSummaryModel` payload.

---

## 9. Edit State → Applied State → Persisted State

JA-34 clarified the Candidate Profile state lifecycle:

```text
Edit State → Applied State → Save → Persisted State
```

### Edit State

Edit State is what the user is currently changing:

```text
ProfessionalSummaryEditState
_skillEditState
_workExperienceEditState
```

It may be incomplete or invalid.

### Applied State

Applied State has crossed the feature's validation/Add boundary:

```text
_professionalSummaryAppliedState
_skillAppliedState
_workExperienceAppliedState
```

Professional Summary crosses this boundary through **Apply**.

Skills and Work Experience cross it when an item is successfully added.

### Persisted State

Persisted state is represented by the Domain Candidate Profile:

```csharp
_candidateProfile
```

with:

```csharp
_candidateProfile.ProfessionalSummary
_candidateProfile.Skills
_candidateProfile.WorkExperience
```

No redundant UI fields such as `_skillPersistedState` were introduced.

The complete flow is:

```text
User input
    ↓
Edit State
    ↓
Apply / Add
    ↓
Applied State
    ↓
Save
    ↓
Domain Candidate Profile
    ↓
CandidateProfileService
    ↓
Persisted State
```

---

## 10. State Ownership Is Intentionally Asymmetric

The final state organization is not mechanically identical across all components.

Professional Summary:

```text
CandidateProfile.razor
├── _professionalSummaryEditState
└── _professionalSummaryAppliedState

ProfessionalSummarySection.razor
├── ProfessionalSummaryEditState
└── ProfessionalSummaryAppliedState
```

Skills:

```text
CandidateProfile.razor
└── _skillAppliedState

SkillsSection.razor
├── _skillEditState
└── SkillAppliedState
```

Work Experience:

```text
CandidateProfile.razor
└── _workExperienceAppliedState

WorkExperienceSection.razor
├── _workExperienceEditState
└── WorkExperienceAppliedState
```

The guiding rule is:

> State should live at the narrowest level that actually owns or needs it.

The temporary Skill and Work Experience edit models are child concerns. They do not need to become parent fields merely for symmetry.

---

## 11. Professional Summary's Apply Boundary

Professional Summary intentionally separates editing from applying.

For example:

```text
Edit State:
Principal Azure Cloud Engineer

Applied State:
Senior Azure Cloud Engineer
```

Until **Apply** succeeds, the applied card remains unchanged.

The flow is:

```text
ProfessionalSummaryEditState
        ↓
Validate
        ↓
ApplyProfessionalSummary()
        ↓
ProfessionalSummaryAppliedState
        ↓
ProfessionalSummaryApplied callback
        ↓
Parent applied state
```

The final regression test explicitly verified that changing the editor without clicking Apply did not alter the applied card.

---

## 12. Skills State Ownership

`SkillsSection` owns:

```csharp
private readonly SkillModel _skillEditState = new SkillModel();
```

The parent owns:

```csharp
private readonly List<SkillModel> _skillAppliedState = new List<SkillModel>();
```

The child receives the applied collection:

```csharp
[Parameter]
public List<SkillModel> SkillAppliedState { get; set; } = new List<SkillModel>();
```

So:

```text
_skillEditState
      ↓
Validate + Add
      ↓
SkillAppliedState
      ↓
same collection as parent _skillAppliedState
```

The parent does not need the temporary Skill form state.

---

## 13. Work Experience State Ownership

`WorkExperienceSection` owns:

```csharp
private readonly WorkExperienceModel _workExperienceEditState = new WorkExperienceModel();
```

The parent owns the applied collection:

```csharp
private readonly List<WorkExperienceModel> _workExperienceAppliedState = new List<WorkExperienceModel>();
```

The child receives:

```csharp
[Parameter]
public List<WorkExperienceModel> WorkExperienceAppliedState { get; set; } = new List<WorkExperienceModel>();

[Parameter]
public List<SkillModel> Skills { get; set; } = new List<SkillModel>();
```

Candidate Profile Skills feed the Work Experience **Skills Used** choices:

```text
Candidate Profile Skills
        ↓
Work Experience Skills Used
```

That cross-feature relationship is orchestrated by the parent.

---

## 14. Meaningful Naming

JA-34 also cleaned up state names after extraction.

Final terminology:

```text
Professional Summary
ProfessionalSummaryEditState
ProfessionalSummaryAppliedState

Skills
_skillEditState
SkillAppliedState

Work Experience
_workExperienceEditState
WorkExperienceAppliedState
```

Parent fields:

```text
_professionalSummaryEditState
_professionalSummaryAppliedState
_skillAppliedState
_workExperienceAppliedState
```

Another useful rename was:

```csharp
WorkExperienceSkillSelectionChanged(...)
```

instead of:

```csharp
WorkExperienceSkillChanged(...)
```

The method changes whether a Skill is selected for the current Work Experience; it does not change the Skill itself.

Good names describe responsibility and state transitions.

---

## 15. `OnParametersSet()`

`OnParametersSet()` runs after a component receives parameter values from its parent.

Professional Summary uses parameter lifecycle behavior so an already-applied Professional Summary can be displayed correctly after persisted data is loaded.

Conceptually:

```text
Parent loads persisted profile
        ↓
Parent populates applied state
        ↓
Child receives parameters
        ↓
OnParametersSet()
        ↓
Applied card presentation is synchronized
```

This matters because correct data alone is not enough; component-local presentation state must also reflect incoming parameters.

---

## 16. Razor `_Imports.razor` Is Directory-Scoped

The project already had:

```text
src/JobAssistant.Web/Components/_Imports.razor
```

The new components live under:

```text
src/JobAssistant.Web/CandidateProfiles/Components/
```

Those are sibling directory trees:

```text
JobAssistant.Web/
├── Components/
│   └── _Imports.razor
└── CandidateProfiles/
    └── Components/
```

The Candidate Profile components do not inherit imports from the sibling `Components/_Imports.razor`.

The structural fix was:

```text
src/JobAssistant.Web/CandidateProfiles/_Imports.razor
```

with the required Razor namespaces:

```razor
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web
```

That import file applies to its descendant components.

---

## 17. The `_Imports.razor` Interactivity Bug

The missing import produced a useful diagnostic pattern:

- Professional Summary markup rendered
- the parent Candidate Profile page was interactive
- parent-owned events worked
- the child Apply button rendered
- the child Apply handler did not fire
- the Blazor circuit was connected

Adding this temporarily to the child:

```razor
@using Microsoft.AspNetCore.Components.Web
```

made the event work.

That proved the problem was import scope.

The temporary one-file fix was removed and replaced by the correct structural fix in `CandidateProfiles/_Imports.razor`.

The debugging sequence was:

```text
Observe symptom
    ↓
Separate parent vs child behavior
    ↓
Confirm interactive circuit
    ↓
Test a minimal import hypothesis
    ↓
Prove the cause
    ↓
Implement the directory-level fix
```

---

## 18. `GlobalUsings.cs` vs `_Imports.razor`

These solve different problems:

```text
GlobalUsings.cs
    ↓
C# namespace imports

_Imports.razor
    ↓
Razor import/component context
```

`GlobalUsings.cs` centralizes repeated C# namespaces for models, validation, Domain types, and aliases.

`_Imports.razor` supplies Razor-specific imports to `.razor` files according to the Razor directory hierarchy.

One does not replace the other.

---

## 19. Render Mode Inheritance

The page declares:

```razor
@page "/candidate-profile"
@rendermode InteractiveServer
```

The child components participate in that interactive component tree.

During debugging, adding `@rendermode InteractiveServer` to a child was attempted and was not the correct solution. It produced a compile error in that context.

The final design keeps the interactive boundary on the page:

```text
CandidateProfile.razor
@rendermode InteractiveServer
        │
        ├── ProfessionalSummarySection
        ├── SkillsSection
        └── WorkExperienceSection
```

A child event problem should not automatically be interpreted as a missing child render mode.

---

## 20. Why Save Remains in `CandidateProfile.razor`

The **Save** button intentionally stayed at page level:

```text
CandidateProfile
├── ProfessionalSummarySection
├── SkillsSection
├── WorkExperienceSection
└── Save
```

Children own UI interactions.

The parent owns mapping and persistence:

```text
Child components
    ↓
UI state

CandidateProfile.razor
    ↓
Domain mapping + orchestration

ICandidateProfileService
    ↓
persistence boundary
```

This keeps saving the complete Candidate Profile as one page-level responsibility rather than scattering persistence across feature components.

---

## 21. UI Models and Validation Remain Separate

JA-34 preserved the separation created by JA-33:

```text
Razor Component
    ↓
presentation and interaction

UI Model
    ↓
editable Web state

Validation
    ↓
rules for that UI state

Domain Model
    ↓
business/persisted representation
```

The components consume the existing UI models and validation classes; they do not absorb those responsibilities.

No Domain model redesign was required for JA-34.

---

## 22. Controlled Refactor Scope

JA-34 intentionally did not redesign:

- Domain Candidate Profile models
- Application service contracts
- Infrastructure persistence
- validation rules
- Professional Summary Apply semantics
- Skill semantics
- Work Experience semantics

The success criterion was:

```text
Behavior before refactor == Behavior after refactor
```

Keeping the scope structural made regressions easier to identify and validate.

---

## 23. Cross-File Naming Review

After extraction, state names were cleaned up.

A cross-file review caught an old parent parameter name:

```razor
<WorkExperienceSection WorkExperiences="_workExperienceAppliedState"
                       Skills="_skillAppliedState" />
```

while the child parameter had become:

```csharp
public List<WorkExperienceModel> WorkExperienceAppliedState { get; set; }
```

The parent was corrected to:

```razor
<WorkExperienceSection WorkExperienceAppliedState="_workExperienceAppliedState"
                       Skills="_skillAppliedState" />
```

This is a good example of why a rename should be reviewed across the entire parent/child contract, not only in the file where the symbol originated.

---

## 24. Regression Testing

A successful build was necessary, but it was not enough. The component boundaries were manually regression-tested.

### Professional Summary

Verified:

- invalid Apply opens validation modal
- valid Apply updates applied card
- editing without Apply does not change applied card
- re-Apply updates applied state
- Save persists applied state
- refresh restores editor and applied card

### Skills

Verified:

- invalid Add opens validation modal
- valid Skill can be added
- Skill can be removed
- Add immediately updates Work Experience Skills Used
- Remove immediately updates Work Experience Skills Used
- Save persists Skills
- refresh restores Skills
- restored Skills remain available to Work Experience

### Work Experience

Verified:

- Current Position clears/disables End Date
- Remote clears/disables City and State
- Responsibility can be added
- Achievement can be added
- Candidate Profile Skill can be selected as Skills Used
- Work Experience can be added
- history card displays correctly
- Work Experience can be removed from the applied collection
- Save persists Work Experience
- refresh restores Work Experience
- responsibility, achievement, and Skill usage restore correctly

### Final persistence flow

The final test validated:

```text
Edit
  ↓
Apply / Add
  ↓
Save
  ↓
Refresh
  ↓
Restore
```

This confirmed that the page-level persistence boundary remained intact after component extraction.

---

## 25. Build Validation

The final solution build succeeded:

```bash
dotnet build JobAssistant.slnx
```

This validated Razor compilation, component references, parameter names, callback signatures, namespaces, and the final naming cleanup.

---

## 26. Final State Map

| File | State | Variable / Parameter | Ownership |
|---|---|---|---|
| `CandidateProfile.razor` | Persisted | `_candidateProfile` | Parent |
| `CandidateProfile.razor` | Edit | `_professionalSummaryEditState` | Parent |
| `CandidateProfile.razor` | Applied | `_professionalSummaryAppliedState` | Parent |
| `CandidateProfile.razor` | Applied | `_skillAppliedState` | Parent |
| `CandidateProfile.razor` | Applied | `_workExperienceAppliedState` | Parent |
| `ProfessionalSummarySection.razor` | Edit | `ProfessionalSummaryEditState` | Parameter from parent |
| `ProfessionalSummarySection.razor` | Applied | `ProfessionalSummaryAppliedState` | Parameter from parent |
| `SkillsSection.razor` | Edit | `_skillEditState` | Child |
| `SkillsSection.razor` | Applied | `SkillAppliedState` | Parameter from parent |
| `WorkExperienceSection.razor` | Edit | `_workExperienceEditState` | Child |
| `WorkExperienceSection.razor` | Applied | `WorkExperienceAppliedState` | Parameter from parent |

Persisted feature data remains:

```csharp
_candidateProfile.ProfessionalSummary
_candidateProfile.Skills
_candidateProfile.WorkExperience
```

---

## 27. Final Communication Map

```text
                         CandidateProfile.razor
                                  │
          ┌───────────────────────┼────────────────────────┐
          │                       │                        │
          ▼                       ▼                        ▼
ProfessionalSummarySection   SkillsSection       WorkExperienceSection
          │                       │                        │
          │                       │                        │
   Edit/Applied state         Edit state             Edit state
          │                  Applied list            Applied list
          │                       │                        │
          └── callback            └── callback             │
                 │                       │                  │
                 └──────────────► Parent ◄─────────────────┘
                                      │
                                      └── orchestrates shared state
```

The parent is not merely a container. It coordinates feature relationships and persistence.

---

## 28. Lessons Learned

### Component extraction is about responsibility

The page became smaller, but the larger improvement is that each feature now has a clear UI owner.

### State ownership should follow need

Professional Summary, Skills, and Work Experience do not need mechanically identical ownership models.

### Shared reference data does not guarantee sibling rerendering

The Skills bug demonstrated the difference between data mutation and Blazor rendering.

### `EventCallback` can matter even with an empty handler

The event can intentionally trigger the parent lifecycle/render cycle.

### Razor imports are directory-scoped

A sibling `_Imports.razor` does not apply to another directory tree.

### `GlobalUsings.cs` and `_Imports.razor` are different tools

One centralizes C# imports; the other controls Razor import context.

### Render mode belongs at the correct interactive boundary

Child components did not need duplicate `InteractiveServer` declarations.

### Persistence belongs at the page orchestration boundary

Feature components manage feature UI behavior; `CandidateProfile.razor` saves the complete Candidate Profile.

### State names should explain responsibility

`EditState`, `AppliedState`, and `SkillSelectionChanged` make the data flow easier to understand.

### Refactors require behavioral regression testing

Compilation does not prove that Apply, validation, callbacks, sibling synchronization, Save, and reload still work.

---

## 29. Result

Before JA-34:

```text
CandidateProfile.razor
├── Professional Summary markup + behavior
├── Skills markup + behavior
├── Work Experience markup + behavior
├── validation UI
└── persistence
```

After JA-34:

```text
CandidateProfile.razor
├── ProfessionalSummarySection
├── SkillsSection
├── WorkExperienceSection
└── persistence/orchestration
```

JA-34 preserved existing Candidate Profile behavior while establishing a cleaner Blazor UI architecture.

The ticket provided practical experience with:

- component boundaries
- parent/child relationships
- `[Parameter]`
- `EventCallback`
- `EventCallback<T>`
- mutable reference state
- rerender behavior
- component lifecycle
- Razor import scoping
- render-mode inheritance
- state ownership
- orchestration
- persistence boundaries
- cross-file naming
- regression testing after refactoring

These concepts provide a strong foundation for continuing to evolve Candidate Profile without allowing `CandidateProfile.razor` to become a monolithic page again.
