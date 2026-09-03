namespace JobAssistant.Web.CandidateProfiles.Validation;

public sealed class ProfessionalSummaryValidation
{
    public List<string> Validate(ProfessionalSummaryModel model)
    {
        List<string> validationMessages = new List<string>();

        if (string.IsNullOrWhiteSpace(model.Headline))
        {validationMessages.Add("Headline is required.");}

        if (string.IsNullOrWhiteSpace(model.Summary))
        {validationMessages.Add("Professional summary is required.");}

        if (model.TotalYearsOfExperience < 1)
        {validationMessages.Add("Total years of experience must be at least one year.");}

        return validationMessages;
    }
}