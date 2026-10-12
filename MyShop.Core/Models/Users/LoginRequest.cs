using System.ComponentModel.DataAnnotations;

namespace MyShop.Core.Models
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "نقش درخواستی الزامی است")]
        public string RoleRequested { get; set; } = string.Empty;
    }
}
