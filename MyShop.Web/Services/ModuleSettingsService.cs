using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.SqlClient;
using MyShop.Core.Interfaces;

namespace MyShop.Web.Services
{
    public class ModuleSettingsService : IModuleSettingsService
    {
        private const string ConfigKey = "Modules";
        private const string DbKeyPrefix = "Modules:";
        private const string FileName = "modulesettings.json";
        private const string AdminMenuOrderKey = "AdminMenu:Order";
        private static readonly string[] ModuleKeys = { "Users", "Stores", "Blog", "Shop", "Commerce", "Points", "Announcements", "Common", "Companies", "Sliders", "Products", "Personnel", "ProjectManagement", "Inventory", "CRM", "Tickets", "Support", "Reports", "Finance", "Contracts", "Assets", "Training", "Academy", "Contact" };

        private readonly IConfiguration _configuration;
        private readonly string _filePath;
        private readonly IDbConnectionFactory _connectionFactory;

        public ModuleSettingsService(IConfiguration configuration, IHostEnvironment env, IDbConnectionFactory connectionFactory)
        {
            _configuration = configuration;
            _filePath = Path.Combine(env.ContentRootPath ?? AppDomain.CurrentDomain.BaseDirectory, FileName);
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyDictionary<string, bool> GetModules()
        {
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in ModuleKeys)
                result[key] = _configuration.GetValue<bool>($"{ConfigKey}:{key}", true);

            try
            {
                using var conn = _connectionFactory.CreateConnection() as SqlConnection;
                if (conn != null)
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT [Key], [Value] FROM [Settings] WHERE [Key] LIKE @Prefix + '%'";
                    cmd.Parameters.AddWithValue("@Prefix", DbKeyPrefix);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var key = reader.GetString(0);
                        if (key.StartsWith(DbKeyPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            var moduleKey = key.Substring(DbKeyPrefix.Length).Trim();
                            if (string.IsNullOrEmpty(moduleKey) || !result.ContainsKey(moduleKey)) continue;
                            var val = reader.IsDBNull(1) ? "true" : reader.GetString(1);
                            result[moduleKey] = string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            catch { /* fallback to config only */ }

            return result;
        }

        public IReadOnlyDictionary<string, string> GetModuleDisplayNames()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Users"] = "کاربران و دسترسی",
                ["Stores"] = "فروشگاه‌ها",
                ["Blog"] = "بلاگ و محتوا (پست، نظر، سئو)",
                ["Shop"] = "فروش (سفارش، کوپن، جعبه‌شانس)",
                ["Commerce"] = "فروشگاه آنلاین (سبد، ارسال، پرداخت)",
                ["Points"] = "امتیاز و ماموریت",
                ["Announcements"] = "اعلانات",
                ["Common"] = "شبکه اجتماعی (بله و ...)",
                ["Products"] = "محصولات (کاتالوگ، دسته‌بندی، قیمت)",
                ["Personnel"] = "پرسونل (رزومه، ساعت کاری، مشخصات پرسنل)",
                ["ProjectManagement"] = "مدیریت پروژه (تعریف پروژه، تیم، وظایف، پیگیری)",
                ["Inventory"] = "انبار و موجودی (ورود/خروج، رسید، حواله)",
                ["CRM"] = "CRM (مشتریان، پیگیری، تاریخچه تماس)",
                ["Tickets"] = "تیکت پشتیبانی و وضعیت‌ها",
                ["Reports"] = "گزارش‌های سفارشی و خروجی",
                ["Finance"] = "حسابداری و صورتحساب مغازه‌داران",
                ["Contracts"] = "قراردادها و تاریخ انقضا",
                ["Assets"] = "دارایی‌ها و تجهیزات شرکت",
                ["Training"] = "آموزش (دوره‌ها، ثبت‌نام، گواهی)",
                ["Academy"] = "آموزشگاه (دانش‌آموز، مربی، فعالیت، دوره، حضورغیاب)",
                ["Companies"] = "شرکت‌ها و هلدینگ (تعریف هلدینگ و شرکت، انتساب به ماژول‌ها)",
                ["Sliders"] = "اسلایدرها (مدیریت اسلایدر و اسلاید برای بخش‌های مختلف سایت)",
                ["Contact"] = "ارتباط (درخواست تماس از لندینگ)",
                ["Support"] = "پشتیبانی زنده (چت و صندوق اپراتور)"
            };
        }

        public IReadOnlyList<string> GetAdminMenuOrder()
        {
            try
            {
                using var conn = _connectionFactory.CreateConnection() as SqlConnection;
                if (conn != null)
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT [Value] FROM [Settings] WHERE [Key] = @Key";
                    cmd.Parameters.AddWithValue("@Key", AdminMenuOrderKey);
                    var val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value)
                    {
                        var s = val.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(s))
                        {
                            var order = new List<string>();
                            foreach (var key in s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                var k = key.Trim();
                                if (!string.IsNullOrEmpty(k) && Array.Exists(ModuleKeys, x => string.Equals(x, k, StringComparison.OrdinalIgnoreCase)))
                                    order.Add(ModuleKeys[Array.FindIndex(ModuleKeys, x => string.Equals(x, k, StringComparison.OrdinalIgnoreCase))]);
                            }
                            if (order.Count > 0)
                                return order;
                        }
                    }
                }
            }
            catch { /* fallback to default */ }

