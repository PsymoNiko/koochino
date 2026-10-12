using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using MyShop.Application.Interfaces.SocialMedia;

namespace MyShop.Infrastructure.SocialMedia
{
    /// <summary>
    /// Lightweight protector using a config secret (SocialMedia:CredentialKey).
    /// Prefer migrating to ASP.NET DataProtection later; avoids plaintext tokens in SocialCredentials.
    /// </summary>
    public class AesSocialCredentialProtector : ISocialCredentialProtector
    {
        private readonly byte[] _key;

        public AesSocialCredentialProtector(IConfiguration config)
        {
            var secret = config["SocialMedia:CredentialKey"]
                ?? config["Jwt:Key"]
                ?? "MyShop-SocialMedia-Dev-Key-ChangeMe-32b!";
            using var sha = SHA256.Create();
            _key = sha.ComputeHash(Encoding.UTF8.GetBytes(secret));
        }

        public string Protect(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return plain;
            var nonce = RandomNumberGenerator.GetBytes(12);
            var plainBytes = Encoding.UTF8.GetBytes(plain);
            var cipher = new byte[plainBytes.Length];
            var tag = new byte[16];
            using var aes = new AesGcm(_key, 16);
            aes.Encrypt(nonce, plainBytes, cipher, tag);
            var packed = new byte[nonce.Length + tag.Length + cipher.Length];
            Buffer.BlockCopy(nonce, 0, packed, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, packed, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipher, 0, packed, nonce.Length + tag.Length, cipher.Length);
            return Convert.ToBase64String(packed);
        }

        public string Unprotect(string protectedValue)
        {
            if (string.IsNullOrEmpty(protectedValue)) return protectedValue;
            try
            {
                var packed = Convert.FromBase64String(protectedValue);
                if (packed.Length < 12 + 16) return null;
                var nonce = new byte[12];
                var tag = new byte[16];
                var cipher = new byte[packed.Length - 28];
                Buffer.BlockCopy(packed, 0, nonce, 0, 12);
                Buffer.BlockCopy(packed, 12, tag, 0, 16);
                Buffer.BlockCopy(packed, 28, cipher, 0, cipher.Length);
                var plain = new byte[cipher.Length];
                using var aes = new AesGcm(_key, 16);
                aes.Decrypt(nonce, cipher, tag, plain);
                return Encoding.UTF8.GetString(plain);
            }
            catch
            {
                return null;
            }
        }
    }
}
