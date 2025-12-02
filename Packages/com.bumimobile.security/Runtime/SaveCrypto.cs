using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BumiMobile.Security
{
    public static class SaveCrypto
    {
        // WARNING: For development only. Replace at build time by generating SaveCryptoConfig.cs
        private const string DevFallbackSecret = "dev-fallback-secret-DO-NOT-USE-IN-PROD";
        private const string Prefix = "enc1:"; // version tag

        public static string EncryptJson(string json, string uid)
        {
            if (string.IsNullOrEmpty(json)) return json;
            if (string.IsNullOrEmpty(uid)) throw new ArgumentException("UID required for encryption.");

            DeriveKeys(uid, out var encKey, out var macKey);

            byte[] iv = new byte[16];
            RandomNumberGenerator.Fill(iv);

            byte[] plain = Encoding.UTF8.GetBytes(json);
            byte[] cipher = AesCbcEncrypt(plain, encKey, iv);

            byte[] mac = HmacSha256(macKey, Concat(iv, cipher));

            byte[] packed = Concat(Concat(iv, cipher), mac);
            string b64 = Convert.ToBase64String(packed);

            Clear(encKey); Clear(macKey); Clear(plain);
            return Prefix + b64;
        }

        public static string DecryptJsonIfNeeded(string value, string uid)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (!value.StartsWith(Prefix, StringComparison.Ordinal)) return value; // legacy plaintext
            if (string.IsNullOrEmpty(uid)) throw new ArgumentException("UID required for decryption.");

            string b64 = value.Substring(Prefix.Length);
            byte[] packed = Convert.FromBase64String(b64);
            if (packed.Length < 16 + 32) throw new CryptographicException("Cipher too short");

            byte[] iv = new byte[16];
            Buffer.BlockCopy(packed, 0, iv, 0, 16);

            int macLen = 32;
            int cipherLen = packed.Length - 16 - macLen;
            if (cipherLen <= 0) throw new CryptographicException("Invalid cipher length");

            byte[] cipher = new byte[cipherLen];
            Buffer.BlockCopy(packed, 16, cipher, 0, cipherLen);

            byte[] mac = new byte[macLen];
            Buffer.BlockCopy(packed, 16 + cipherLen, mac, 0, macLen);

            DeriveKeys(uid, out var encKey, out var macKey);

            byte[] macCalc = HmacSha256(macKey, Concat(iv, cipher));
            if (!FixedTimeEquals(mac, macCalc)) throw new CryptographicException("HMAC mismatch");

            byte[] plain = AesCbcDecrypt(cipher, encKey, iv);
            string json = Encoding.UTF8.GetString(plain);

            Clear(encKey); Clear(macKey); Clear(plain);
            return json;
        }

        // ===== helpers =====
        private static void DeriveKeys(string uid, out byte[] encKey, out byte[] macKey)
        {
            byte[] appKey = GetAppKeyBytes();
            byte[] salt = SHA256(uid);
            using var kdf = new Rfc2898DeriveBytes(appKey, salt, 10000, HashAlgorithmName.SHA256);
            byte[] key64 = kdf.GetBytes(64);
            encKey = new byte[32];
            macKey = new byte[32];
            Buffer.BlockCopy(key64, 0, encKey, 0, 32);
            Buffer.BlockCopy(key64, 32, macKey, 0, 32);
            Clear(key64); Clear(appKey);
        }

        private static byte[] GetAppKeyBytes()
        {
            try
            {
                // Try to locate SaveCryptoConfig.AppSecret via reflection to avoid hard dependency
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var t = asm.GetType("Watermelon.Security.SaveCryptoConfig");
                    if (t == null) continue;
                    var f = t.GetField("AppSecret", BindingFlags.Public | BindingFlags.Static);
                    if (f == null) continue;
                    var secret = f.GetValue(null) as string;
                    if (!string.IsNullOrEmpty(secret)) return SHA256(secret);
                }
                Debug.LogWarning("[SaveCrypto] Using DevFallbackSecret. Generate SaveCryptoConfig.cs for production builds.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveCrypto] Failed to fetch AppSecret via reflection: " + e.Message);
            }
            return SHA256(DevFallbackSecret);
        }

        private static byte[] AesCbcEncrypt(byte[] plain, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;
            using var enc = aes.CreateEncryptor();
            return enc.TransformFinalBlock(plain, 0, plain.Length);
        }

        private static byte[] AesCbcDecrypt(byte[] cipher, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;
            using var dec = aes.CreateDecryptor();
            return dec.TransformFinalBlock(cipher, 0, cipher.Length);
        }

        private static byte[] HmacSha256(byte[] key, byte[] data)
        {
            using var h = new HMACSHA256(key);
            return h.ComputeHash(data);
        }

        private static byte[] SHA256(string s)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(s ?? string.Empty));
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            byte[] r = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            return r;
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static void Clear(byte[] arr)
        {
            if (arr == null) return;
            Array.Clear(arr, 0, arr.Length);
        }
    }
}
