using System;
using System.IO;
using System.Linq;

namespace MyShop.Core.Helpers
{
    /// <summary>Validates file content by magic bytes (not only extension).</summary>
    public static class FileMagicBytes
    {
        public static bool IsAllowedImage(ReadOnlySpan<byte> header)
        {
            if (header.Length < 3) return false;
            // JPEG
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;
            // PNG
            if (header.Length >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                return true;
            // GIF
            if (header.Length >= 6 &&
                header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46)
                return true;
            // WEBP (RIFF....WEBP)
            if (header.Length >= 12 &&
                header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
                return true;
            return false;
        }

        public static bool IsPdf(ReadOnlySpan<byte> header)
            => header.Length >= 5 &&
               header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 &&
               header[3] == 0x46 && header[4] == 0x2D; // %PDF-

        public static bool MatchesExtension(string ext, ReadOnlySpan<byte> header)
        {
            ext = (ext ?? "").ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => IsAllowedImage(header),
                ".pdf" => IsPdf(header),
                // Office/ZIP containers — basic ZIP local file header
                ".zip" or ".docx" =>
                    header.Length >= 4 && header[0] == 0x50 && header[1] == 0x4B,
                ".doc" =>
                    (header.Length >= 8
                     && header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0)
                    || (header.Length >= 4 && header[0] == 0x50 && header[1] == 0x4B),
                _ => false
            };
        }

        public static byte[] ReadHeader(Stream stream, int length = 16)
        {
            var buf = new byte[length];
            var read = stream.Read(buf, 0, length);
            if (stream.CanSeek) stream.Position = 0;
            if (read < length)
                return buf.Take(read).ToArray();
            return buf;
        }
    }
}
