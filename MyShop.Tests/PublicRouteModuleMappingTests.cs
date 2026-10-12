using MyShop.Core.Routing;
using Xunit;

namespace MyShop.Tests
{
    public class PublicRouteModuleMappingTests
    {
        [Theory]
        [InlineData("/post", "Blog")]
        [InlineData("/post/1", "Blog")]
        [InlineData("/post/42", "Blog")]
        [InlineData("/stores", "Stores")]
        [InlineData("/store/1", "Stores")]
        [InlineData("/store/99", "Stores")]
        [InlineData("/storeowner/billing", "Finance")]
        [InlineData("/storeowner/voucher-wallet", "Finance")]
        [InlineData("/help", "Support")]
        [InlineData("/help/how-to-order", "Support")]
        public void GetModuleForPath_ReturnsExpectedModule(string path, string expectedModule)
        {
            var result = PublicRouteModuleMapping.GetModuleForPath(path);
            Assert.Equal(expectedModule, result);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/customer/profile")]
        [InlineData("/admin")]
        [InlineData("/store")] // no trailing / and no id
        public void GetModuleForPath_UnmappedPath_ReturnsNull(string path)
        {
            var result = PublicRouteModuleMapping.GetModuleForPath(path);
            Assert.Null(result);
        }

        [Fact]
        public void GetModuleForPath_NullOrEmpty_ReturnsNull()
        {
            Assert.Null(PublicRouteModuleMapping.GetModuleForPath(null));
            Assert.Null(PublicRouteModuleMapping.GetModuleForPath(""));
        }
    }
}
