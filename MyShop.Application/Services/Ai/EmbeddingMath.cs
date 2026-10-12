using MyShop.Core.Models.Ai;
using System;
using System.Security.Cryptography;
using System.Text;

namespace MyShop.Application.Services.Ai
{
    /// <summary>
    /// Vector helpers shared by the indexer and hybrid retrieval. Kept allocation-light because cosine
    /// runs against every cached chunk on each question.
    /// </summary>
    public static class EmbeddingMath
    {
        public const float KeywordWeight = 0.55f;
        public const float SemanticWeight = 0.45f;

        public static float Cosine(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length == 0 || b.Length == 0) return 0f;
            var n = Math.Min(a.Length, b.Length);
            double dot = 0, na = 0, nb = 0;
            for (var i = 0; i < n; i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }
            if (na <= 0 || nb <= 0) return 0f;
            var cos = (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb)));
            if (cos > 1f) return 1f;
            if (cos < -1f) return -1f;
            return cos;
        }

        public static byte[] ToBytes(float[] vector)
        {
            if (vector == null || vector.Length == 0) return Array.Empty<byte>();
            var bytes = new byte[vector.Length * sizeof(float)];
            Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static float[] FromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length < sizeof(float) || bytes.Length % sizeof(float) != 0)
                return Array.Empty<float>();
            var vector = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
            return vector;
        }

        /// <summary>0–100. Keyword-only articles keep ranking when cosine is weak; paraphrases get a semantic lift.</summary>
        public static int HybridScore(int keywordScore, float cosine)
        {
            var keyword = keywordScore < 0 ? 0 : keywordScore;
            var semantic = Math.Max(0f, cosine) * 100f;
            var hybrid = (int)Math.Round(KeywordWeight * keyword + SemanticWeight * semantic);
            if (hybrid < 0) return 0;
            return hybrid > 100 ? 100 : hybrid;
        }

        /// <summary>A semantic-only candidate (no keyword hit) must clear this floor or it never reaches the gate.</summary>
        public static bool PassesSemanticFloor(int keywordScore, float cosine)
            => keywordScore > 0 || cosine >= AiEmbeddingDefaults.SemanticFloor;

        public static string Sha256Hex(string text)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? ""));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
