# Bumi Mobile Feedback

In-app feedback system for Bumi Mobile projects. Users fill in their email, select an issue category, write a message, and optionally attach a screenshot from the device gallery — all submitted through an encrypted API client to a backend endpoint.

## Features

- **Feedback form UI** — `FeedbackPanel` MonoBehaviour with email input, message field, issue category toggles, screenshot preview, and loading overlay.
- **Screenshot attachment** — Pick an image from the device gallery via NativeGallery (provided by `com.bumimobile.gallery`), auto-compressed to ≤ 2 MB by `ImageCompressor`.
- **Issue categories** — Configurable `IssueData` list supporting localised and fallback labels, rendered as grouped toggles via `IssueToggle`.
- **Encrypted keystore** — `SecretKeystore` stores API keys in an AES-256-CBC encrypted `.keystore.bytes` file (PBKDF2 key derivation, random salt + IV per operation). Runtime password supplied through a gitignored partial class.
- **API client** — `ApiController` (`IApiClient`) with async/await, configurable timeout, exponential-backoff retry, cancellation tokens, and multipart file upload. Typed responses via `ApiResponse<T>`.
- **Route management** — `ApiConfiguration` ScriptableObject defines base URL, timeout, and named routes. Context-menu generates a static `ApiRoutes` class with baked constants (no `Resources.Load` at runtime).
- **JSON serialization** — `IJsonSerializer` / `NewtonsoftJsonSerializer` pluggable layer using `com.unity.nuget.newtonsoft-json`.
- **Alert messages** — `FeedbackAlertMessage` fade-in/fade-out toast for success and error states.
- **Editor tooling** — `Tools > 🔐 Secret Keystore` window for creating keystores, generating `KeystorePassword.Local.cs`, and managing secrets.

## Requirements

- Unity **2021.3** or newer.
- `com.bumimobile.core` **0.1.1+** — initializer, module registry, and shared utilities.
- `com.bumimobile.gallery` **0.1.3+** — wraps NativeGallery for device image picking.
- `com.unity.nuget.newtonsoft-json` **3.2.1+** — JSON serialization.

## Installation

### Via Unity Package Manager

1. Open **Window ▸ Package Manager**.
2. Click **+ ▸ Add package from git URL…** and enter the package repository URL.
3. Unity resolves dependencies (`core`, `gallery`, `Newtonsoft.Json`) automatically.

### Via `manifest.json`

Add the following under `"dependencies"` in your project's `Packages/manifest.json`:

```json
"com.bumimobile.feedback": "https://github.com/Bumi-Studio/BumiMobileCore.git?path=/Packages/com.bumimobile.feedback"
```

## Setup

### 1. Configure API Routes

1. Create an **ApiConfiguration** asset via **Assets ▸ Create ▸ Bumi Mobile ▸ Api ▸ ApiConfiguration**.
2. Set **Base URL** (e.g. `https://your-project.supabase.co/functions/v1/`).
3. Add a route entry — **Name:** `PostFeedbackRoute`, **Route:** `upload-feedback`.
4. Right-click the asset → **Generate Static API Class** to produce `ApiRoutes.cs` with baked constants.

### 2. Create & Unlock the Secret Keystore

1. Open **Tools ▸ 🔐 Secret Keystore**.
2. In the **Create** tab, enter a password (≥ 6 characters) and click **Create Keystore**. This writes `Resources/secrets.keystore.bytes`.
3. Click **🔑 Generate KeystorePassword.Local.cs** — a random 16-character password is written to the gitignored partial class.

### 3. Store Your API Key

1. In the **Secrets** tab, unlock the keystore with your password.
2. Add a secret with alias `api_key` and paste your backend API key.

### 4. Add the Feedback Panel to Your Scene

1. Drag the **Feedback Panel** prefab into your canvas.
2. In the Inspector, configure the **Issue Data List** (each entry has `Localize Issue` and `Default Issue` strings, e.g. "Bug Report", "Feature Request", "General Feedback").

### 5. Wire Open / Close Buttons

```csharp
feedbackPanel.Open();   // show panel and scroll to top
feedbackPanel.Close();  // hide panel
```

Add an **On Click ()** event on a button → drag the Feedback Panel GameObject → select `FeedbackPanel.Open` / `FeedbackPanel.Close`.

## Runtime Flow

```
User taps "Submit"
        │
        ▼
  ┌─────────────────┐
  │  Validate Form  │  ← message, email, and at least one issue required
  └────────┬────────┘
           │ valid?
     ┌─────┴─────┐
     No          Yes
     │            │
     ▼            ▼
  Show Alert   Show Loading
  Message           │
                    ▼
            ┌─────────────────────┐
            │ Compress Screenshot │  ← ImageCompressor.CompressImageTo2MB()
            │ (if attached)        │    PNG → JPEG quality steps → downscale
            └────────┬────────────┘
                     │
                     ▼
            ┌────────────────────────┐
            │ Build FeedbackFormData │  { issue, message, email, screenshot }
            └────────┬───────────────┘
                     │
                     ▼
            ┌────────────────────────┐
            │ Unlock SecretKeystore  │  ← KeystorePassword.Password
            │ Get api_key secret     │
            └────────┬───────────────┘
                     │
                     ▼
            ┌─────────────────────────┐
            │ ApiController           │
            │ .PostMultipartAsync()   │  ← multipart/form-data
            │   email, desc, subject,│
            │   images (screenshot)  │
            └────────┬────────────────┘
                     │
                ┌────┴────┐
             Success    Failure
                │          │
                ▼          ▼
       Reset form &    Show error alert
       show "Thank you"
```

