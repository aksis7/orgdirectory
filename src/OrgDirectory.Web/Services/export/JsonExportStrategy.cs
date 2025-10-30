using System.Text.Json;

namespace OrgDirectory.Web.Services.Export;

public class JsonExportStrategy<T> : IExportStrategy<T>
{
    public string Format => "json";
    public string ContentType => "application/json";
    public string FileExtension => "json";

    public byte[] Export(IEnumerable<T> items)
        => JsonSerializer.SerializeToUtf8Bytes(items);
}
