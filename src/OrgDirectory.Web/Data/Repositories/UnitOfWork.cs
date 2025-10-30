namespace OrgDirectory.Web.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context, IActivityRepository activities, IOrganizationRepository organizations, ICitizenRepository citizens)
    {
        _context = context;
        Activities = activities;
        Organizations = organizations;
        Citizens = citizens;
    }

    public IActivityRepository Activities { get; }
    public IOrganizationRepository Organizations { get; }
    public ICitizenRepository Citizens { get; }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
