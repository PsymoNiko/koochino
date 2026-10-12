using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyShop.Shared;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MyShop.Web.Services
{
    public class JwtTokenValidator
    {
        private readonly JwtSettings _settings;

        public JwtTokenValidator(IOptions<JwtSettings> options)
        {
            _settings = options?.Value ?? new JwtSettings();
        }

        public bool TryValidate(string token, out ClaimsPrincipal principal)
        {
            principal = null;
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_settings.Key))
                return false;

            var handler = new JwtSecurityTokenHandler();
            try
            {
                principal = handler.ValidateToken(token, BuildParameters(), out _);
                return principal?.Identity?.IsAuthenticated == true;
            }
            catch
            {
                return false;
            }
        }

        TokenValidationParameters BuildParameters()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = !string.IsNullOrWhiteSpace(_settings.Issuer),
                ValidIssuer = _settings.Issuer,
                ValidateAudience = !string.IsNullOrWhiteSpace(_settings.Audience),
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
        }
    }
}
