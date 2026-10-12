using System;
using System.Linq;
using System.Text;

namespace MyShop.Core.Helpers
{
    public static class PhoneNormalizer
    {
        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            var sb = new StringBuilder(input.Length);
            foreach (var c in input.Trim())
            {
                if (c >= '۰' && c <= '۹') sb.Append((char)('0' + (c - '۰')));
                else if (c >= '٠' && c <= '٩') sb.Append((char)('0' + (c - '٠')));
                else sb.Append(c);
            }

            var value = sb.ToString().Replace(" ", "").Replace("-", "");
            if (value.StartsWith("+98", StringComparison.Ordinal))
                value = "0" + value.Substring(3);
            else if (value.StartsWith("98", StringComparison.Ordinal) && value.Length >= 12)
                value = "0" + value.Substring(2);

            value = new string(value.Where(char.IsDigit).ToArray());
            if (value.StartsWith("9", StringComparison.Ordinal) && value.Length == 10)
                value = "0" + value;
            return value;
        }
    }
}
