using OrgDirectory.Web.Models;

namespace OrgDirectory.Web.Data.Repositories;

public interface IUnitOfWork
{
    IActivityRepository Activities { get; }
    IOrganizationRepository Organizations { get; }
    ICitizenRepository Citizens { get; }
    Task<int> SaveChangesAsync();
}
