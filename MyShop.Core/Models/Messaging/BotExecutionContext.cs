using System.Threading;

namespace MyShop.Core.Models.Messaging
{
    /// <summary>
    /// زمینه بات جاری در طول پردازش وب‌هوک / ارسال (AsyncLocal).
    /// </summary>
    public static class BotExecutionContext
    {
        static readonly AsyncLocal<BotCredentials> CurrentCreds = new();

        public static BotCredentials Current
        {
            get => CurrentCreds.Value;
            set => CurrentCreds.Value = value;
        }

        public static void Clear() => CurrentCreds.Value = null;
    }
}
