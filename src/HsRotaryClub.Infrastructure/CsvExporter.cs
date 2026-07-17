using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace HsRotaryClub.Infrastructure;

/// <summary>
/// 輕量 CSV 匯出 (UTF-8 BOM + 標題列 + 屬性值的 [Display(Name=\"...\")] header)。
/// 給 WPF 「轉 CSV」按鈕用,ClosedXML 之後再加。
/// </summary>
public static class CsvExporter
{
    public static string ToCsv<T>(IEnumerable<T> rows)
    {
        var sb = new StringBuilder();
        var props = typeof(T).GetProperties()
            .Where(p => p.CanRead)
            .ToList();

        // Header
        sb.AppendLine(string.Join(",", props.Select(p =>
            Escape(DisplayNameOf(p)))));

        // Rows
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", props.Select(p =>
                Escape(FormatValue(p.GetValue(row))))));
        }
        return sb.ToString();
    }

    public static void WriteCsv<T>(string path, IEnumerable<T> rows)
    {
        // CSV needs BOM so Excel detects UTF-8 correctly for 中文
        var bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        File.WriteAllText(path, ToCsv(rows), bom);
    }

    private static string DisplayNameOf(PropertyInfo p)
        => p.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? p.Name;

    private static string FormatValue(object? value) => value switch
    {
        null => "",
        DateOnly d => d.ToString("yyyy/M/d"),
        DateTime dt => dt.ToString("yyyy/M/d HH:mm:ss"),
        bool b => b ? "是" : "否",
        decimal m => m.ToString("0.##"),
        _ => value.ToString() ?? "",
    };

    private static string Escape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
