namespace JobAssistant.Domain.CandidateProfiles;

public sealed record CandidateProfile
{
    public required ProfessionalSummary  ProfessionalSummary { get; init; }
    public required IReadOnlyList<Skill> Skills              { get; init; }
    public required IReadOnlyList<Job>   Jobs                { get; init; }
    public required JobPreferences       JobPreferences      { get; init; }
}