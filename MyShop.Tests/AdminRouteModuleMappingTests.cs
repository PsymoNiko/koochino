using MyShop.Core.Routing;
using Xunit;

namespace MyShop.Tests
{
    public class AdminRouteModuleMappingTests
    {
        [Theory]
        [InlineData("/admin", null)]
        [InlineData("/admin/", null)]
        [InlineData("/admin/dashboard", null)]
        [InlineData("/admin/modules", null)]
        [InlineData("/admin/settings", null)]
        [InlineData("/admin/users", "Users")]
        [InlineData("/admin/online-users", "Users")]
        [InlineData("/admin/create-admin", "Users")]
        [InlineData("/admin/stores", "Stores")]
        [InlineData("/admin/store-documents", "Stores")]
        [InlineData("/admin/orders-pending", "Stores")]
        [InlineData("/admin/posts", "Blog")]
        [InlineData("/admin/comments", "Blog")]
        [InlineData("/admin/seo", "Blog")]
        [InlineData("/admin/announcements", "Announcements")]
        [InlineData("/admin/print-designs", "Announcements")]
        [InlineData("/admin/coupons", "Shop")]
        [InlineData("/admin/chancebox-stores", "Shop")]
        [InlineData("/admin/sales-reports", "Shop")]
        [InlineData("/admin/point-missions", "Points")]
        [InlineData("/admin/purchase-rewards", "Points")]
        [InlineData("/admin/bale", "Common")]
        [InlineData("/admin/social/channels", "Common")]
        [InlineData("/admin/social-media/accounts", "SocialMedia")]
        [InlineData("/admin/social-media/inbox", "SocialMedia")]
        [InlineData("/admin/social-media/publish", "SocialMedia")]
        [InlineData("/admin/social-media/logs", "SocialMedia")]
        [InlineData("/admin/products", "Products")]
        [InlineData("/admin/products/1", "Products")]
        [InlineData("/admin/personnel", "Personnel")]
        [InlineData("/admin/personnel/1", "Personnel")]
        [InlineData("/admin/projects", "ProjectManagement")]
        [InlineData("/admin/projects/create", "ProjectManagement")]
        [InlineData("/admin/projects/1", "ProjectManagement")]
        [InlineData("/admin/inventory", "Inventory")]
        [InlineData("/admin/crm", "CRM")]
        [InlineData("/admin/crm/create", "CRM")]
        [InlineData("/admin/crm/edit/42", "CRM")]
        [InlineData("/admin/tickets", "Tickets")]
        [InlineData("/admin/tickets/create", "Tickets")]
        [InlineData("/admin/tickets/edit/42", "Tickets")]
        [InlineData("/admin/tickets/departments", "Tickets")]
        [InlineData("/admin/contact", "Contact")]
        [InlineData("/admin/ai", "AiAssistant")]
        [InlineData("/admin/ai/live-chat", "AiAssistant")]
        [InlineData("/admin/ai/suggestions", "AiAssistant")]
        [InlineData("/admin/ai/diagnostics", "AiAssistant")]
        [InlineData("/admin/ai/usage", "AiAssistant")]
        [InlineData("/admin/ai/unanswered", "AiAssistant")]
        [InlineData("/admin/support", "Support")]
        [InlineData("/admin/support/conversations", "Support")]
        [InlineData("/admin/support/settings", "Support")]
        [InlineData("/admin/support/knowledge", "Support")]
        [InlineData("/admin/support/reports", "Support")]
        [InlineData("/admin/support/canned", "Support")]
        [InlineData("/admin/support/teams", "Support")]
        [InlineData("/admin/support/blocks", "Support")]
        [InlineData("/admin/support/knowledge/12", "Support")]
        [InlineData("/admin/reports", "Reports")]
        [InlineData("/admin/finance", "Finance")]
        [InlineData("/admin/finance/store-billing", "Finance")]
        [InlineData("/admin/finance/store-billing/invoices", "Finance")]
        [InlineData("/admin/finance/store-billing/voucher-wallet", "Finance")]
        [InlineData("/admin/contracts", "Contracts")]
        [InlineData("/admin/assets", "Assets")]
        [InlineData("/admin/training", "Training")]
        [InlineData("/admin/academy", "Academy")]
        [InlineData("/admin/academy/students", "Academy")]
        [InlineData("/admin/academy/locations", "Academy")]
        [InlineData("/admin/academy/categories", "Academy")]
        [InlineData("/admin/academy/activities", "Academy")]
        [InlineData("/admin/academy/sessions", "Academy")]
        [InlineData("/admin/academy/requests", "Academy")]
        public void GetModuleForPath_ReturnsExpectedModule(string path, string expectedModule)
        {
            var result = AdminRouteModuleMapping.GetModuleForPath(path);
            Assert.Equal(expectedModule, result);
        }

        [Fact]
        public void GetModuleForPath_WithQueryString_IgnoresQuery()
        {
            Assert.Equal("Blog", AdminRouteModuleMapping.GetModuleForPath("/admin/posts?page=1"));
        }

        [Fact]
        public void GetModuleForPath_NullOrEmpty_ReturnsNull()
        {
            Assert.Null(AdminRouteModuleMapping.GetModuleForPath(null));
            Assert.Null(AdminRouteModuleMapping.GetModuleForPath(""));
        }
    }
}
