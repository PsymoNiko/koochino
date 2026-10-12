using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace MyShop.Web.Services
{
    /// <summary>خواندن وضعیت ماژول‌ها از دیتابیس در زمان استارت اپلیکیشن.</summary>
    public static class ModuleSettingsDbHelper
    {
        private const string Prefix = "Modules:";

        /// <summary>وضعیت ماژول‌ها را از جدول Settings می‌خواند. در صورت خطا یا نبود جدول null برمی‌گرداند.</summary>
        public static IReadOnlyDictionary<string, bool> LoadFromDb(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return null;

            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT [Key], [Value] FROM [Settings] WHERE [Key] LIKE @Prefix + '%'";
                cmd.Parameters.AddWithValue("@Prefix", Prefix);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var key = reader.GetString(0);
                    if (key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var moduleKey = key.Substring(Prefix.Length).Trim();
                        if (string.IsNullOrEmpty(moduleKey)) continue;
                        var val = reader.IsDBNull(1) ? "true" : reader.GetString(1);
                        result[moduleKey] = string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch
            {
                return null;
            }

            return result.Count > 0 ? result : null;
        }
    }
}
