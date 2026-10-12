using MyShop.Core.Models.AdminAccess;
using MyShop.Core.Routing;
using Xunit;

namespace MyShop.Tests
{
    public class AdminRouteActionMappingTests
    {
        [Theory]
        [InlineData("/admin/dashboard", AdminPermissionActions.Read)]
        [InlineData("/admin/finance", AdminPermissionActions.Read)]
        [InlineData("/admin/finance/store-billing/invoices", AdminPermissionActions.Read)]
        [InlineData("/admin/users", AdminPermissionActions.Read)]
        [InlineData("/admin/access-profiles", AdminPermissionActions.Read)]
        [InlineData("/admin/create-admin", AdminPermissionActions.Create)]
        [InlineData("/admin/crm/create", AdminPermissionActions.Create)]
        [InlineData("/admin/academy/students/create", AdminPermissionActions.Create)]
        [InlineData("/admin/crm/edit/42", AdminPermissionActions.Update)]
        [InlineData("/admin/tickets/edit/42", AdminPermissionActions.Update)]
        [InlineData("/admin/academy/sessions/edit/5", AdminPermissionActions.Update)]
        [InlineData("/admin/products/1", AdminPermissionActions.Read)]
        [InlineData("/admin/credits", AdminPermissionActions.Read)]
        public void GetActionForPath_ReturnsExpectedAction(string path, string expected)
        {
            Assert.Equal(expected, AdminRouteActionMapping.GetActionForPath(path));
        }

        [Fact]
        public void GetActionForPath_DeleteMethod_ReturnsDelete()
        {
            Assert.Equal(AdminPermissionActions.Delete,
                AdminRouteActionMapping.GetActionForPath("/admin/users", "DELETE"));
        }

        [Fact]
        public void GetActionForPath_WithQueryString_IgnoresQuery()
        {
            Assert.Equal(AdminPermissionActions.Create,
                AdminRouteActionMapping.GetActionForPath("/admin/crm/create?x=1"));
        }

        [Fact]
        public void GetActionForPath_NullOrEmpty_ReturnsRead()
        {
            Assert.Equal(AdminPermissionActions.Read, AdminRouteActionMapping.GetActionForPath(null));
            Assert.Equal(AdminPermissionActions.Read, AdminRouteActionMapping.GetActionForPath(""));
        }
    }
}
