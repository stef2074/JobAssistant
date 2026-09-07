# JA-33 Tutorial --- Refactor Candidate Profile UI Models and Validation

## Overview

JA-33 refactored the Candidate Profile UI so that page-specific state
and validation logic were no longer concentrated inside
`CandidateProfile.razor`.

Before this work, the Razor page was responsible for too many concerns
at once:

``` text
CandidateProfile.razor
├── UI markup
├── editable state
├── applied state
├── validation rules
├── validation messages
├── validation modals
├── Domain model conversion
└── persistence orchestration
```

JA-33 did **not** change Candidate Profile functionality. Instead, it
reorganized the code so that UI state and validation became explicit,
reusable concepts.

The resulting structure was:

``` text
src/JobAssistant.Web/CandidateProfiles/
├── Models/
│   ├── ProfessionalSummaryModel.cs
│   ├── SkillModel.cs
│   └── WorkExperienceModel.cs
└── Validation/
    ├── ProfessionalSummaryValidation.cs
    ├── SkillValidation.cs
    └── WorkExperienceValidation.cs
```

This refactor created a cleaner boundary between UI state, validation,
Domain models, and persistence, and prepared the Candidate Profile page
for the component extraction performed later in JA-34.

## 1. Why UI Models Were Introduced

A Domain model represents the application's business data.

A UI model represents the state needed while a user is interacting with
the interface.

Those two responsibilities are related, but they are not identical.

For example, the Domain layer may represent a Work Experience start date
using `DateOnly`, while the UI may need `DateTime?` because an HTML
month input binds more naturally to a nullable UI value during editing.

The UI also needs temporary values that may never belong in the Domain
model at all:

``` text
Current text being typed
Temporary responsibility
Temporary achievement
Current Position checkbox
Validation state
```

These are interaction concerns rather than business-domain concepts.

That leads to this separation:

``` text
Razor UI
   │
   ▼
UI Model
   │
   ▼
Domain Model
   │
   ▼
Application Service
```

The UI model acts as a buffer between the user's partially completed
form and the Domain model that should contain meaningful application
data.

## 2. Why We Should Not Bind the UI Directly to Domain Models

Suppose the user begins entering Work Experience information:

``` text
Employer: Contoso
Start Date: empty
Job Title: empty
Responsibilities: none
```

That is a perfectly normal intermediate UI state. It is not necessarily
a valid Domain object yet.

Instead of binding the Razor page directly to a Domain type:

``` text
User types
   │
   ▼
WorkExperienceModel
   │
   │ validation succeeds
   ▼
WorkExperience
```

The UI model can temporarily contain incomplete state. Only after
validation succeeds do we convert that state into Domain data.

## 3. The Three Candidate Profile UI Models

JA-33 established dedicated UI models for the three major Candidate
Profile areas.

### ProfessionalSummaryModel

Conceptually:

``` text
ProfessionalSummaryModel
├── Headline
├── Summary
└── TotalYearsOfExperience
```

It mirrors much of the Domain `ProfessionalSummary`, but exists
specifically for the Web UI.

### SkillModel

Conceptually:

``` text
SkillModel
├── Name
└── YearsOfExperience
```

The flow is:

``` text
Skill form
   │
   ▼
_skillModel
   │
   │ Add Skill
   ▼
_skills
   │
   │ Save Candidate Profile
   ▼
List<Skill>
```

### WorkExperienceModel

Work Experience needs substantially more temporary UI state:

``` text
Employer
City
State
Remote
Official Job Title
Start Date
End Date
Current Position
Responsibilities
Achievements
Skills Used
```

Some concepts map directly to the Domain model. Others help the UI
determine how the Domain model should eventually be constructed.

For example:

``` text
UI:
IsCurrentPosition = true

        ↓ Save conversion

Domain:
EndDate = null
```

## 4. UI Models Are Not Domain Models

A useful mental distinction is:

``` text
UI Model
"What is happening in the form right now?"

Domain Model
"What does this Candidate Profile mean to the application?"
```

The UI model can contain incomplete, temporary, or presentation-oriented
state. The Domain model represents meaningful application data.

