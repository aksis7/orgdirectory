// Services/TokenDtos.cs
using System.Text.Json.Serialization;

namespace OrgDirectory.Web.Services;

public record LoginRequest(
    [property: JsonPropertyName("login")] string? login,
    [property: JsonPropertyName("email")] string? email,
    [property: JsonPropertyName("password")] string password
);

public record LoginResponse(
    [property: JsonPropertyName("access_token")]  string accessToken,
    [property: JsonPropertyName("refresh_token")] string refreshToken
);

public record RefreshRequest(
    [property: JsonPropertyName("refresh_token")] string refreshToken
);

public record RefreshResponse(
    [property: JsonPropertyName("access_token")]  string accessToken,
    [property: JsonPropertyName("refresh_token")] string refreshToken
);


public record RegisterRequest(
    [property: JsonPropertyName("login")]    string login,
    [property: JsonPropertyName("email")]    string email,
    [property: JsonPropertyName("password")] string password
);
