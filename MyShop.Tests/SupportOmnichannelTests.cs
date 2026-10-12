using MyShop.Application.Services.SupportChat;
using MyShop.Core.Models.SupportChat;

namespace MyShop.Tests
{
    public class SupportOmnichannelTests
    {
        [Theory]
        [InlineData("Telegram", "Telegram")]
        [InlineData("telegram", "Telegram")]
        [InlineData("Bale", "Bale")]
        [InlineData("bale", "Bale")]
        [InlineData("Website", "Website")]
        public void NormalizeProvider_MapsKnownChannels(string input, string expected)
        {
            Assert.Equal(expected, SupportOmnichannelService.NormalizeProvider(input));
        }

        [Fact]
        public void TryParseExternal_ReadsBaleGuestToken()
        {
            var chat = new SupportChatThread
            {
                Channel = SupportChannels.Website,
                GuestToken = "Bale:123456"
            };
            Assert.True(SupportOmnichannelService.TryParseExternal(chat, out var provider, out var chatId));
            Assert.Equal(SupportChannels.Bale, provider);
            Assert.Equal("123456", chatId);
        }

        [Fact]
        public void TryParseExternal_ReadsTelegramGuestToken()
        {
            var chat = new SupportChatThread { GuestToken = "Telegram:998877" };
            Assert.True(SupportOmnichannelService.TryParseExternal(chat, out var provider, out var chatId));
            Assert.Equal(SupportChannels.Telegram, provider);
            Assert.Equal("998877", chatId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("guest-only")]
        [InlineData("Website:abc")]
        public void TryParseExternal_RejectsWebsiteOrInvalid(string token)
        {
            var chat = new SupportChatThread { GuestToken = token };
            Assert.False(SupportOmnichannelService.TryParseExternal(chat, out _, out _));
        }

        [Fact]
        public void KnowledgeStatuses_HaveExpectedValues()
        {
            Assert.Equal("Published", KnowledgeArticleStatuses.Published);
            Assert.Equal("Faq", KnowledgeArticleTypes.Faq);
            Assert.Equal("Ai", SupportChatSenders.Ai);
        }
    }
}
