using OrgDirectory.Web.Models;

namespace OrgDirectory.Web.Data.Repositories;

public class CitizenRepository : Repository<Citizen>, ICitizenRepository
{
    public CitizenRepository(AppDbContext context) : base(context) {}
}
