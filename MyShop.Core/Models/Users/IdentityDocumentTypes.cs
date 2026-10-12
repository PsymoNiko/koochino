namespace MyShop.Core.Models.Users
{
    public static class IdentityDocumentTypes
    {
        public const string NationalCard = "NationalCard";
        public const string Selfie = "Selfie";

        public static string Label(string type) => type switch
        {
            NationalCard => "تصویر کارت ملی",
            Selfie => "سلفی با کد احراز هویت",
            _ => type
        };
    }
}
