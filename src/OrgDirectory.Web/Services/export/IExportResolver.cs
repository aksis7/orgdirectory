namespace OrgDirectory.Web.Services.Export;

public interface IExportResolver<T>
{
    IExportStrategy<T> Resolve(string? format);
}
