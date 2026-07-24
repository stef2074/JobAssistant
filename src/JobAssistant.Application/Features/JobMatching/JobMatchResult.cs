namespace JobAssistant.Application.Features.JobMatching;

public sealed class JobMatchResult
{
    public int OverallMatchScore { get; init; }

    public IReadOnlyList<string> Strengths { get; init; } = [];

    public IReadOnlyList<string> MissingSkills { get; init; } = [];

    public IReadOnlyList<string> Recommendations { get; init; } = [];
}