using MyShop.Core.Helpers;

namespace MyShop.Tests
{
    public class ChanceBoxPrizeRevealTests
    {
        [Fact]
        public void ValidCode_RevealsOnce()
        {
            Assert.True(ChanceBoxRevealPolicy.ShouldReveal(found: true, isUsed: false, code: "000100001", lastRevealedCode: ""));
        }

        [Fact]
        public void SameCodeAgain_DoesNotReveal()
        {
            Assert.False(ChanceBoxRevealPolicy.ShouldReveal(found: true, isUsed: false, code: "000100001", lastRevealedCode: "000100001"));
        }

        [Fact]
        public void DifferentCode_RevealsAgain()
        {
            Assert.True(ChanceBoxRevealPolicy.ShouldReveal(found: true, isUsed: false, code: "000100002", lastRevealedCode: "000100001"));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void NotFoundOrUsed_DoesNotReveal(bool found, bool isUsed)
        {
            Assert.False(ChanceBoxRevealPolicy.ShouldReveal(found, isUsed, "000100001", ""));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void EmptyCode_DoesNotReveal(string code)
        {
            Assert.False(ChanceBoxRevealPolicy.ShouldReveal(found: true, isUsed: false, code: code, lastRevealedCode: ""));
        }

        [Fact]
        public void PersianDigits_TreatedAsSameCode()
        {
            Assert.False(ChanceBoxRevealPolicy.ShouldReveal(found: true, isUsed: false, code: "۰۰۰۱۰۰۰۰۱", lastRevealedCode: "000100001"));
            Assert.Equal("000100001", ChanceBoxRevealPolicy.NormalizeCode(" ۰۰۰۱۰۰۰۰۱ "));
        }

        [Fact]
        public void PrizeText_FallsBackWhenSellerLeftItEmpty()
        {
            Assert.Equal(ChanceBoxRevealPolicy.DefaultPrizeText, ChanceBoxRevealPolicy.PrizeText(""));
            Assert.Equal(ChanceBoxRevealPolicy.DefaultPrizeText, ChanceBoxRevealPolicy.PrizeText("   "));
            Assert.Equal(ChanceBoxRevealPolicy.DefaultPrizeText, ChanceBoxRevealPolicy.PrizeText(null));
        }

        [Fact]
        public void PrizeText_KeepsSellerText()
        {
            Assert.Equal("یک قهوه رایگان", ChanceBoxRevealPolicy.PrizeText("  یک قهوه رایگان  "));
        }
    }
}
