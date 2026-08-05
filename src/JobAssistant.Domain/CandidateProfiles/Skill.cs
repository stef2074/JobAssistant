namespace JobAssistant.Domain.CandidateProfiles;

public sealed record Skill
{
    public required string Name { get; init; }

    public required int YearsOfExperience { get; init; }
}