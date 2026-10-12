using Dapper;
using MyShop.Application.Common;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Data
{
    internal static class ReferenceCount
    {
        public static async Task EnsureUnusedAsync(IDbConnection conn, object param, params (string Sql, string Label)[] checks)
        {
            var reasons = new List<string>();
            foreach (var (sql, label) in checks)
            {
                try
                {
                    var n = await conn.ExecuteScalarAsync<int>(sql, param);
                    if (n > 0)
                        reasons.Add(label + " (" + n + " مورد)");
                }
                catch
                {
                    /* table/column may not exist yet */
                }
            }
            ReferencedDeleteGuard.ThrowIfReferenced(reasons);
        }
    }
}
