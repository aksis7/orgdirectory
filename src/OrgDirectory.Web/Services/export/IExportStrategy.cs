namespace OrgDirectory.Web.Services.Export;

public interface IExportStrategy<T>
{
    string Format { get; }        // "csv" | "json" | "xml"
    string ContentType { get; }   // MIME
    string FileExtension { get; } // "csv" | "json" | "xml"
    byte[] Export(IEnumerable<T> items);
}
