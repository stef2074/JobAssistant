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
    public string           Highlight         { get; set; } = string.Empty;
    public List<string>     Highlights        { get; set; } = new List<string>();
    public List<SkillModel> Skills            { get; set; } = new List<SkillModel>();
}