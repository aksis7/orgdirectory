using System.ComponentModel.DataAnnotations;

namespace OrgDirectory.Web.Models;

public class Organization
{
    public int Id { get; set; }

    [Required, Display(Name="Полное наименование")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name="Краткое наименование")]
    public string? ShortName { get; set; }

    [Display(Name="Сфера деятельности")]
    public int ActivityId { get; set; }
    public Activity? Activity { get; set; }

    [Display(Name="Директор")]
    public Guid? DirectorId { get; set; }   // <-- БЫЛО int, СТАЛО Guid?
    public Citizen? Director { get; set; }

    [Display(Name="Уставной капитал")]
    public decimal CharterCapital { get; set; }

    [Display(Name="ИНН")] public string? Inn { get; set; }
    [Display(Name="КПП")] public string? Kpp { get; set; }
    [Display(Name="ОГРН")] public string? Ogrn { get; set; }
}