## 5. Validation Was Extracted from CandidateProfile.razor

JA-33 extracted validation into:

``` text
ProfessionalSummaryValidation
SkillValidation
WorkExperienceValidation
```

The resulting pattern became:

``` text
UI event
   │
   ▼
Validation class
   │
   ├── errors
   │      ▼
   │   validation modal
   │
   └── valid
          ▼
       continue
```

The Razor page decides **when** validation happens, while the validation
class decides **whether the data is valid**.

## 6. ProfessionalSummaryValidation

Professional Summary validation established rules such as:

``` text
Headline is required
Summary is required
Total Years of Experience must be at least 1
```

Conceptually:

``` csharp
validationMessages.Clear();
validationMessages.AddRange(professionalSummaryValidation.Validate(model));

if (validationMessages.Count > 0)
{
    showValidationModal = true;
    return;
}
```

This separates rule definition from the UI response to failed
validation.

## 7. SkillValidation

Skill validation can inspect both the Skill being entered and the
existing Skills collection:

``` text
SkillModel being entered
       +
Existing SkillModel collection
       │
       ▼
SkillValidation
```

The Razor page asks whether the Skill is valid and reacts to the
returned messages rather than implementing every rule itself.

## 8. WorkExperienceValidation

Work Experience has richer validation requirements, including Employer,
Official Job Title, dates, Remote behavior, Responsibilities, and Skills
Used.

The architectural point is that these rules belong to
`WorkExperienceValidation` rather than being spread throughout button
handlers in `CandidateProfile.razor`.

## 9. Validation Returns Messages Instead of Controlling the UI

The validation classes do not open modals or manipulate Razor state.
They return validation results:

``` text
Validation class
     │
     ▼
List<string>
```

The Razor UI decides how those messages are presented.

This gives each side one responsibility:

``` text
Validation:
"What is wrong?"

UI:
"How should we show what is wrong?"
```

## 10. Validation Messages Remain Natural English

Technical identifiers can match code terminology, while validation
messages should remain natural English.

For example:

``` text
Headline is required.
Professional Summary is required.
Total Years of Experience must be at least 1.
```

rather than exposing internal identifier syntax.

## 11. Editable and Applied Professional Summary State

One of the most important JA-33 concepts was the distinction between
editable and applied state:

``` text
_professionalSummaryModel
_appliedProfessionalSummaryModel
```

The first represents what the user is currently editing. The second
represents what the user explicitly accepted by clicking **Apply**.

## 12. Why Two Professional Summary Models Are Necessary

Suppose the applied headline is:

``` text
Azure Cloud Engineer
```

The user changes the input to:

``` text
Senior Cloud Architect
```

but does not click Apply.

At that moment:

``` text
Editable model:
Senior Cloud Architect

Applied model:
Azure Cloud Engineer
```

This creates three deliberate lifecycle stages:

``` text
Edit
   │
   ▼
Apply
   │
   ▼
Save
```

## 13. Edit, Apply, and Save Are Different Concepts

The workflow is:

``` text
User types
   │
   ▼
Editable model
   │
   │ Apply
   ▼
Validation
   │
   ├── invalid → modal
   │
   └── valid
          │
          ▼
     Applied model
          │
          │ Save
          ▼
     Domain model
          │
          ▼
Application service
```

Typing does not automatically mean that the value should be persisted.

## 14. Why Save Uses the Applied Model

Persistence uses `_appliedProfessionalSummaryModel`, not
`_professionalSummaryModel`.

Consider:

``` text
1. Existing headline = Azure Cloud Engineer
2. User edits headline = Senior Cloud Architect
3. User does NOT click Apply
4. User clicks Save
```

Because Save uses the applied model, `Azure Cloud Engineer` remains the
value that is persisted.

If Save used the editable model, the Apply button would have no real
meaning.

## 15. The Applied Summary Card Represents Accepted UI State

After successful Apply, the UI displays a card containing the applied
Professional Summary.

The card communicates:

``` text
"This is the Professional Summary currently accepted for saving."
```

