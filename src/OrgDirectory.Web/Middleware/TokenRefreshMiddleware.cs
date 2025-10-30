
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using OrgDirectory.Web.Services;

namespace OrgDirectory.Web.Middleware;

public class TokenRefreshMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JwtSecurityTokenHandler _handler = new();

    public TokenRefreshMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context, ITokenService tokenService)
    {
        if (context.Request.Cookies.TryGetValue("AccessToken", out var access) &&
            context.Request.Cookies.TryGetValue("RefreshToken", out var refresh) &&
            !string.IsNullOrWhiteSpace(access) && !string.IsNullOrWhiteSpace(refresh))
        {
            try
            {
                var jwt = _handler.ReadJwtToken(access);
                var expUnix = long.Parse(jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Exp).Value);
                var exp = DateTimeOffset.FromUnixTimeSeconds(expUnix);
                var now = DateTimeOffset.UtcNow;
                if (exp - now < TimeSpan.FromMinutes(2))
                {
                    var rr = await tokenService.RefreshAsync(refresh);
                    if (rr is not null)
                    {
                        SetAuthCookies(context.Response, rr.accessToken, rr.refreshToken);
                    }
                }
            }
            catch { /* ignore parse errors, let bearer handle it */ }
        }
        await _next(context);
    }

    public static void SetAuthCookies(HttpResponse response, string accessToken, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            return; // или лог/throw, на ваше усмотрение

        var opts = new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, Secure = false };
        response.Cookies.Append("AccessToken", accessToken, opts);
        response.Cookies.Append("RefreshToken", refreshToken, opts);
    }


    public static void ClearAuthCookies(HttpResponse response)
    {
        response.Cookies.Delete("AccessToken");
        response.Cookies.Delete("RefreshToken");
    }
}
