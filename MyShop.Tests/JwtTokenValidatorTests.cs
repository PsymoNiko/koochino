using Microsoft.Extensions.Options;
using MyShop.Shared;
using MyShop.Web.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace MyShop.Tests
{
    public class JwtTokenValidatorTests
    {
        [Fact]
        public void Rejects_tampered_token_without_valid_signature()
        {
            var settings = new JwtSettings { Key = "unit-test-secret-key-at-least-32-chars!!", Issuer = "MyShop", Audience = "MyShop" };
            var validator = new JwtTokenValidator(Options.Create(settings));

            var handler = new JwtSecurityTokenHandler();
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var token = handler.WriteToken(new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
                expires: System.DateTime.UtcNow.AddMinutes(5),
                signingCredentials: creds));

            var parts = token.Split('.');
            var tampered = parts[0] + "." + parts[1] + ".invalidsignature";
            Assert.False(validator.TryValidate(tampered, out _));
        }

        [Fact]
        public void Accepts_valid_signed_token()
        {
            var settings = new JwtSettings { Key = "unit-test-secret-key-at-least-32-chars!!", Issuer = "MyShop", Audience = "MyShop" };
            var validator = new JwtTokenValidator(Options.Create(settings));

            var handler = new JwtSecurityTokenHandler();
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var token = handler.WriteToken(new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
                expires: System.DateTime.UtcNow.AddMinutes(5),
                signingCredentials: creds));

            Assert.True(validator.TryValidate(token, out var principal));
            Assert.Equal("1", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        }
    }
}