            return AdminMenuConfig.DefaultModuleOrder;
        }

        public void SaveAdminMenuOrder(IReadOnlyList<string> orderedModuleKeys)
        {
            var order = new List<string>();
            foreach (var key in orderedModuleKeys)
            {
                if (Array.Exists(ModuleKeys, x => string.Equals(x, key, StringComparison.OrdinalIgnoreCase)))
                    order.Add(ModuleKeys[Array.FindIndex(ModuleKeys, x => string.Equals(x, key, StringComparison.OrdinalIgnoreCase))]);
            }
            if (order.Count == 0)
                return;

            var value = string.Join(",", order);
            try
            {
                using var conn = _connectionFactory.CreateConnection() as SqlConnection;
                if (conn != null)
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        IF EXISTS (SELECT 1 FROM [Settings] WHERE [Key] = @Key)
                            UPDATE [Settings] SET [Value] = @Value WHERE [Key] = @Key;
                        ELSE
                            INSERT INTO [Settings] ([Key], [Value]) VALUES (@Key, @Value);";
                    cmd.Parameters.AddWithValue("@Key", AdminMenuOrderKey);
                    cmd.Parameters.AddWithValue("@Value", value);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* optional: persist to file as fallback */ }
        }

        public void SaveModules(IReadOnlyDictionary<string, bool> values)
        {
            var dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in values)
                dict[kv.Key] = kv.Value;

            try
            {
                using var conn = _connectionFactory.CreateConnection() as SqlConnection;
                if (conn != null)
                {
                    conn.Open();
                    foreach (var kv in dict)
                    {
                        var key = DbKeyPrefix + kv.Key;
                        var value = kv.Value ? "true" : "false";
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                            IF EXISTS (SELECT 1 FROM [Settings] WHERE [Key] = @Key)
                                UPDATE [Settings] SET [Value] = @Value WHERE [Key] = @Key;
                            ELSE
                                INSERT INTO [Settings] ([Key], [Value]) VALUES (@Key, @Value);";
                        cmd.Parameters.AddWithValue("@Key", key);
                        cmd.Parameters.AddWithValue("@Value", value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { /* DB optional; file is secondary */ }

            // روی هاست اشتراکی اغلب نوشتن در ContentRoot ممنوع است — DB منبع اصلی است
            try
            {
                var obj = new Dictionary<string, object> { [ConfigKey] = dict };
                var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (UnauthorizedAccessException)
            {
                // ignore — persisted in Settings table
            }
            catch (IOException)
            {
                // ignore — persisted in Settings table
            }
        }
    }
}
