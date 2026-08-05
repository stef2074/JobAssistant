using JobAssistant.Domain.CandidateProfiles;

namespace JobAssistant.Application.Features.CandidateProfiles;

public interface ICandidateProfileService
{
    Task<CandidateProfile> CreateAsync(
        CandidateProfile candidateProfile,
        CancellationToken cancellationToken = default);

    Task<CandidateProfile?> GetAsync(
        CancellationToken cancellationToken = default);

    Task<CandidateProfile> UpdateAsync(
        CandidateProfile candidateProfile,
        CancellationToken cancellationToken = default);
}
