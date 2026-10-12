using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MyShop.Web.Services
{
    /// <summary>ساخت CSV ساده UTF-8 با BOM برای Excel.</summary>
    public static class CsvExportHelper
    {
        public static string ToCsv(IEnumerable<string> headers, IEnumerable<IEnumerable<object>> rows)
        {
            var sb = new StringBuilder();
            sb.Append('\uFEFF');
            sb.AppendLine(string.Join(",", headers.Select(Escape)));
            foreach (var row in rows)
                sb.AppendLine(string.Join(",", row.Select(v => Escape(Format(v)))));
            return sb.ToString();
        }

        static string Format(object v)
        {
            if (v == null) return "";
            if (v is decimal d) return d.ToString("0.##", CultureInfo.InvariantCulture);
            if (v is double db) return db.ToString("0.##", CultureInfo.InvariantCulture);
            if (v is float f) return f.ToString("0.##", CultureInfo.InvariantCulture);
            if (v is System.DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            return v.ToString();
        }

        static string Escape(string s)
        {
            s ??= "";
            if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
