using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgDirectory.Web.Data.Repositories;
using OrgDirectory.Web.Models;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
namespace OrgDirectory.Web.Controllers;
using OrgDirectory.Web.Services.Export; 
using OrgDirectory.Web.Models.Export;
[Authorize]
public class CitizensController : Controller
{
    private readonly IUnitOfWork _uow;
    public CitizensController(IUnitOfWork uow) { _uow = uow; }

    public async Task<IActionResult> Index(string? sortField, string? sortDir)
{
    bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
    Func<IQueryable<Citizen>, IOrderedQueryable<Citizen>> order = sortField?.ToLower() switch
    {
        "lastname"  => q => desc ? q.OrderByDescending(c => c.LastName)  : q.OrderBy(c => c.LastName),
        "firstname" => q => desc ? q.OrderByDescending(c => c.FirstName) : q.OrderBy(c => c.FirstName),
        "birthyear" => q => desc ? q.OrderByDescending(c => c.BirthYear) : q.OrderBy(c => c.BirthYear),
        _           => q => q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
    };

    var list = await _uow.Citizens.GetAllAsync(orderBy: order);
    ViewBag.SortField = sortField;
    ViewBag.SortDir   = desc ? "desc" : "asc";

   
    
    return View(list);

    
}

    [HttpGet]
    public IActionResult Create() => View(new Citizen());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Citizen model)
    {
        if (!ModelState.IsValid) return View(model);
        await _uow.Citizens.AddAsync(model);
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var m = await _uow.Citizens.GetByIdAsync(id);
        if (m == null) return NotFound();
        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Citizen model)
    {
        if (!ModelState.IsValid) return View(model);
        _uow.Citizens.Update(model);
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var m = await _uow.Citizens.GetByIdAsync(id);
        if (m != null)
        {
            _uow.Citizens.Remove(m);
            await _uow.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        [FromServices] IExportResolver<ExportCitizen> resolver,
        string fmt = "csv"
        )
    {
        var citizens = await _uow.Citizens.GetAllAsync(
            orderBy: q => q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
        );

        var rows = citizens.Select(c => new ExportCitizen
        {
            Id = c.Id,
            LastName = c.LastName,
            FirstName = c.FirstName,
            MiddleName = c.MiddleName,
            BirthYear = c.BirthYear,
            Gender = c.Gender,
            RegistrationAddress = c.RegistrationAddress,
            Inn = c.Inn,
            Snils = c.Snils
        });

        var strategy = resolver.Resolve(fmt);
        var bytes = strategy.Export(rows);
        return File(bytes, strategy.ContentType, $"citizens.{strategy.FileExtension}");
    }
    
}
