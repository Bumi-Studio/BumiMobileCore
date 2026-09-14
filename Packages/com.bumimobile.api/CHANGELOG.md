# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.0.1] - 2026-01-13

### Added

- Initial release of BumiMobile API package
- `ApiController` - Production-ready API client with full HTTP method support (GET, POST, PUT, DELETE, PATCH)
- `ApiResponse<T>` - Generic response wrapper with success/error handling
- `ApiConfiguration` - ScriptableObject-based configuration with code generation
- `IApiClient` - Interface for dependency injection and unit testing
- `IJsonSerializer` - Pluggable JSON serialization interface
- `NewtonsoftJsonSerializer` - Default JSON serializer using Newtonsoft.Json
- Async/await support with proper cancellation token handling
- Configurable timeout and retry logic with exponential backoff
- Multipart form data and file upload support
- Default header management and API key support
- Comprehensive logging with configurable verbosity
