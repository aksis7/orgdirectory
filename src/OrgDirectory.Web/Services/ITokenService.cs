
namespace OrgDirectory.Web.Services;

public interface ITokenService
{
    Task<LoginResponse?> LoginAsync(string loginOrEmail, string password);
    Task<RefreshResponse?> RefreshAsync(string refreshToken);
    Task<bool> RegisterAsync(string login, string email, string password);
}
