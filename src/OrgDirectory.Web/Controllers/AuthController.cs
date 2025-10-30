
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgDirectory.Web.Middleware;
using OrgDirectory.Web.Services;
using OrgDirectory.Web.Models;

namespace OrgDirectory.Web.Controllers;

public class AuthController : Controller
{
    private readonly ITokenService _tokens;
    public AuthController(ITokenService tokens) { _tokens = tokens; }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string loginOrEmail, string password, string? returnUrl = null)
    {
        var res = await _tokens.LoginAsync(loginOrEmail, password);
        if (res is null)
        {
            ViewBag.Error = "Неверные учетные данные";
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
            return View();
        }
        TokenRefreshMiddleware.SetAuthCookies(Response, res.accessToken, res.refreshToken);
        return Redirect(returnUrl ?? "/");
    }

    [Authorize]
    [HttpGet]
    public IActionResult Logout()
    {
        TokenRefreshMiddleware.ClearAuthCookies(Response);
        return Redirect("/Auth/Login");
    }


    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        return View(new RegisterViewModel());
    }
     [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel m, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
            return View(m);
        }

        var ok = await _tokens.RegisterAsync(m.Login, m.Email, m.Password);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, "Не удалось зарегистрировать пользователя");
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
            return View(m);
        }

        // Автовход после регистрации
        var res = await _tokens.LoginAsync(m.Login, m.Password);
        if (res is not null)
        {
            TokenRefreshMiddleware.SetAuthCookies(Response, res.accessToken, res.refreshToken);
            return Redirect(returnUrl ?? "/");
        }

        // Если токен не вернулся — просим войти вручную
        TempData["Info"] = "Регистрация успешна. Войдите под своими учетными данными.";
        return RedirectToAction(nameof(Login), new { returnUrl });
    }

}