## Component Reference

### FeedbackPanel

**Namespace:** `BumiMobile.Feedback`

The main MonoBehaviour that orchestrates the feedback flow.

| Inspector Field | Type | Purpose |
|---|---|---|
| `screenshotPreview` | `Image` | Displays the selected screenshot |
| `clearScreenshotButton` | `Button` | Clears the attached screenshot |
| `messageInputField` | `TMP_InputField` | User's feedback message |
| `emailInputField` | `TMP_InputField` | User's email |
| `togglePrefab` | `IssueToggle` | Prefab for each issue category row |
| `toggleParent` | `RectTransform` | Container for instantiated toggles |
| `issueDataList` | `List<IssueData>` | Configurable issue categories |
| `loadingPanel` | `GameObject` | Shown during submission |
| `alertMessage` | `FeedbackAlertMessage` | Toast notification component |
| `contentScrollView` | `ScrollRect` | Scroll view reset to top on open |

**Public Methods:** `Submit()`, `OnPickImageButtonPressed()`, `ClearScreenshot()`, `Open()`, `Close()`

### FeedbackAlertMessage

**Namespace:** `BumiMobile.Feedback`

Fade-in / fade-out toast notification.

| Inspector Field | Type | Default | Purpose |
|---|---|---|---|
| `displayDuration` | `float` | 1.5s | How long the message stays fully visible |
| `fadeDuration` | `float` | 1.5s | Duration of the fade-out animation |
| `messageText` | `TextMeshProUGUI` | — | The text element |
| `canvasGroup` | `CanvasGroup` | — | Controls alpha for fading |

**Public Methods:** `ShowMessage(string message)`, `ShowMessage(string message, Action onComplete)`

### IssueToggle

**Namespace:** `BumiMobile.Feedback`

Single toggle row for selecting an issue category.

**Public Methods:** `SetToggle(string localizeIssue, string defaultIssueText, ToggleGroup group, Action<bool> OnSelected)`, `Switch(bool isActive)`

### ApiConfiguration

**Namespace:** `BumiMobile.Api`

ScriptableObject defining base URL, timeout, retry settings, and route list. Right-click → **Generate Static API Class** produces `ApiRoutes.cs`.

### ApiController

**Namespace:** `BumiMobile.Api`

Async HTTP client built on `UnityWebRequest`. Supports GET, POST, PUT, DELETE, PATCH; multipart upload; retry with exponential backoff; cancellation tokens; and `IApiClient` interface for DI/testing.

### SecretKeystore

**Namespace:** `BumiMobile.Utility.Keystore`

AES-256-CBC encrypted key-value store. PBKDF2 with 50 000 iterations for key derivation.

| Method | Description |
|---|---|
| `SecretKeystore.Create(password)` | Create a new keystore |
| `SecretKeystore.Load(password)` | Load & unlock existing keystore |
| `SecretKeystore.Exists()` | Check if keystore file exists |
| `GetSecret(alias)` | Retrieve a secret by alias |
| `SetSecret(alias, value)` | Add or update a secret |
| `RemoveSecret(alias)` | Delete a secret |
| `Save(password)` | Persist changes to disk |
| `Lock()` | Wipe the derived key from memory |

### ImageCompressor

**Namespace:** `BumiMobile.Utility`

Static utility compressing a `Texture2D` to ≤ 2 MB: PNG → JPEG quality steps (75 → 40) → downscale (80% → 50%) → JPEG quality 60.

```csharp
byte[] compressed = ImageCompressor.CompressImageTo2MB(texture);
```

## Namespaces

| Namespace | Purpose |
|---|---|
| `BumiMobile.Feedback` | UI components & data models |
| `BumiMobile.Api` | HTTP client, routes, serialization |
| `BumiMobile.Utility` | Image compression |
| `BumiMobile.Utility.Keystore` | Encrypted secret storage |
| `BumiMobile.Keystore` | Password partial class |

## Troubleshooting

| Problem | Solution |
|---|---|
| **"Password is empty!" warning** | Generate `KeystorePassword.Local.cs` via **Tools ▸ 🔐 Secret Keystore ▸ Generate** |
| **"Keystore not found" error** | Create a keystore first via the Editor window |
| **"API key not found in keystore"** | Add a secret with alias `api_key` |
| **"No internet connection available"** | The device has no network — shown as an alert |
| **Screenshot not attaching** | Ensure `com.bumimobile.gallery` (NativeGallery) is installed and gallery permissions are set in AndroidManifest / Info.plist |
| **Image too large** | `ImageCompressor` has a 2 MB hard limit; choose a smaller image |
| **`ApiRoutes` class not generated** | Right-click the ApiConfiguration asset → **Generate Static API Class** |
| **Keystore password mismatch after rebuild** | Regenerate `KeystorePassword.Local.cs` — it is gitignored |

## License

See the root `LICENSE` file for licensing details.