# Candidate Profile Domain Model

## Purpose

The Candidate Profile represents the information JobAssistant requires to understand a candidate and support the complete job search lifecycle.

The Candidate Profile is the central domain concept used by job discovery, job matching, resume optimization, cover letter generation, application automation, and application tracking.

## Domain Model

```text
CandidateProfile
├── ProfessionalSummary
│   ├── Headline
│   ├── Summary
│   └── TotalYearsOfExperience
│
├── Skills
│   ├── Name
│   └── YearsOfExperience
│
├── Jobs
│   ├── Employer
│   ├── Location
│   ├── OfficialJobTitle
│   ├── StartDate
│   ├── EndDate
│   ├── Responsibilities
│   ├── Achievements
│   └── Skills
│
└── JobPreferences
    ├── DesiredJobTitles
    ├── PreferredLocations
    ├── WorkArrangements
    ├── EmploymentTypes
    └── MinimumSalary
```

## Design Notes

- The model represents business concepts rather than persistence or UI concerns.
- The model should remain independent of AI providers and external job platforms.
- Additional concepts such as Education and Certifications may be introduced when they provide business value.