Editing the form therefore should not immediately change the card. The
card changes only after successful Apply.

## 16. Validation Happens Before Applied State Changes

The ordering matters:

``` text
Apply clicked
     │
     ▼
Validate editable model
     │
     ├── invalid
     │      ▼
     │    modal
     │
     └── valid
            │
            ▼
       copy to applied model
```

Invalid data should never become accepted state.

## 17. Existing Persisted Data Must Restore Both States

When a Candidate Profile is loaded, its saved Professional Summary is
already considered accepted.

Therefore persisted values restore both models:

``` text
Persisted Domain ProfessionalSummary
             │
             ├──► editable model
             │
             └──► applied model
```

## 18. Why Both Models Are Populated on Load

If only the editable model were restored, the form would contain the
saved values but the applied state would be empty.

Persisted state represents existing applied state, so both models must
be restored.

## 19. The Validation Modal Is UI State

A value such as `_professionalSummaryValidationModal` answers a
presentation question:

``` text
"Should this Bootstrap modal currently be visible?"
```

That belongs in the Web UI.

The flow is:

``` text
Validation class
    │
    ▼
validation messages
    │
    ▼
Razor state
    │
    ▼
Bootstrap modal
```

## 20. Why Pop-Up Validation Was Chosen

Candidate Profile uses validation modals rather than inline validation
messages:

``` text
Operation attempted
      │
      ▼
Validation fails
      │
      ▼
Modal opens
      │
      ▼
User reviews problems
      │
      ▼
Modal closes
```

The presentation choice remains separate from the validation rules.

## 21. GlobalUsings.cs Reduced Repeated C# Imports

JA-33 uses `GlobalUsings.cs` to centralize commonly referenced C#
namespaces, conceptually:

``` csharp
global using JobAssistant.Application.Features.CandidateProfiles;
global using JobAssistant.Domain.CandidateProfiles;
global using JobAssistant.Web.CandidateProfiles.Models;
global using JobAssistant.Web.CandidateProfiles.Validation;
```

An important distinction that becomes visible in JA-34 is:

``` text
GlobalUsings.cs
    → C# namespace imports

_Imports.razor
    → Razor component namespace imports
```

A C# global using can make `ProfessionalSummaryModel` available to C#
code, while Razor component tags such as
`<ProfessionalSummarySection />` are discovered through Razor imports.

## 22. Domain Aliasing Avoids Type-Name Confusion

The Web project can use an alias such as:

``` csharp
global using DomainCandidateProfile = JobAssistant.Domain.CandidateProfiles.CandidateProfile;
```

This distinguishes the Domain entity from the Razor page named
`CandidateProfile`.

## 23. The Save Boundary Stayed in CandidateProfile.razor

JA-33 intentionally left persistence orchestration in
`CandidateProfile.razor`.

The page remained responsible for:

``` text
loading Candidate Profile
mapping Domain → UI
mapping UI → Domain
calling create/update
```

The architecture became:

``` text
CandidateProfile.razor
│
├── UI models
│   ├── ProfessionalSummaryModel
│   ├── SkillModel
│   └── WorkExperienceModel
│
├── validation
│   ├── ProfessionalSummaryValidation
│   ├── SkillValidation
│   └── WorkExperienceValidation
│
└── persistence orchestration
        │
        ▼
ICandidateProfileService
```

## 24. Mapping UI Models into Domain Models

When Save occurs, UI state is explicitly translated into Domain objects.

For Skills:

``` csharp
_skills
    .Select(skill => new Skill
    {
        Name = skill.Name,
        YearsOfExperience = skill.YearsOfExperience
    })
    .ToList();
```

This represents the Web-to-Domain boundary.

## 25. Work Experience Requires More Translation

Work Experience demonstrates why UI and Domain models should be
separate.

For dates:

``` text
UI StartDate:
DateTime?

        ↓

Domain StartDate:
DateOnly
```

For a current position:

``` text
UI:
IsCurrentPosition = true

        ↓

Domain:
EndDate = null
```

For remote work:

``` text
UI:
IsRemote = true

        ↓

Domain:
City = null
State = null
```

