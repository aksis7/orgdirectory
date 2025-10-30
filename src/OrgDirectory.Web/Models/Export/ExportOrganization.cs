namespace OrgDirectory.Web.Models.Export;

public class ExportOrganization
{
    public int      Id { get; set; }
    public string?  FullName { get; set; }
    public string?  ShortName { get; set; }
    public string?  Activity { get; set; }
    public string?  Director { get; set; }
    public decimal  CharterCapital { get; set; }
    public string?  Inn { get; set; }
    public string?  Kpp { get; set; }
    public string?  Ogrn { get; set; }
}
