namespace JobAssistant.Web.CandidateProfiles.Models;

public sealed class ProfessionalSummaryModel
{
    public string Headline               { get; set; } = string.Empty;
    public string Summary                { get; set; } = string.Empty;
    public int    TotalYearsOfExperience { get; set; } = 0;
}