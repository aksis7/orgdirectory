using System.ComponentModel.DataAnnotations;

namespace OrgDirectory.Web.Models;

public class Citizen
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }   
    public string? Email { get; set; }   
    public string? Position { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Organization { get; set; }
    [Required, Display(Name="Фамилия")] public string LastName { get; set; } = string.Empty;
    [Required, Display(Name="Имя")] public string FirstName { get; set; } = string.Empty;
    [Display(Name="Отчество")] public string? MiddleName { get; set; }

    [Display(Name="Год рождения")]
    public int? BirthYear { get; set; }

    [Display(Name="Пол")] public string? Gender { get; set; } // "M"/"F"

    [Display(Name="Адрес регистрации")] public string? RegistrationAddress { get; set; }

    [Display(Name="ИНН")] public string? Inn { get; set; }
    [Display(Name="СНИЛС")] public string? Snils { get; set; }

    public string FullName => string.Join(" ", new[]{LastName, FirstName, MiddleName}.Where(s => !string.IsNullOrWhiteSpace(s)));

    public List<Organization> OrganizationsDirected { get; set; } = new();
}
