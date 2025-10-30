using OrgDirectory.Web.Models;

namespace OrgDirectory.Web.Data.Repositories;

public class ActivityRepository : Repository<Activity>, IActivityRepository
{
    public ActivityRepository(AppDbContext context) : base(context) {}
}
