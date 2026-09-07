namespace JobAssistant.Domain.CandidateProfiles;

public sealed record JobPreferences
{
    public required IReadOnlyList<string>          DesiredJobTitles   { get; init; }
    public required IReadOnlyList<string>          PreferredLocations { get; init; }
    public required IReadOnlyList<WorkArrangement> WorkArrangements   { get; init; }
    public required IReadOnlyList<EmploymentType>  EmploymentTypes    { get; init; }
    public          decimal?                       MinimumSalary      { get; init; }
}