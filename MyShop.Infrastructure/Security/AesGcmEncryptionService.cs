using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using MyShop.Application.Interfaces.Security;

namespace MyShop.Infrastructure.Security
{
    public class AesGcmEncryptionService : IEncryptionService
    {
        readonly Dictionary<int, byte[]> _keys = new();
        readonly int _current;

        public AesGcmEncryptionService(IConfiguration config)
        {
            var section = config.GetSection("Security:MasterKeys");
            foreach (var child in section.GetChildren())
            {
                var name = child.Key?.TrimStart('v', 'V');
                if (!int.TryParse(name, out var ver) || string.IsNullOrWhiteSpace(child.Value))
                    continue;
                _keys[ver] = SHA256.HashData(Encoding.UTF8.GetBytes(child.Value.Trim()));
            }

            var currentRaw = config["Security:CurrentKeyVersion"];
            if (!int.TryParse(currentRaw, out _current) || _current <= 0)
                _current = _keys.ContainsKey(1) ? 1 : (_keys.Count > 0 ? _keys.Keys.Min() : 0);
        }

        public int CurrentKeyVersion => _current;
        public bool HasMasterKey => _current > 0 && _keys.ContainsKey(_current);

        public byte[] Encrypt(string plainText, out int keyVersion)
        {
            if (!HasMasterKey)
                throw new InvalidOperationException("کلید رمزنگاری تنظیم نشده است (Security:MasterKeys).");
            if (plainText == null) plainText = "";

            keyVersion = _current;
            var key = _keys[keyVersion];
            var nonce = RandomNumberGenerator.GetBytes(12);
            var plain = Encoding.UTF8.GetBytes(plainText);
            var cipher = new byte[plain.Length];
            var tag = new byte[16];
            using var gcm = new AesGcm(key, 16);
            gcm.Encrypt(nonce, plain, cipher, tag);

            var blob = new byte[12 + 16 + cipher.Length];
            Buffer.BlockCopy(nonce, 0, blob, 0, 12);
            Buffer.BlockCopy(tag, 0, blob, 12, 16);
            Buffer.BlockCopy(cipher, 0, blob, 28, cipher.Length);
            return blob;
        }

        public string Decrypt(byte[] blob, int keyVersion)
        {
            if (blob == null || blob.Length < 28)
                throw new InvalidOperationException("داده رمزشده نامعتبر است.");
            if (!_keys.TryGetValue(keyVersion, out var key))
                throw new InvalidOperationException("نسخه کلید رمزنگاری یافت نشد: v" + keyVersion);

            var nonce = blob.AsSpan(0, 12);
            var tag = blob.AsSpan(12, 16);
            var cipher = blob.AsSpan(28);
            var plain = new byte[cipher.Length];
            using var gcm = new AesGcm(key, 16);
            gcm.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
    }
}
