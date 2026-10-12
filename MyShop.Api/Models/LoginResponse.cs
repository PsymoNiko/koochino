namespace MyShop.Api.Models
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public string Role { get; set; }

        // اضافه شده:
        public int UserId { get; set; }
        public string Username { get; set; }
    }
}
