namespace OrgDirectory.Web.Services.Export;

public class ExportStrategyFactory<T>
{
    private readonly Dictionary<string, IExportStrategy<T>> _strategies;

    public ExportStrategyFactory(IEnumerable<IExportStrategy<T>>? overrides = null)
    {
        _strategies = new(StringComparer.OrdinalIgnoreCase)
        {
            ["json"] = new JsonExportStrategy<T>(),
            ["xml"]  = new XmlExportStrategy<T>(),
            ["csv"]  = new CsvExportStrategy<T>()
        };

        if (overrides != null)
        {
            foreach (var s in overrides)
                _strategies[s.Format] = s; // переопределить дефолт
        }
    }

    public IExportStrategy<T> Get(string format)
        => _strategies.TryGetValue(format ?? "csv", out var s) ? s : _strategies["csv"];
}
