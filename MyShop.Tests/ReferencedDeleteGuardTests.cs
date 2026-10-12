using MyShop.Application.Common;

namespace MyShop.Tests
{
    public class ReferencedDeleteGuardTests
    {
        [Fact]
        public void Unused_DoesNotThrow()
        {
            ReferencedDeleteGuard.ThrowIfReferenced(null);
            ReferencedDeleteGuard.ThrowIfReferenced(Array.Empty<string>());
            ReferencedDeleteGuard.ThrowIfReferenced(new[] { " ", "" });
        }

        [Fact]
        public void Used_ThrowsPersianMessage()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ReferencedDeleteGuard.ThrowIfReferenced(new[] { "استفاده در سفارش (2 مورد)" }));
            Assert.Contains("امکان حذف نیست", ex.Message);
            Assert.Contains("استفاده در سفارش", ex.Message);
        }
    }
}
