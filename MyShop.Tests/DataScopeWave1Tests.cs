using MyShop.Core.Models.Security;
using MyShop.Core.Security;
using Xunit;

namespace MyShop.Tests
{
    public class DataScopeWave1Tests
    {
        [Fact]
        public void StoreScopePredicate_includes_all_scope_kinds()
        {
            var sql = DataScopeSqlBuilder.StoreScopePredicate("s");
            Assert.Contains("N'All'", sql);
            Assert.Contains("N'Own'", sql);
            Assert.Contains("N'Site'", sql);
            Assert.Contains("N'Company'", sql);
            Assert.Contains("s.UserID", sql);
            Assert.Contains("s.SiteId", sql);
            Assert.Contains("s.CompanyId", sql);
        }

        [Fact]
        public void SiteEntityScopePredicate_filters_by_site_values()
        {
            var sql = DataScopeSqlBuilder.SiteEntityScopePredicate("c");
            Assert.Contains("c.SiteId", sql);
            Assert.Contains("STRING_SPLIT", sql);
        }

        [Fact]
        public void ScopeParameters_maps_values_to_csv()
        {
            var scope = new DataScopeQuery
            {
                Kind = "Site",
                Values = new System.Collections.Generic.List<int> { 1, 2, 3 },
                ActorUserId = 42
            };
            var p = DataScopeSqlBuilder.ScopeParameters(scope);
            var t = p.GetType();
            Assert.Equal("Site", t.GetProperty("ScopeKind")?.GetValue(p));
            Assert.Equal("1,2,3", t.GetProperty("ScopeValues")?.GetValue(p));
            Assert.Equal(42, t.GetProperty("ActorUserId")?.GetValue(p));
        }
    }
}
