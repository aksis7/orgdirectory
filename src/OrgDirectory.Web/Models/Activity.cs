using System.ComponentModel.DataAnnotations;

namespace OrgDirectory.Web.Models;

public class Activity
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    [Display(Name="Наименование")]
    public string Name { get; set; } = string.Empty;

    public List<Organization> Organizations { get; set; } = new();
}
