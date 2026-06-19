# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- (Add unreleased changes here)

## [0.1.1] - 2026-06-19

### Fixed

- Sample UnityPackage was missing from the package.

## [0.1.0] - 2026-05-15

### Added

- `FeedbackPanel` MonoBehaviour for in-app feedback submission (email, message, issue category, screenshot).
- `FeedbackAlertMessage` fade-in/fade-out toast notification component.
- `IssueToggle` toggle row UI component with localised and default issue labels.
- `FeedbackFormData`, `FeedbackResponseData`, and `IssueData` serialisable data models.
- `ApiController` async HTTP client (`IApiClient`) with retry, timeout, cancellation, multipart upload, and `ApiResponse<T>` wrapper.
- `ApiConfiguration` ScriptableObject for base URL, timeout, retries, and named routes with context-menu code generation to a static `ApiRoutes` class.
- `IApiClient` interface for dependency injection / testing.
- `IJsonSerializer` / `NewtonsoftJsonSerializer` pluggable JSON layer.
- `SecretKeystore` AES-256-CBC encrypted key-value store (PBKDF2 key derivation) for API keys and tokens, loaded from `Resources/secrets.keystore.bytes`.
- `KeystorePassword` partial class pattern — runtime password supplied via gitignored `KeystorePassword.Local.cs`.
- `ImageCompressor` static pipeline that compresses `Texture2D` to ≤ 2 MB (PNG → JPEG quality steps → downscale) for upload.
- Editor window (`Tools > 🔐 Secret Keystore`) for creating, unlocking, and managing secrets.
- Demo scene showcasing the full feedback flow.