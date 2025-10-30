using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OrgDirectory.Web.Data.Repositories;
using OrgDirectory.Web.Models;
using System.Text;
using System.Xml.Serialization;
using System.Text.Json;
using OrgDirectory.Web.Services.Export; 
using OrgDirectory.Web.Models.Export;
namespace OrgDirectory.Web.Controllers;

[Authorize]
public class OrganizationsController : Controller
{
    private readonly IUnitOfWork _uow;
    public OrganizationsController(IUnitOfWork uow) { _uow = uow; }

    public async Task<IActionResult> Index(string? sortField, string? sortDir)
    {
        var orgs = await _uow.Organizations.GetWithReferencesAsync(sortField, sortDir);
        ViewBag.SortField = sortField;
        ViewBag.SortDir = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        
        return View(orgs);
    }

    private async Task PopulateLookups()
    {
        var activities = await _uow.Activities.GetAllAsync(orderBy: q => q.OrderBy(a => a.Name));
        var citizens = await _uow.Citizens.GetAllAsync(orderBy: q => q.OrderBy(c => c.LastName).ThenBy(c=>c.FirstName));
        ViewBag.ActivityId = new SelectList(activities, "Id", "Name");
        ViewBag.DirectorId = new SelectList(citizens.Select(c => new { c.Id, Name = c.FullName }), "Id", "Name");
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateLookups();
        return View(new Organization());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Organization model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookups();
            return View(model);
        }
        await _uow.Organizations.AddAsync(model);
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var m = await _uow.Organizations.GetByIdAsync(id);
        if (m == null) return NotFound();
        await PopulateLookups();
        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Organization model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookups();
            return View(model);
        }
        _uow.Organizations.Update(model);
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var m = await _uow.Organizations.GetByIdAsync(id);
        if (m != null)
        {
            _uow.Organizations.Remove(m);
            await _uow.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        [FromServices] IExportResolver<ExportOrganization> resolver,
        string fmt = "csv"
        )
    {
        var orgs = await _uow.Organizations.GetWithReferencesAsync("activity", "asc");

        var rows = orgs.Select(o => new ExportOrganization
        {
            Id = o.Id,
            FullName = o.FullName,
            ShortName = o.ShortName,
            Activity = o.Activity?.Name ?? "",
            Director = o.Director?.FullName ?? "",
            CharterCapital = o.CharterCapital,
            Inn = o.Inn,
            Kpp = o.Kpp,
            Ogrn = o.Ogrn
        });

        var strategy = resolver.Resolve(fmt);
        var bytes = strategy.Export(rows);
        return File(bytes, strategy.ContentType, $"organizations.{strategy.FileExtension}");
    }

    
}
