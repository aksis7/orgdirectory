using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgDirectory.Web.Data.Repositories;
using OrgDirectory.Web.Models;
using System.Text;
using System.Xml.Serialization;
using System.Text.Json;
using OrgDirectory.Web.Services.Export;
using OrgDirectory.Web.Models.Export;
namespace OrgDirectory.Web.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly IUnitOfWork _uow;
    public ActivitiesController(IUnitOfWork uow) { _uow = uow; }

    public async Task<IActionResult> Index(string? sortField, string? sortDir)
{
    bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
    Func<IQueryable<Activity>, IOrderedQueryable<Activity>> order = sortField?.ToLower() switch
    {
        "name" => q => desc ? q.OrderByDescending(a => a.Name) : q.OrderBy(a => a.Name),
        _      => q => q.OrderBy(a => a.Name)
    };

    var list = await _uow.Activities.GetAllAsync(orderBy: order);
    ViewBag.SortField = sortField;
    ViewBag.SortDir   = desc ? "desc" : "asc";

    
    return View(list);


}

    [HttpGet]
    public IActionResult Create() => View(new Activity());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Activity model)
    {
        if (!ModelState.IsValid) return View(model);
        await _uow.Activities.AddAsync(model);
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var m = await _uow.Activities.GetByIdAsync(id);
        if (m == null) return NotFound();
        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Activity model)
    {
        if (!ModelState.IsValid) return View(model);
        _uow.Activities.Update(model);
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var m = await _uow.Activities.GetByIdAsync(id);
        if (m != null)
        {
            _uow.Activities.Remove(m);
            await _uow.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        [FromServices] IExportResolver<ExportActivity> resolver,
        string fmt = "csv"
        )
    {
        var activities = await _uow.Activities.GetAllAsync(
            orderBy: q => q.OrderBy(a => a.Name)
        );

        var rows = activities.Select(a => new ExportActivity
        {
            Id = a.Id,
            Name = a.Name
        });

        var strategy = resolver.Resolve(fmt);
        var bytes = strategy.Export(rows);
        return File(bytes, strategy.ContentType, $"activities.{strategy.FileExtension}");
    }

    
}
