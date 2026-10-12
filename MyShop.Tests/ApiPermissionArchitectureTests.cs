using MyShop.Application.Authorization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MyShop.Tests
{
    public class ApiPermissionArchitectureTests
    {
        static readonly string[] AdminControllersRequiringPermission =
        {
            "ContractsController",
            "CrmContactsController",
            "PersonnelController",
            "WarehousesController",
            "CompaniesController",
            "HoldingsController",
            "TicketsController",
            "ModuleLicensesController"
        };

        [Fact]
        public void Admin_api_controllers_have_require_permission_on_type_or_actions()
        {
            var apiAsm = Assembly.Load("MyShop.Api");
            foreach (var name in AdminControllersRequiringPermission)
            {
                var t = apiAsm.GetType($"MyShop.Api.Controllers.{name}");
                Assert.NotNull(t);
                var hasTypeLevel = t.GetCustomAttributes(typeof(RequirePermissionAttribute), true).Any();
                var actionHas = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute), true).Any()
                             || m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute), true).Any()
                             || m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPutAttribute), true).Any()
                             || m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpDeleteAttribute), true).Any())
                    .All(m => m.GetCustomAttributes(typeof(RequirePermissionAttribute), true).Any()
                           || m.Name is "GetType" or "ToString" or "Equals" or "GetHashCode");
                Assert.True(hasTypeLevel || actionHas, $"{name} should use [RequirePermission]");
            }
        }
    }
}
