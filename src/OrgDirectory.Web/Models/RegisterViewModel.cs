using System.ComponentModel.DataAnnotations;

namespace OrgDirectory.Web.Models;

public class RegisterViewModel
{
    [Required, Display(Name = "Логин")]
    public string Login { get; set; } = string.Empty;

    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(6), Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Подтверждение пароля"),
     Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
