using System.Net.Http.Json;

namespace OrgDirectory.Web.Services;

public class TokenService : ITokenService
{
    private readonly HttpClient _http;
    public TokenService(HttpClient http) { _http = http; }

    public async Task<LoginResponse?> LoginAsync(string loginOrEmail, string password)
    {
        var req = loginOrEmail.Contains('@')
            ? new LoginRequest(null, loginOrEmail, password)   // email
            : new LoginRequest(loginOrEmail, null, password);  // login

        var resp = await _http.PostAsJsonAsync("/auth/login", req);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<LoginResponse>();
    }


    public async Task<RefreshResponse?> RefreshAsync(string refreshToken)
    {
        var req  = new RefreshRequest(refreshToken);
        var resp = await _http.PostAsJsonAsync("/auth/refresh", req);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<RefreshResponse>();
    }

   
    public async Task<bool> RegisterAsync(string login, string email, string password)
    {
        var req  = new RegisterRequest(login, email, password);
        var resp = await _http.PostAsJsonAsync("/auth/register", req);
        return resp.IsSuccessStatusCode;
    }
}
