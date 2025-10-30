using OrgDirectory.Web.Models;

namespace OrgDirectory.Web.Data.Repositories;

public interface IOrganizationRepository : IRepository<Organization>
{
    Task<List<Organization>> GetWithReferencesAsync(string? sortField, string? sortDir);
}
