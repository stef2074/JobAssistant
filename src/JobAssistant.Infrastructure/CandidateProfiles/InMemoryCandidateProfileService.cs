using JobAssistant.Application.Features.CandidateProfiles;
using JobAssistant.Domain.CandidateProfiles;

namespace JobAssistant.Infrastructure.CandidateProfiles;

public sealed class InMemoryCandidateProfileService : ICandidateProfileService
{
    private CandidateProfile? _candidateProfile;

    public Task<CandidateProfile> CreateAsync(
        CandidateProfile candidateProfile,
        CancellationToken cancellationToken = default)
    {
        _candidateProfile = candidateProfile;

        return Task.FromResult(candidateProfile);
    }

    public Task<CandidateProfile?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_candidateProfile);
    }

    public Task<CandidateProfile> UpdateAsync(
        CandidateProfile candidateProfile,
        CancellationToken cancellationToken = default)
    {
        _candidateProfile = candidateProfile;

        return Task.FromResult(candidateProfile);
    }
}