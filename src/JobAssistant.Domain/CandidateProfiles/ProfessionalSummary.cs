namespace JobAssistant.Domain.CandidateProfiles;

public sealed record ProfessionalSummary
{
    public required string Headline { get; init; }

    public required string Summary { get; init; }

    public required int TotalYearsOfExperience { get; init; }
}