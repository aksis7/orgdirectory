using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace OrgDirectory.Web.Services.Export;

public class CsvExportStrategy<T> : IExportStrategy<T>
{
    private readonly string[]? _columnOrder;
    private readonly char _delimiter;
    private readonly bool _alwaysQuote;
    private readonly bool _useCrlf;
    private readonly bool _headerPresent;
    private readonly Encoding _encoding;

    /// <param name="columnOrder">Необязательный порядок колонок (имена свойств модели)</param>
    /// <param name="delimiter">Разделитель столбцов (RFC 4180 — запятая)</param>
    /// <param name="alwaysQuote">Кавычить все поля (по умолчанию — только когда нужно)</param>
    /// <param name="useCrlf">Разделять строки CRLF (\r\n)</param>
    /// <param name="headerPresent">Есть ли строка заголовков</param>
    /// <param name="encoding">Кодировка вывода. По умолчанию UTF-8 c BOM (для «UTF-8 со спецификацией»)</param>
    public CsvExportStrategy(
        string[]? columnOrder = null,
        char delimiter = ',',
        bool alwaysQuote = false,
        bool useCrlf = true,
        bool headerPresent = true,
        Encoding? encoding = null)
    {
        _columnOrder   = columnOrder;
        _delimiter     = delimiter;
        _alwaysQuote   = alwaysQuote;
        _useCrlf       = useCrlf;
        _headerPresent = headerPresent;
        _encoding      = encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true); // UTF-8 с BOM
    }

    public string Format => "csv";
    public string FileExtension => "csv";
    public string ContentType => $"text/csv; charset={_encoding.WebName}; header={(_headerPresent ? "present" : "absent")}";

    public byte[] Export(IEnumerable<T> items)
    {
        var props = GetOrderedReadableProperties();
        var nl = _useCrlf ? "\r\n" : "\n";

        var sb = new StringBuilder(capacity: 4096);

        // Заголовок
        for (int i = 0; i < props.Length; i++)
        {
            if (i > 0) sb.Append(_delimiter);
            sb.Append(QuoteIfNeeded(GetHeaderName(props[i])));
        }
        sb.Append(nl);

        // Данные
        foreach (var item in items)
        {
            for (int i = 0; i < props.Length; i++)
            {
                if (i > 0) sb.Append(_delimiter);
                var raw = props[i].GetValue(item, null);
                var text = FormatValue(raw);
                sb.Append(QuoteIfNeeded(text));
            }
            sb.Append(nl);
        }

        // Важно: GetBytes НЕ добавляет BOM. Приклеиваем преамбулу вручную.
        var body = _encoding.GetBytes(sb.ToString());
        var preamble = _encoding.GetPreamble(); // для UTF-8 с BOM = EF BB BF, для UTF-16LE = FF FE и т.п.

        if (preamble is { Length: > 0 })
        {
            var bytes = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);
            return bytes;
        }

        return body;
    }

    private static string GetHeaderName(PropertyInfo pi)
    {
        var dn = pi.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
        if (!string.IsNullOrWhiteSpace(dn)) return dn!;
        var d = pi.GetCustomAttribute<DisplayAttribute>()?.Name;
        if (!string.IsNullOrWhiteSpace(d)) return d!;
        return pi.Name;
    }

    private string QuoteIfNeeded(string s)
    {
        if (s is null) return "";
        bool needsQuoting =
            _alwaysQuote ||
            s.Contains(_delimiter) ||
            s.Contains('"') ||
            s.Contains('\r') ||
            s.Contains('\n') ||
            (s.Length > 0 && (char.IsWhiteSpace(s[0]) || char.IsWhiteSpace(s[^1])));

        if (!needsQuoting) return s;

        var escaped = s.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static string FormatValue(object? value)
    {
        if (value is null) return "";

        return value switch
        {
            DateTime dt => dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture),
#if NET8_0_OR_GREATER
            DateOnly dOnly => dOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly tOnly => tOnly.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
#endif
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture) ?? "",
            _ => value.ToString() ?? ""
        };
    }

    private PropertyInfo[] GetOrderedReadableProperties()
    {
        var type = typeof(T);
        var all = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

        if (_columnOrder is null || _columnOrder.Length == 0)
            return all.Values.OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();

        var ordered = new List<PropertyInfo>(_columnOrder.Length + all.Count);
        foreach (var name in _columnOrder)
            if (all.Remove(name, out var pi)) ordered.Add(pi);

        ordered.AddRange(all.Values.OrderBy(p => p.Name, StringComparer.Ordinal));
        return ordered.ToArray();
    }
}
