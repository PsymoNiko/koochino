using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyShop.Core.Models;
using MyShop.Shared;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MyShop.Api.Auth
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _settings;

        public TokenService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public string GenerateToken(int userId, string username, string role, int expiresMinutes)
        {
            // -------------------------
            // Claims اصلی توکن
            // -------------------------
            var claims = new List<Claim>
            {
                new Claim("UserId", userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username ?? string.Empty),
                new Claim(ClaimTypes.Role, role)
            };

            // -------------------------
            // کلید رمزنگاری
            // -------------------------
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // -------------------------
            // زمان انقضای توکن
            // اگر expiresMinutes ورودی صفر باشد، از مقدار appsettings استفاده کن
            // -------------------------
            int expMinutes = expiresMinutes > 0 ? expiresMinutes : _settings.ExpiresMinutes;

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
