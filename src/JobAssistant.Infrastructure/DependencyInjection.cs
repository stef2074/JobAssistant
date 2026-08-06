using JobAssistant.Application.Features.CandidateProfiles;
using JobAssistant.Infrastructure.CandidateProfiles;
using Microsoft.Extensions.DependencyInjection;

namespace JobAssistant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<
            ICandidateProfileService,
            InMemoryCandidateProfileService>();

        return services;
    }
}