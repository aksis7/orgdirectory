using System.Xml.Serialization;

namespace OrgDirectory.Web.Services.Export;

public class XmlExportStrategy<T> : IExportStrategy<T>
{
    public string Format => "xml";
    public string ContentType => "application/xml";
    public string FileExtension => "xml";

    public byte[] Export(IEnumerable<T> items)
    {
        var list = items.ToList();
        var xs = new XmlSerializer(typeof(List<T>));
        using var ms = new MemoryStream();
        xs.Serialize(ms, list);
        return ms.ToArray();
    }
}
