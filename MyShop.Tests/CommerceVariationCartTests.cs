using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Products;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Products;
using MyShop.Core.Routing;
using Xunit;

namespace MyShop.Tests
{
    public class CommerceVariationCartTests
    {
        sealed class Flag : ICommerceFeatureFlags
        {
            public bool On;
            public bool IsEnabled(string featureKey) =>
                On && string.Equals(featureKey, CommerceConstants.Features.VariationCart, StringComparison.OrdinalIgnoreCase);
        }

        sealed class Products : IProductService
        {
            public ProductDto P;
            public Task<ProductDto> GetByIdAsync(int id) => Task.FromResult(id == P?.Id ? P : null);
            public Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null) => Task.FromResult(new List<ProductDto>());
            public Task<int> CreateAsync(CreateProductDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateProductDto dto) => Task.FromResult(false);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(false);
        }

        sealed class Carts : ICommerceCartRepository
        {
            public CommerceCart Cart = new() { Id = 1, SiteId = 10, Items = new List<CommerceCartItem>() };
            public Task<CommerceCart> GetOrCreateAsync(int? siteId, int? userId, string guestKey) => Task.FromResult(Cart);
            public Task<CommerceCart> GetWithItemsAsync(int cartId) => Task.FromResult(Cart);
            public Task SetItemAsync(int cartId, int productId, string title, long unitPrice, int quantity)
            {
                var ex = Cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (ex == null) Cart.Items.Add(new CommerceCartItem { CartId = cartId, ProductId = productId, Title = title, UnitPrice = unitPrice, Quantity = quantity });
                else { ex.Quantity = quantity; ex.UnitPrice = unitPrice; }
                return Task.CompletedTask;
            }
            public Task RemoveItemAsync(int cartId, int productId) { Cart.Items.RemoveAll(i => i.ProductId == productId); return Task.CompletedTask; }
            public Task ClearAsync(int cartId) { Cart.Items.Clear(); return Task.CompletedTask; }
            public Task AttachUserAsync(int cartId, int userId) { Cart.UserId = userId; return Task.CompletedTask; }
        }

        sealed class Access : ICommerceProductAccessRepository
        {
            public int? AllowedSite = 10;
            public Task<bool> ProductAccessibleAsync(int productId, int? siteId) =>
                Task.FromResult(!siteId.HasValue || AllowedSite == siteId);
            public Task<string> GetProductSkuAsync(int productId) => Task.FromResult("P");
            public Task<bool> SkuUsedOnProductAsync(string sku, int? exceptProductId) => Task.FromResult(false);
        }

        sealed class Vars : ICommerceVariationRepository
        {
            public List<CommerceProductVariation> All = new();
            public Task<List<CommerceProductVariation>> ListAsync(int productId) => Task.FromResult(All.Where(v => v.ProductId == productId).ToList());
            public Task<CommerceProductVariation> GetAsync(int id) => Task.FromResult(All.FirstOrDefault(v => v.Id == id));
            public Task<int> InsertAsync(CommerceProductVariation m) => Task.FromResult(m.Id);
            public Task UpdateAsync(CommerceProductVariation m) => Task.CompletedTask;
            public Task DeleteAsync(int id) => Task.CompletedTask;
            public Task<bool> SkuExistsAsync(string sku, int? siteId, int? exceptVariationId) => Task.FromResult(false);
            public Task ReplaceAttributesAsync(int variationId, IReadOnlyList<CommerceVariationAttribute> attrs) => Task.CompletedTask;
            public Task<List<CommerceVariationAttribute>> ListAttributesAsync(int variationId) => Task.FromResult(new List<CommerceVariationAttribute>());
            public Task SetDefaultAsync(int productId, int variationId)
            {
                foreach (var x in All.Where(v => v.ProductId == productId))
                    x.IsDefault = x.Id == variationId;
                return Task.CompletedTask;
            }
            public Task DecrementStockAsync(int variationId, int quantity)
            {
                var v = All.FirstOrDefault(x => x.Id == variationId);
                if (v == null || v.Stock < quantity) throw new InvalidOperationException("موجودی تنوع کافی نیست.");
                v.Stock -= quantity;
                return Task.CompletedTask;
            }
        }

