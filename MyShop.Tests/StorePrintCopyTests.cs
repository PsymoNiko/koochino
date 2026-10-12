using MyShop.Core.Models.Cms;

namespace MyShop.Tests
{
    public class StorePrintCopyTests
    {
        [Fact]
        public void Default_HasThreeDesignsAndFourBenefits()
        {
            var c = StorePrintCopy.GetDefault();
            Assert.Equal(4, c.Design1.Benefits.Count);
            Assert.False(string.IsNullOrWhiteSpace(c.Design1.HeaderTitle));
            Assert.Equal("اسکن برای ثبت سفارش", c.Design2.QrCaption);
            Assert.Contains("کوچینو", c.Design3.BrandName);
        }

        [Fact]
        public void Coalesce_FillsEmptyFieldsFromDefault()
        {
            var c = new StorePrintCopy
            {
                Design1 = new StorePrintDesign1Copy { HeaderTitle = "عنوان سفارشی" },
                Design2 = new StorePrintDesign2Copy(),
                Design3 = new StorePrintDesign3Copy()
            };
            c.CoalesceDefaults();
            Assert.Equal("عنوان سفارشی", c.Design1.HeaderTitle);
            Assert.False(string.IsNullOrWhiteSpace(c.Design1.HeaderSubtitle));
            Assert.False(string.IsNullOrWhiteSpace(c.Design2.QrCaption));
            Assert.False(string.IsNullOrWhiteSpace(c.Design3.Tagline));
        }
    }
}
