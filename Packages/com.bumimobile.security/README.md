# Bumi Mobile Security

Encryption helpers and editor tooling that keep Bumi Mobile save data tamper-resistant without burdening individual games. The module currently focuses on AES-256 encryption with HMAC verification via `SaveCrypto`.

## Features

- **Encrypted saves**: `SaveCrypto.EncryptJson`/`DecryptJsonIfNeeded` wrap JSON payloads before they hit disk. Data is AES-CBC encrypted and tagged with an HMAC-SHA256 signature to detect tampering.
- **Per-install secrets**: Uses a generated `SaveCryptoConfig` (unique per project) combined with the player UID to derive independent encryption + MAC keys via PBKDF2.
- **Version tagging**: Encrypted blobs are prefixed with `enc1:`. Legacy plaintext saves keep working—the decrypt helper simply returns the original string when the prefix is absent.
- **Editor generator**: `Tools ▸ Security ▸ Generate SaveCryptoConfig` emits a local `SaveCryptoConfig.cs` (not committed) with a random `AppSecret`. Regenerate when starting a new project or rotating secrets.
- **Secure defaults**: Uses 10k PBKDF2 iterations, zeroizes temporary key buffers, and avoids timing attacks via constant-time comparisons.

## Requirements

- Unity **2021.3** or newer.
- `com.bumimobile.core` 0.1.1+ (for shared utilities and module registration).

## Getting Started

1. **Generate the config**

   - In the Unity editor choose `Tools ▸ Security ▸ Generate SaveCryptoConfig`.
   - A file at `Assets/Project Files/Game/Scripts/Security/SaveCryptoConfig.cs` is created with a random `AppSecret`. Keep it out of source control; regenerate for each project/environment.

2. **Encrypt saves**

```csharp
string uid = playerProfile.UserId; // stable identifier per user
string json = JsonUtility.ToJson(saveData);
string encrypted = SaveCrypto.EncryptJson(json, uid);
SaveToDisk(encrypted);
```

3. **Decrypt when loading**

```csharp
string raw = LoadFromDisk();
string json = SaveCrypto.DecryptJsonIfNeeded(raw, uid);
var saveData = JsonUtility.FromJson<MySaveData>(json);
```

- If the stored value is plaintext, `DecryptJsonIfNeeded` simply returns it—useful when upgrading legacy saves.
- Corrupted or tampered data throws `CryptographicException`; catch it to trigger fallback flows or wipe invalid saves.

## Tips

- Rotate the `AppSecret` only when you plan to invalidate existing saves (you will not be able to decrypt older data).
- Always pass a non-empty, stable `uid`—a device ID or authenticated user ID works best.
- Consider storing the generated `SaveCryptoConfig.cs` outside version control (e.g., gitignore the folder) to prevent leaking secrets.

## License

Refer to the repository root `LICENSE` for licensing terms.
