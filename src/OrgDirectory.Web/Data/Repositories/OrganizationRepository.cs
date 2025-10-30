using Microsoft.EntityFrameworkCore;
using OrgDirectory.Web.Models;

namespace OrgDirectory.Web.Data.Repositories;

public class OrganizationRepository : Repository<Organization>, IOrganizationRepository
{
    private readonly AppDbContext _ctx;
    public OrganizationRepository(AppDbContext context) : base(context) { _ctx = context; }

    public async Task<List<Organization>> GetWithReferencesAsync(string? sortField, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var q = _context.Set<Organization>()
            .Include(o => o.Activity)
            .Include(o => o.Director)
            .AsQueryable();

        q = (sortField ?? "").ToLower() switch
        {
            "fullname"  => desc ? q.OrderByDescending(o => o.FullName)  : q.OrderBy(o => o.FullName),
            "shortname" => desc ? q.OrderByDescending(o => o.ShortName) : q.OrderBy(o => o.ShortName),
            "activity"  => desc ? q.OrderByDescending(o => o.Activity!.Name) : q.OrderBy(o => o.Activity!.Name),
            "director"  => desc ? q.OrderByDescending(o => o.Director!.LastName).ThenByDescending(o => o.Director!.FirstName)
                                : q.OrderBy(o => o.Director!.LastName).ThenBy(o => o.Director!.FirstName),
            "capital"   => desc ? q.OrderByDescending(o => o.CharterCapital) : q.OrderBy(o => o.CharterCapital),
            "inn"       => desc ? q.OrderByDescending(o => o.Inn) : q.OrderBy(o => o.Inn),
            "kpp"       => desc ? q.OrderByDescending(o => o.Kpp) : q.OrderBy(o => o.Kpp),
            "ogrn"      => desc ? q.OrderByDescending(o => o.Ogrn) : q.OrderBy(o => o.Ogrn),
            _           => q.OrderBy(o => o.FullName)
        };

        return await q.AsNoTracking().ToListAsync();
    }
}
