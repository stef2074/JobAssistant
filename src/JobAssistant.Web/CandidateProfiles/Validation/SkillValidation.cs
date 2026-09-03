namespace JobAssistant.Web.CandidateProfiles.Validation;

public sealed class SkillValidation
{
    public List<string> Validate(SkillModel model, IReadOnlyList<SkillModel> skills)
    {
        List<string> validationMessages = new List<string>();

        if (string.IsNullOrWhiteSpace(model.Name))
        {validationMessages.Add("Skill is required.");}

        if (!string.IsNullOrWhiteSpace(model.Name) && !Regex.IsMatch(model.Name, @"^[a-zA-Z0-9 .+#/-]+$"))
        {validationMessages.Add("Skill contains invalid characters.");}

        if (!string.IsNullOrWhiteSpace(model.Name) && skills.Any(skill => string.Equals(skill.Name, model.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {validationMessages.Add("Skill already exists.");}

        if (model.YearsOfExperience < 1)
        {validationMessages.Add("Years of experience must be at least one year.");}

        return validationMessages;
    }
}