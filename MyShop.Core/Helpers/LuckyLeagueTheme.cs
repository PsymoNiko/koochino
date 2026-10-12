namespace MyShop.Core.Helpers
{
    public static class LuckyLeagueTheme
    {
        public static string GetDisplayName(string themeKey, string fallbackName = null)
        {
            var name = themeKey switch
            {
                "wooden" => "چوبی",
                "bronze" => "برنزی",
                "silver" => "نقره‌ای",
                "gold" => "طلایی",
                "diamond" => "الماسی",
                _ => fallbackName
            };

            return string.IsNullOrWhiteSpace(name) ? fallbackName ?? themeKey : name;
        }

        public static string GetEmoji(string themeKey) => themeKey switch
        {
            "wooden" => "🪵",
            "bronze" => "🥉",
            "silver" => "🥈",
            "gold" => "🥇",
            "diamond" => "💎",
            _ => "🎁"
        };
    }
}
