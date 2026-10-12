using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Interfaces;
using MyShop.Core.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst("UserId");

            if (userIdClaim == null)
                return Unauthorized();

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Invalid UserId claim");

            var profile = await _userService.GetProfileAsync(userId);

            if (profile == null)
                return NotFound();

            return Ok(profile);
        }

        [HttpPost("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateUserProfileDto dto)
        {
            var userIdClaim = User.FindFirst("UserId");

            if (userIdClaim == null)
                return Unauthorized();

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Invalid UserId claim");

            dto.UserId = userId;

            var result = await _userService.UpdateProfileAsync(dto);

            return result switch
            {
                1 => Ok(),
                0 => BadRequest("خطا در به‌روزرسانی. لطفاً دوباره تلاش کنید."),
                2 => BadRequest("نام کاربری تکراری است"),
                3 => BadRequest("کد ملی تکراری است"),
                4 => BadRequest("ایمیل تکراری است"),
                -1 => StatusCode(500, "خطای داخلی سرور"),
                _ => StatusCode(500, "خطای داخلی")
            };
        }
    }

}
