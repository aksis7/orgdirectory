namespace OrgDirectory.Web.Models.Export;

public class ExportCitizen
{
    public Guid   Id { get; set; }
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public int?    BirthYear { get; set; }
    public string? Gender { get; set; }
    public string? RegistrationAddress { get; set; }
    public string? Inn { get; set; }
    public string? Snils { get; set; }
}
