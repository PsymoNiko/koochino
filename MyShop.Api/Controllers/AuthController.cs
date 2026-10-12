using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyShop.Api.Auth;
using MyShop.Api.Models;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("api-auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly ITokenService _token;
        private readonly IPermissionService _permissions;

        public AuthController(IAuthService auth, ITokenService token, IPermissionService permissions)
        {
            _auth = auth;
            _token = token;
            _permissions = permissions;
        }

        // ----------------------------
        // 1) Login
        // ----------------------------
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (req == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _auth.LoginWithPhoneAsync(req.PhoneNumber, req.Password, req.RoleRequested);

            if (!result.ok)
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = result.error
                });
            }

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "ورود موفق",
                Token = result.token,
                Role = req.RoleRequested,
                UserId = result.user.Id,
                Username = result.user.Username
            });
        }

        // ----------------------------
        // نقش‌های کاربر — فقط خود کاربر یا ادمین
        // ----------------------------
        [HttpGet("roles/{userId:int}")]
        [Authorize]
        public async Task<IActionResult> GetRoles(int userId)
        {
            if (!await IsSelfOrAdminAsync(userId))
                return Forbid();

            var roles = await _auth.GetRolesAsync(userId);
            return Ok(roles);
        }

        // ----------------------------
        // 2) Find user by phone — فقط وجود/فعال/نقش (بدون PII)
        // ----------------------------
        [HttpGet("find/{phone}")]
        [AllowAnonymous]
        public async Task<IActionResult> FindUser(string phone)
        {
            var user = await _auth.FindByPhoneAsync(phone);

            if (user == null)
                return NotFound(new AuthLookupResponse { Success = false, Exists = false });

            return Ok(new AuthLookupResponse
            {
                Success = true,
                Exists = true,
                User = ToLookupUser(user)
            });
        }

        // ----------------------------
        // Find by phone + role — فقط وجود/فعال/نقش (بدون PII)
        // ----------------------------
        [HttpGet("findrole")]
        [AllowAnonymous]
        public async Task<IActionResult> FindByPhoneRole(
                [FromQuery] string phone,
                [FromQuery] int roleId
            )
        {
            var user = await _auth.GetUserByPhoneAsync(phone, roleId);

            if (user == null)
                return NotFound(new AuthLookupResponse { Success = false, Exists = false });

            return Ok(new AuthLookupResponse
            {
                Success = true,
                Exists = true,
                User = ToLookupUser(user)
            });
        }

        // ----------------------------
        // پروفایل محدود — فقط خود کاربر یا ادمین (بدون هش رمز)
        // ----------------------------
        [HttpGet("user/{userId:int}")]
        [Authorize]
        public async Task<IActionResult> GetUser(int userId)
        {
            if (!await IsSelfOrAdminAsync(userId))
                return Forbid();

            var user = await _auth.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new { Success = false, Message = "کاربری یافت نشد" });

            return Ok(new { Success = true, User = ToAuthorizedUser(user) });
        }

        // ----------------------------
        // ثبت‌نام عمومی — Customer(3) یا StoreOwner(2)
        // ----------------------------
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (req == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            if (req.RoleId is not (2 or 3))
                req.RoleId = 3;

            var result = await _auth.RegisterAsync(req);

            if (!result.ok)
                return BadRequest(new { Success = false, Message = result.error });

            return Ok(new { Success = true, Message = "ثبت‌نام موفق" });
        }

        static AuthLookupUser ToLookupUser(User user) => new()
        {
            Id = user.Id,
            IsActive = user.IsActive,
            RoleId = user.RoleId,
            RoleName = user.RoleName
        };

        static object ToAuthorizedUser(User user) => new
        {
            user.Id,
            user.Username,
            user.Email,
            user.PhoneNumber,
            user.FirstName,
            user.LastName,
            user.IsActive,
            user.IsPhoneVerified,
            user.CreatedAt,
            user.UserStatID,
            user.RoleId,
            user.RoleName,
            user.UserStatTitle,
            user.SiteId
        };

        async Task<bool> IsSelfOrAdminAsync(int userId)
        {
            var claim = User.FindFirst("UserId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out var id) && id == userId)
                return true;

            if (await _permissions.IsEngineEnabledAsync())
            {
                if (int.TryParse(claim, out var adminId) && adminId > 0)
                    return await _permissions.HasPermissionAsync(adminId, "Users.View");
            }

            return User.IsInRole("Admin");
        }
    }
}
