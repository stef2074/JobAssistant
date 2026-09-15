namespace JobAssistant.Domain.CandidateProfiles;

public sealed record Job
{
    public required string                Employer         { get; init; }
    public          string?               City             { get; init; }
    public          string?               State            { get; init; }
    public required bool                  IsRemote         { get; init; }
    public required string                OfficialJobTitle { get; init; }
    public required DateOnly              StartDate        { get; init; }
    public          DateOnly?             EndDate          { get; init; }
    public required IReadOnlyList<string> Highlights       { get; init; }
    public required IReadOnlyList<Skill>  Skills           { get; init; }
}