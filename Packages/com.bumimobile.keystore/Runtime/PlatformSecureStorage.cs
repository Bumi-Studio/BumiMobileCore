using System;
using System.Security.Cryptography;
using UnityEngine;

// Lightweight cross-platform secure storage helper.
// - Editor / Standalone: uses DPAPI (ProtectedData) and stores protected bytes in PlayerPrefs (local only).
// - Android/iOS: stubs included. For production you should implement native KeyStore/Keychain calls.

public static class PlatformSecureStorage
{
    // Returns true if running on a platform where at least a fallback is available.
    public static bool IsAvailable
    {
        get
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR || UNITY_STANDALONE
            return true;
#else
            return false;
#endif
        }
    }

    // Get or create a 32-byte AES key protected by platform secure storage.
    public static byte[] GetOrCreateProtectedKey(string id)
    {
        var key = GetProtectedKey(id);
        if (key != null && key.Length == 32)
            return key;

        var newKey = new byte[32];
        using (var rng = new RNGCryptoServiceProvider())
            rng.GetBytes(newKey);

        StoreProtectedKey(id, newKey);
        return newKey;
    }

    // Retrieve the unprotected key. Returns null if not present.
    public static byte[] GetProtectedKey(string id)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        try
        {
            string prefKey = "BumiMobile.ProtectedKey." + id;
            string base64 = PlayerPrefs.GetString(prefKey, null);
            if (string.IsNullOrEmpty(base64))
                return null;

            byte[] protectedBytes = Convert.FromBase64String(base64);
            byte[] unprotected = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return unprotected;
        }
        catch
        {
            return null;
        }
#elif UNITY_ANDROID
        // TODO: Implement using AndroidKeyStore. Recommended approach:
        // 1. Create an AES key and wrap it with an RSA key stored in AndroidKeyStore OR use AndroidKeyStore's AES support on API 23+.
        // 2. Store wrapped bytes in persistent storage and unwrap when needed via KeyStore.
        throw new NotImplementedException("Android secure storage not implemented. Implement native KeyStore integration or use an existing plugin.");
#elif UNITY_IOS
        // TODO: Implement using iOS Keychain APIs (SecItemAdd / SecItemCopyMatching / SecItemDelete).
        throw new NotImplementedException("iOS secure storage not implemented. Implement native Keychain integration or use an existing plugin.");
#else
        return null;
#endif
    }

    // Store a key protected by platform secure storage.
    public static void StoreProtectedKey(string id, byte[] key)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        byte[] protectedBytes = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
        string base64 = Convert.ToBase64String(protectedBytes);
        string prefKey = "BumiMobile.ProtectedKey." + id;
        PlayerPrefs.SetString(prefKey, base64);
        PlayerPrefs.Save();
#elif UNITY_ANDROID
        // TODO: Implement Android KeyStore store logic.
        throw new NotImplementedException("Android secure storage not implemented. Implement native KeyStore integration or use an existing plugin.");
#elif UNITY_IOS
        // TODO: Implement iOS Keychain store logic.
        throw new NotImplementedException("iOS secure storage not implemented. Implement native Keychain integration or use an existing plugin.");
#endif
    }
}