        sealed class Lines : ICommerceVariationCartRepository
        {
            public List<CommerceVariationCartItem> Items = new();
            public int Upserts;
            public Task<CommerceVariationCartItem> GetAsync(int cartId, int variationId) =>
                Task.FromResult(Items.FirstOrDefault(i => i.CartId == cartId && i.VariationId == variationId));
            public Task<List<CommerceVariationCartItem>> ListAsync(int cartId) =>
                Task.FromResult(Items.Where(i => i.CartId == cartId).ToList());
            public Task UpsertAsync(CommerceVariationCartItem item)
            {
                Upserts++;
                var ex = Items.FirstOrDefault(i => i.CartId == item.CartId && i.VariationId == item.VariationId);
                if (ex == null) Items.Add(item);
                else
                {
                    ex.Quantity = item.Quantity;
                    ex.UnitPriceSnapshot = item.UnitPriceSnapshot;
                    ex.CompareAtPriceSnapshot = item.CompareAtPriceSnapshot;
                }
                return Task.CompletedTask;
            }
            public Task RemoveAsync(int cartId, int variationId)
            {
                Items.RemoveAll(i => i.CartId == cartId && i.VariationId == variationId);
                return Task.CompletedTask;
            }
            public Task ClearAsync(int cartId)
            {
                Items.RemoveAll(i => i.CartId == cartId);
                return Task.CompletedTask;
            }
        }

        CommerceVariationCartService Svc(Flag f, Products p, Carts c, Access a, Vars v, Lines l) =>
            new(f, c, l, v, a, p, new CommercePricingService());

        (Flag f, Products p, Carts c, Access a, Vars v, Lines l) Seed()
        {
            var f = new Flag { On = true };
            var p = new Products { P = new ProductDto { Id = 1, Name = "Tee", IsActive = true, Price = 10000, CompareAtPrice = 12000, Quantity = 99 } };
            var v = new Vars();
            v.All.Add(new CommerceProductVariation { Id = 11, ProductId = 1, SiteId = 10, Sku = "RED-M", Price = 9000, CompareAtPrice = 11000, Stock = 5, IsActive = true });
            v.All.Add(new CommerceProductVariation { Id = 12, ProductId = 1, SiteId = 10, Sku = "BLU-L", Price = 9500, Stock = 5, IsActive = true });
            return (f, p, new Carts(), new Access(), v, new Lines());
        }

