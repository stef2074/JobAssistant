namespace JobAssistant.Web.CandidateProfiles.Validation;

public sealed class WorkExperienceValidation
{
    public List<string> Validate(WorkExperienceModel model)
    {
        List<string> validationMessages = new List<string>();

        if (string.IsNullOrWhiteSpace(model.Employer))
        {validationMessages.Add("Employer is required.");}

        if (!model.IsRemote && string.IsNullOrWhiteSpace(model.City))
        {validationMessages.Add("City is required.");}

        if (!model.IsRemote && string.IsNullOrWhiteSpace(model.State))
        {validationMessages.Add("State is required.");}

        if (string.IsNullOrWhiteSpace(model.OfficialJobTitle))
        {validationMessages.Add("Official job title is required.");}

        if (model.StartDate is null)
        {validationMessages.Add("Start date is required.");}

        if (!model.IsCurrentPosition && model.EndDate is null)
        {validationMessages.Add("End date is required unless this is the current position.");}

        if (model.StartDate is not null && model.EndDate is not null && model.EndDate < model.StartDate)
        {validationMessages.Add("End date cannot be before start date.");}

        if (model.Responsibilities.Count == 0)
        {validationMessages.Add("At least one responsibility is required.");}

        if (model.Skills.Count == 0)
        {validationMessages.Add("At least one skill used in this role is required.");}

        return validationMessages;
    }
}