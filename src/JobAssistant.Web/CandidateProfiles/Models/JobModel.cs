namespace JobAssistant.Web.CandidateProfiles.Models;

public sealed class JobModel
{
    public string           Employer          { get; set; } = string.Empty;
    public string           City              { get; set; } = string.Empty;
    public string           State             { get; set; } = string.Empty;
    public bool             IsRemote          { get; set; } = false;
    public string           OfficialJobTitle  { get; set; } = string.Empty;
    public DateTime?        StartDate         { get; set; } = null;
    public DateTime?        EndDate           { get; set; } = null;
    public bool             IsCurrentPosition { get; set; } = false;
    public string           Responsibility    { get; set; } = string.Empty;
    public string           Achievement       { get; set; } = string.Empty;
    public List<string>     Responsibilities  { get; set; } = new List<string>();
    public List<string>     Achievements      { get; set; } = new List<string>();
    public List<SkillModel> Skills            { get; set; } = new List<SkillModel>();
}