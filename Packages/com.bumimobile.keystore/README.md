Bumi Mobile Keystore
=====================

A small Unity package that provides an encrypted keystore for storing secrets (API keys, tokens) during development and in builds.

Features
- Editor window: Tools/🔐 Secret Keystore (create, unlock, add/remove secrets)
- Runtime helper: SecretKeystore class for loading secrets from Resources or file in Editor
- AES encryption with PBKDF2-derived keys and verification

Quick start
1. Window: open Tools -> 🔐 Secret Keystore to create a keystore (secrets.keystore.bytes will be created under Assets/Game/Resources)
2. Generate KeystorePassword.Local.cs from the window to produce a local password file (this file should be gitignored)
3. Add secrets via the editor and use SecretKeystore.Load(password) or the generated KeystorePassword helper at runtime/editor.

Notes
- Keystore file path (editor): Assets/Game/Resources/secrets.keystore.bytes
- In player builds, the keystore is loaded from Resources (TextAsset named "secrets.keystore")
- KeystorePassword.Local.cs is intended to live outside version control and must be added to .gitignore

Version
- 0.0.1 — Initial package

License
- Add your license here
