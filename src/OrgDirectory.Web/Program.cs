using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrgDirectory.Web.Data;
using OrgDirectory.Web.Data.Repositories;
using OrgDirectory.Web.Middleware;
using OrgDirectory.Web.Services;
using OrgDirectory.Web.Services.Export;
using OrgDirectory.Web.Models.Export;
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

var builder = WebApplication.CreateBuilder(args);

// ---------- DB ----------
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
              ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
              ?? throw new InvalidOperationException("No connection string found.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connStr));

// ---------- DataProtection (ключи в /keys томе) ----------
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
    .SetApplicationName("OrgDirectory");

// ---------- Repos ----------
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<ICitizenRepository, CitizenRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// === Export strategies & resolver ===
builder.Services.AddSingleton(typeof(IExportResolver<>), typeof(ExportResolver<>));

// Открытые generic-стратегии (JSON и XML доступны для всех T)
builder.Services.AddSingleton(typeof(IExportStrategy<>), typeof(JsonExportStrategy<>));
builder.Services.AddSingleton(typeof(IExportStrategy<>), typeof(XmlExportStrategy<>));

// CSV — типо-специфичный с порядком колонок
builder.Services.AddSingleton<IExportStrategy<ExportCitizen>>(
    new CsvExportStrategy<ExportCitizen>(new[]
    {
        "Id","LastName","FirstName","MiddleName","BirthYear",
        "Gender","RegistrationAddress","Inn","Snils"
    })
);

builder.Services.AddSingleton<IExportStrategy<ExportActivity>>(
    new CsvExportStrategy<ExportActivity>(new[] { "Id", "Name" })
);

builder.Services.AddSingleton<IExportStrategy<ExportOrganization>>(
    new CsvExportStrategy<ExportOrganization>(new[]
    {
        "Id","FullName","ShortName","Activity","Director",
        "CharterCapital","Inn","Kpp","Ogrn"
    })
);




// ---------- Auth / JWT ----------
var cfg = builder.Configuration;
var issuer   = cfg["Auth:Issuer"]   ?? Environment.GetEnvironmentVariable("AUTH_ISSUER")   ?? "dev-issuer";
var audience = cfg["Auth:Audience"] ?? Environment.GetEnvironmentVariable("AUTH_AUDIENCE") ?? "orgdirectory-web";
var secret   = cfg["Auth:Secret"]   ?? Environment.GetEnvironmentVariable("AUTH_SECRET")   ?? "dev-secret-please-change";

var signingKeys = new List<SecurityKey>();

// HS256 — секрет как обычная строка
signingKeys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)));

// HS256 — секрет как base64 (если задан так)
try
{
    var raw = Convert.FromBase64String(secret);
    if (raw.Length > 0) signingKeys.Add(new SymmetricSecurityKey(raw));
}
catch { /* secret не base64 — ок */ }

// RS256 — публичный ключ (опционально)
var rsaPem = cfg["Auth:PublicKeyPem"] ?? Environment.GetEnvironmentVariable("AUTH_PUBLIC_KEY_PEM");
if (!string.IsNullOrWhiteSpace(rsaPem))
{
    using var rsa = RSA.Create();
    rsa.ImportFromPem(rsaPem);
    signingKeys.Add(new RsaSecurityKey(rsa));
}
var rsaPath = cfg["Auth:PublicKeyPath"] ?? Environment.GetEnvironmentVariable("AUTH_PUBLIC_KEY_PATH");
if (!string.IsNullOrWhiteSpace(rsaPath) && File.Exists(rsaPath))
{
    using var rsa = RSA.Create();
    rsa.ImportFromPem(await File.ReadAllTextAsync(rsaPath));
    signingKeys.Add(new RsaSecurityKey(rsa));
}

// не ремапить клеймы
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKeys = signingKeys, // список ключей (а не один)
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true
        };

        // токен из куки + краткая диагностика
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (ctx.Request.Cookies.TryGetValue("AccessToken", out var t) && !string.IsNullOrWhiteSpace(t))
                {
                    ctx.Token = t;
                    try
                    {
                        var h = new JwtSecurityTokenHandler();
                        var jwt = h.ReadJwtToken(t);
                        Console.WriteLine($"JWT dbg: alg={jwt.Header.Alg} iss={jwt.Issuer} aud={string.Join(',', jwt.Audiences)}");
                    }
                    catch { /* ignore */ }
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("JWT fail: " + ctx.Exception.GetType().Name + " - " + ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                Console.WriteLine($"JWT challenge: {ctx.Error} {ctx.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

// HttpClient к auth-сервису
builder.Services.AddHttpClient<ITokenService, TokenService>(client =>
{
    var baseUrl = cfg["Auth:ServiceBaseUrl"] ?? "http://auth-service:7001";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddControllersWithViews();

var app = builder.Build();
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// сидинг
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await SeedData.EnsureSeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTPS-редирект включаем только если реально есть HTTPS-порт
var httpsPort = Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT") ?? builder.Configuration["ASPNETCORE_HTTPS_PORT"];
if (!string.IsNullOrEmpty(httpsPort))
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();


app.UseMiddleware<TokenRefreshMiddleware>(); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