## 26. Loading Performs the Reverse Mapping

When an existing Candidate Profile loads:

``` text
Domain
  │
  ▼
UI Models
```

Examples include converting `DateOnly` to the UI's date representation
and interpreting a null Domain `EndDate` as a current position.

## 27. Remote Work Is a UI Interaction Plus a Domain Rule

In the UI:

``` text
Remote checked
      │
      ├── disable City
      └── disable State
```

When saving:

``` text
IsRemote = true
      │
      ▼
City = null
State = null
```

The UI interaction and Domain representation are related but distinct.

## 28. Current Position Works the Same Way

The Current Position checkbox controls UI behavior:

``` text
Current Position checked
       │
       └── End Date disabled
```

During Domain conversion:

``` text
IsCurrentPosition = true
       │
       ▼
EndDate = null
```

## 29. Responsibilities and Achievements Are UI Collections

While editing Work Experience, entries are built incrementally:

``` text
temporary text
    │
    │ Add
    ▼
List<string>
```

The UI model represents both the current entry being typed and the
collection already added. Only the resulting collection matters to the
Domain model.

## 30. Skills Used Reuse Candidate Profile Skill State

Work Experience Skills Used are selected from Skills already added to
the Candidate Profile:

``` text
Candidate Profile Skills
       │
       ▼
checkbox list
       │
       ▼
WorkExperienceModel.Skills
```

## 31. Separation of Concerns Improved CandidateProfile.razor

The key improvement was making responsibilities explicit:

``` text
CandidateProfile.razor
    = orchestration

Models/
    = UI state

Validation/
    = UI validation rules
```

The benefit is architectural clarity, not merely reducing line count.

## 32. JA-33 Prepared the Code for JA-34

JA-33 established clean units:

``` text
ProfessionalSummaryModel
ProfessionalSummaryValidation

SkillModel
SkillValidation

WorkExperienceModel
WorkExperienceValidation
```

JA-34 can build focused Blazor components around those units.

The progression is:

``` text
JA-33
Extract state and validation
        │
        ▼
JA-34
Extract visual components
```

## 33. Architectural Result

After JA-33, the architecture conceptually became:

``` text
                    CandidateProfile.razor
                            │
           ┌────────────────┼────────────────┐
           │                │                │
           ▼                ▼                ▼
ProfessionalSummaryModel  SkillModel  WorkExperienceModel
           │                │                │
           ▼                ▼                ▼
ProfessionalSummaryValidation
                    SkillValidation
                              WorkExperienceValidation

                            │
                            ▼
                    Domain CandidateProfile
                            │
                            ▼
                 ICandidateProfileService
```

The Web layer has an explicit place for temporary interaction state and
validation. The Domain layer remains focused on Candidate Profile data.

## 34. Key Lessons from JA-33

-   **UI models and Domain models serve different purposes.**
-   UI models may contain temporary or interaction-specific state.
-   Domain models should not be forced to accommodate form-editing
    concerns.
-   Validation logic is easier to understand when extracted from Razor
    event handlers.
-   Validation classes should return validation results rather than
    control UI elements.
-   The UI decides how validation errors are presented.
-   Editable state and applied state can intentionally be different.
-   Apply and Save are separate operations.
-   Persistence should use the accepted/applied state when that is the
    intended workflow.
-   Loading persisted data requires Domain → UI mapping.
-   Saving requires the reverse UI → Domain mapping.
-   Remote and Current Position demonstrate the difference between UI
    interaction state and Domain meaning.
-   Small structural refactors can make later componentization much
    easier.

## Final Mental Model

The simplest way to remember JA-33 is:

``` text
User interacts with the page
          │
          ▼
      UI Models
          │
          ▼
      Validation
          │
          ▼
 Accepted UI state
          │
          ▼
 Domain conversion
          │
          ▼
 Application service
```

And architecturally:

``` text
JA-33 did not change what Candidate Profile does.

JA-33 changed where its responsibilities live.
```

That is the purpose of the refactor, and it is what prepared the
Candidate Profile UI for the component extraction in JA-34.
