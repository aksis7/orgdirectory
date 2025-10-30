namespace OrgDirectory.Web.Services.Export;

public class ExportResolver<T> : IExportResolver<T>
{
    private readonly Dictionary<string, IExportStrategy<T>> _map;

    public ExportResolver(IEnumerable<IExportStrategy<T>> strategies)
    {
        _map = new(StringComparer.OrdinalIgnoreCase);
        // порядок регистрации в DI важен: позже зарегистрированная стратегия перекроет раннюю
        foreach (var s in strategies)
            _map[s.Format] = s;
    }

    public IExportStrategy<T> Resolve(string? format)
    {
        if (!string.IsNullOrWhiteSpace(format) && _map.TryGetValue(format!, out var s))
            return s;

        // дефолт — CSV, иначе первая попавшаяся
        if (_map.TryGetValue("csv", out var csv)) return csv;
        return _map.Values.First();
    }
}