        [Fact]
        public async Task FlagOff_DoesNotWrite()
        {
            var s = Seed();
            s.f.On = false;
            var svc = Svc(s.f, s.p, s.c, s.a, s.v, s.l);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddAsync(10, 1, "g", 11, 1));
            Assert.Empty(s.l.Items);
            Assert.Empty(s.c.Cart.Items);
        }

        [Fact]
        public async Task SimpleProductCart_UnchangedByVariationService()
        {
            var s = Seed();
            await s.c.SetItemAsync(1, 1, "Tee", 777, 2);
            var svc = Svc(s.f, s.p, s.c, s.a, s.v, s.l);
            await svc.AddAsync(10, 1, "g", 11, 1);
            Assert.Single(s.c.Cart.Items);
            Assert.Equal(777, s.c.Cart.Items[0].UnitPrice);
            Assert.Equal(2, s.c.Cart.Items[0].Quantity);
        }

        [Fact]
        public async Task AddVariation_SnapshotsServerPrice_IgnoresClient()
        {
            var s = Seed();
            var line = await Svc(s.f, s.p, s.c, s.a, s.v, s.l).AddAsync(10, 1, "g", 11, 1);
            Assert.Equal(9000, line.UnitPriceSnapshot);
            Assert.Equal(11000, line.CompareAtPriceSnapshot);
        }

        [Fact]
        public async Task SameVariationTwice_IncreasesQuantity()
        {
            var s = Seed();
            var svc = Svc(s.f, s.p, s.c, s.a, s.v, s.l);
            await svc.AddAsync(10, 1, "g", 11, 1);
            var line = await svc.AddAsync(10, 1, "g", 11, 2);
            Assert.Single(s.l.Items);
            Assert.Equal(3, line.Quantity);
        }

        [Fact]
        public async Task TwoVariations_CanCoexist_IndependentPrices()
        {
            var s = Seed();
            var svc = Svc(s.f, s.p, s.c, s.a, s.v, s.l);
            await svc.AddAsync(10, 1, "g", 11, 1);
            await svc.AddAsync(10, 1, "g", 12, 1);
            Assert.Equal(2, s.l.Items.Count);
            Assert.Equal(9000, s.l.Items.First(i => i.VariationId == 11).UnitPriceSnapshot);
            Assert.Equal(9500, s.l.Items.First(i => i.VariationId == 12).UnitPriceSnapshot);
        }

        [Fact]
        public async Task InactiveVariation_Rejected()
        {
            var s = Seed();
            s.v.All[0].IsActive = false;
            await Assert.ThrowsAsync<InvalidOperationException>(() => Svc(s.f, s.p, s.c, s.a, s.v, s.l).AddAsync(10, 1, "g", 11, 1));
        }

        [Fact]
        public async Task InactiveProduct_Rejected()
        {
            var s = Seed();
            s.p.P.IsActive = false;
            await Assert.ThrowsAsync<InvalidOperationException>(() => Svc(s.f, s.p, s.c, s.a, s.v, s.l).AddAsync(10, 1, "g", 11, 1));
        }

        [Fact]
        public async Task WrongSite_Rejected()
        {
            var s = Seed();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Svc(s.f, s.p, s.c, s.a, s.v, s.l).AddAsync(99, 1, "g", 11, 1));
        }

        [Fact]
        public async Task DuplicateLine_PreventedByUpsertKey()
        {
            var s = Seed();
            var svc = Svc(s.f, s.p, s.c, s.a, s.v, s.l);
            await svc.AddAsync(10, 1, "g", 11, 1);
            await svc.AddAsync(10, 1, "g", 11, 1);
            Assert.Single(s.l.Items);
        }

        [Fact]
        public async Task ConcurrentAdds_SerializeToOneLine()
        {
            var s = Seed();
            var svc = Svc(s.f, s.p, s.c, s.a, s.v, s.l);
            await Task.WhenAll(svc.AddAsync(10, 1, "g", 11, 1), svc.AddAsync(10, 1, "g", 11, 1));
            Assert.Single(s.l.Items);
        }

        [Fact]
        public void PublicCheckoutRoutes_Unchanged()
        {
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/shop"));
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/cart"));
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/checkout"));
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/commerce/payment/callback"));
        }

        [Fact]
        public void Pricing_SaleWhenCompareHigher()
        {
            var q = new CommercePricingService().QuoteProduct(new ProductDto { Price = 80, CompareAtPrice = 100 });
            Assert.True(q.IsSale);
            Assert.Equal(80, q.UnitPrice);
        }

        [Fact]
        public async Task SetQuantity_RequotesAndCapsStock()
        {
            var s = Seed();
            var svc = Svc(s.f, s.p, s.c, s.a, s.v, s.l);
            await svc.AddAsync(10, 1, "g", 11, 1);
            await svc.SetQuantityAsync(10, 1, "g", 11, 4);
            Assert.Equal(4, s.l.Items[0].Quantity);
            Assert.Equal(9000, s.l.Items[0].UnitPriceSnapshot);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SetQuantityAsync(10, 1, "g", 11, 9));
            await svc.SetQuantityAsync(10, 1, "g", 11, 0);
            Assert.Empty(s.l.Items);
        }

        [Fact]
        public async Task SetQuantity_FlagOff_DoesNotWrite()
        {
            var s = Seed();
            s.f.On = false;
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Svc(s.f, s.p, s.c, s.a, s.v, s.l).SetQuantityAsync(10, 1, "g", 11, 2));
            Assert.Empty(s.l.Items);
        }
    }
}
