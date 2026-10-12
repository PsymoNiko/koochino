using System;
using System.Data;
using Microsoft.Data.SqlClient;
using MyShop.Core.Interfaces;

namespace MyShop.Infrastructure.Data
{
    /// <summary>
    /// سازنده اتصال دیتابیس بر اساس tenant جاری (هر دامنه → یک دیتابیس).
    /// باید به‌صورت Scoped ثبت شود تا در هر درخواست از connection string همان دامنه استفاده شود.
    /// </summary>
    public class TenantAwareDbConnectionFactory : IDbConnectionFactory
    {
        private readonly ICurrentTenant _currentTenant;

        public TenantAwareDbConnectionFactory(ICurrentTenant currentTenant)
        {
            _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
        }

        public IDbConnection CreateConnection()
        {
            if (!_currentTenant.IsResolved || string.IsNullOrWhiteSpace(_currentTenant.ConnectionString))
                throw new InvalidOperationException(
                    "Tenant is not resolved or has no connection string. Ensure TenantResolutionMiddleware runs and appsettings has ConnectionStrings:Tenants for this domain.");

            return new SqlConnection(_currentTenant.ConnectionString);
        }
    }
}
