namespace JobAssistant.Domain.CandidateProfiles;

public sealed record WorkExperience
{
    public required string Employer { get; init; }

    public required string Location { get; init; }

    public required string OfficialJobTitle { get; init; }

    public required DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public required IReadOnlyList<string> Responsibilities { get; init; }

    public required IReadOnlyList<string> Achievements { get; init; }

    public required IReadOnlyList<Skill> Skills { get; init; }
}