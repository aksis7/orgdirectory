using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OrgDirectory.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (Request.Headers["X-Partial"] == "true")
            return PartialView(); // только тело страницы
        return View();
    }
}
