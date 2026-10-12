namespace MyShop.Api.Models
{
    /// <summary>پاسخ عمومی find/findrole — بدون PII (کد ملی، ایمیل، نام).</summary>
    public class AuthLookupResponse
    {
        public bool Success { get; set; }
        public bool Exists { get; set; }
        public AuthLookupUser User { get; set; }
    }

    public class AuthLookupUser
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }
}
