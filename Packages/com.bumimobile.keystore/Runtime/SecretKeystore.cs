using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[Serializable]
public class KeystoreData
{
    public string verification;
    public string salt;
    public List<KeystoreEntry> entries = new List<KeystoreEntry>();
}

[Serializable]
public class KeystoreEntry
{
    public string alias;
    public string encryptedValue;
}

public class SecretKeystore
{
    private const string KEYSTORE_PATH = "Assets/Game/Resources/secrets.keystore.bytes";
    private const string KEYSTORE_RESOURCE_NAME = "secrets.keystore";
    private const int ITERATIONS = 50000;

    private KeystoreData data;
    private byte[] derivedKey;
    private bool isUnlocked = false;

    public bool IsUnlocked => isUnlocked;
    public string[] Aliases => data?.entries.ConvertAll(e => e.alias).ToArray() ?? new string[0];

    #region Public Methods

    /// <summary>
    /// Create a new keystore with password
    /// </summary>
    public static SecretKeystore Create(string password)
    {
        var keystore = new SecretKeystore();
        keystore.data = new KeystoreData
        {
            salt = GenerateSalt(),
            entries = new List<KeystoreEntry>()
        };
        keystore.derivedKey = DeriveKey(password, keystore.data.salt);
        keystore.data.verification = ComputeVerification(keystore.derivedKey);
        keystore.isUnlocked = true;
        return keystore;
    }

    /// <summary>
    /// Load and unlock existing keystore
    /// </summary>
    public static SecretKeystore Load(string password)
    {
        var keystore = new SecretKeystore();

        // Try loading from Resources (works in builds)
        TextAsset keystoreAsset = Resources.Load<TextAsset>(KEYSTORE_RESOURCE_NAME);
        byte[] encryptedBytes;

        if (keystoreAsset != null)
        {
            encryptedBytes = keystoreAsset.bytes;
        }
        else if (File.Exists(KEYSTORE_PATH))
        {
            // Fallback to File.ReadAllBytes for Editor
            encryptedBytes = File.ReadAllBytes(KEYSTORE_PATH);
        }
        else
        {
            throw new FileNotFoundException("Keystore not found at: " + KEYSTORE_PATH);
        }
        byte[] decryptedBytes = DecryptBytes(encryptedBytes, password);

        if (decryptedBytes == null)
        {
            throw new CryptographicException("Invalid keystore password!");
        }

        string json = Encoding.UTF8.GetString(decryptedBytes);
        keystore.data = JsonUtility.FromJson<KeystoreData>(json);
        keystore.derivedKey = DeriveKey(password, keystore.data.salt);

        // Verify password
        if (ComputeVerification(keystore.derivedKey) != keystore.data.verification)
        {
            throw new CryptographicException("Invalid keystore password!");
        }

        keystore.isUnlocked = true;
        return keystore;
    }

    /// <summary>
    /// Check if keystore exists
    /// </summary>
    public static bool Exists()
    {
        // Check in Resources first (works in builds)
        TextAsset keystoreAsset = Resources.Load<TextAsset>(KEYSTORE_RESOURCE_NAME);
        if (keystoreAsset != null)
            return true;

        // Fallback to File.Exists for Editor
        return File.Exists(KEYSTORE_PATH);
    }

    /// <summary>
    /// Get a secret by alias
    /// </summary>
    public string GetSecret(string alias)
    {
        if (!isUnlocked)
            throw new InvalidOperationException("Keystore is locked!");

        var entry = data.entries.Find(e => e.alias == alias);
        if (entry == null)
            return null;

        return DecryptValue(entry.encryptedValue, derivedKey);
    }

    /// <summary>
    /// Add or update a secret
    /// </summary>
    public void SetSecret(string alias, string value)
    {
        if (!isUnlocked)
            throw new InvalidOperationException("Keystore is locked!");

        var entry = data.entries.Find(e => e.alias == alias);
        if (entry == null)
        {
            entry = new KeystoreEntry { alias = alias };
            data.entries.Add(entry);
        }

        entry.encryptedValue = EncryptValue(value, derivedKey);
    }

    /// <summary>
    /// Remove a secret
    /// </summary>
    public void RemoveSecret(string alias)
    {
        if (!isUnlocked)
            throw new InvalidOperationException("Keystore is locked!");

        data.entries.RemoveAll(e => e.alias == alias);
    }

    /// <summary>
    /// Save keystore to file
    /// </summary>
    public void Save(string password)
    {
        if (!isUnlocked)
            throw new InvalidOperationException("Keystore is locked!");

        string directory = Path.GetDirectoryName(KEYSTORE_PATH);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string json = JsonUtility.ToJson(data);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        byte[] encryptedBytes = EncryptBytes(jsonBytes, password);

        File.WriteAllBytes(KEYSTORE_PATH, encryptedBytes);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    /// <summary>
    /// Lock the keystore
    /// </summary>
    public void Lock()
    {
        derivedKey = null;
        isUnlocked = false;
    }

    #endregion

    #region Encryption Helpers

    private static string GenerateSalt()
    {
        byte[] salt = new byte[32];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(salt);
        }
        return Convert.ToBase64String(salt);
    }

    private static byte[] DeriveKey(string password, string salt)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), ITERATIONS))
        {
            return pbkdf2.GetBytes(32);
        }
    }

    private static string ComputeVerification(byte[] key)
    {
        using (var sha = SHA256.Create())
        {
            return Convert.ToBase64String(sha.ComputeHash(key));
        }
    }

    private static byte[] EncryptBytes(byte[] data, string password)
    {
        using (Aes aes = Aes.Create())
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, ITERATIONS))
            {
                aes.Key = pbkdf2.GetBytes(32);
                aes.GenerateIV();
            }

            using (var ms = new MemoryStream())
            {
                // Write:  salt (16) + IV (16) + encrypted data
                ms.Write(salt, 0, salt.Length);
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                }

                return ms.ToArray();
            }
        }
    }

    private static byte[] DecryptBytes(byte[] encryptedData, string password)
    {
        try
        {
            using (Aes aes = Aes.Create())
            {
                byte[] salt = new byte[16];
                byte[] iv = new byte[16];

                Array.Copy(encryptedData, 0, salt, 0, 16);
                Array.Copy(encryptedData, 16, iv, 0, 16);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, ITERATIONS))
                {
                    aes.Key = pbkdf2.GetBytes(32);
                    aes.IV = iv;
                }

                using (var ms = new MemoryStream(encryptedData, 32, encryptedData.Length - 32))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var output = new MemoryStream())
                {
                    cs.CopyTo(output);
                    return output.ToArray();
                }
            }
        }
        catch
        {
            return null;
        }
    }

    private static string EncryptValue(string value, byte[] key)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.GenerateIV();

            using (var ms = new MemoryStream())
            {
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(value);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    private static string DecryptValue(string encryptedValue, byte[] key)
    {
        byte[] data = Convert.FromBase64String(encryptedValue);

        using (Aes aes = Aes.Create())
        {
            byte[] iv = new byte[16];
            Array.Copy(data, 0, iv, 0, 16);

            aes.Key = key;
            aes.IV = iv;

            using (var ms = new MemoryStream(data, 16, data.Length - 16))
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }

    #endregion
}