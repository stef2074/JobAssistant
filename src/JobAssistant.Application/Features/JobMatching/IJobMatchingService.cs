namespace JobAssistant.Application.Features.JobMatching;

public interface IJobMatchingService
{
    Task<JobMatchResult> MatchAsync(CancellationToken cancellationToken = default);
}