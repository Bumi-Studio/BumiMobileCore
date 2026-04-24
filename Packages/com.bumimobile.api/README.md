# BumiMobile API

A production-ready HTTP API client for Unity with async/await support, automatic retries, timeout handling, and Newtonsoft.Json serialization.

## Features

- ✅ **Async/Await** - Modern C# async patterns with proper Unity integration
- ✅ **All HTTP Methods** - GET, POST, PUT, DELETE, PATCH support
- ✅ **Automatic Retries** - Configurable retry logic with exponential backoff
- ✅ **Timeout Handling** - Request timeouts with proper cleanup
- ✅ **Cancellation Support** - Cancel requests using CancellationToken
- ✅ **File Upload** - Multipart form data support for file uploads
- ✅ **JSON Serialization** - Built-in Newtonsoft.Json integration
- ✅ **ScriptableObject Config** - Easy configuration via Unity Inspector
- ✅ **Code Generation** - Auto-generate static route classes
- ✅ **Dependency Injection** - Interface-based design for easy testing

## Requirements

- Unity 2021.3 or later
- Newtonsoft.Json (automatically installed as dependency)

## Installation

### Option 1: Via Package Manager (Recommended)

1. Open **Window > Package Manager**
2. Click the **+** button in the top-left corner
3. Select **Add package from git URL...**
4. Enter the repository URL or local path

### Option 2: Via manifest.json

Add the following to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.bumimobile.api": "file:../path/to/com.bumimobile.api"
  }
}
```

### Option 3: Local Package

1. Copy the `com.bumimobile.api` folder to your project's `Packages` directory
2. Unity will automatically detect and import the package

## Quick Start

### Basic Usage

```csharp
using BumiMobile.Api;
using UnityEngine;

public class MyApiExample : MonoBehaviour
{
    private ApiController _api;

    void Start()
    {
        // Create API client
        _api = new ApiController("https://api.example.com");
        
        // Set default headers (optional)
        _api.SetDefaultHeader("Accept", "application/json");
        _api.SetApiKey("your-api-key");
        
        // Make a request
        FetchData();
    }

    async void FetchData()
    {
        var response = await _api.GetAsync<MyDataModel>("endpoint");
        
        if (response.Success)
        {
            Debug.Log($"Data: {response.Data}");
        }
        else
        {
            Debug.LogError($"Error: {response.Error}");
        }
    }

    void OnDestroy()
    {
        _api?.Dispose();
    }
}

[System.Serializable]
public class MyDataModel
{
    public int Id;
    public string Name;
}
```

### Using ScriptableObject Configuration

1. Create a configuration asset: **Right-click > Create > BumiMobile > Api > ApiConfiguration**
2. Configure the base URL and settings in the Inspector
3. Reference it in your script:

```csharp
using BumiMobile.Api;
using UnityEngine;

public class MyApiExample : MonoBehaviour
{
    [SerializeField] private ApiConfiguration _apiConfig;
    
    private IApiClient _api;

    void Awake()
    {
        _api = _apiConfig.CreateClient();
    }

    void OnDestroy()
    {
        _api?.Dispose();
    }
}
```

## API Reference

### Creating an API Client

```csharp
// Simple - just base URL
var api = new ApiController("https://api.example.com");

// With custom JSON serializer
var api = new ApiController("https://api.example.com", new NewtonsoftJsonSerializer());

// With full customization
var settings = new ApiClientSettings
{
    TimeoutSeconds = 30f,
    MaxRetries = 3,
    RetryDelaySeconds = 1f,
    RetryBackoffMultiplier = 2f,
    EnableLogging = true,
    LogResponseOnError = true
};
var api = new ApiController("https://api.example.com", new NewtonsoftJsonSerializer(), settings);

// Using preset settings
var api = new ApiController("https://api.example.com", null, ApiClientSettings.Production);
var api = new ApiController("https://api.example.com", null, ApiClientSettings.Development);
```

### GET Requests

```csharp
// GET with typed response
var response = await api.GetAsync<UserModel>("users/123");

// GET with raw string response
var response = await api.GetRawAsync("users/123");

// GET with custom headers
var headers = new Dictionary<string, string>
{
    { "X-Custom-Header", "value" }
};
var response = await api.GetAsync<UserModel>("users/123", headers);

// GET with cancellation
var cts = new CancellationTokenSource();
var response = await api.GetAsync<UserModel>("users/123", cancellationToken: cts.Token);

// Cancel the request
cts.Cancel();
```

### POST Requests

```csharp
// POST with typed request and response
var request = new CreateUserRequest { Name = "John", Email = "john@example.com" };
var response = await api.PostAsync<CreateUserRequest, UserModel>("users", request);

// POST with typed request, raw string response
var response = await api.PostAsync<CreateUserRequest>("users", request);

// POST with custom headers
var response = await api.PostAsync<CreateUserRequest, UserModel>(
    "users", 
    request, 
    customHeaders: new Dictionary<string, string> { { "X-Request-Id", "123" } }
);
```

### PUT, PATCH, DELETE Requests

```csharp
// PUT
var response = await api.PutAsync<UpdateUserRequest, UserModel>("users/123", updateRequest);

// PATCH
var response = await api.PatchAsync<PatchUserRequest, UserModel>("users/123", patchRequest);

// DELETE
var response = await api.DeleteAsync<DeleteResponse>("users/123");
```

### File Upload

```csharp
// Simple file upload
byte[] fileData = File.ReadAllBytes("path/to/file.png");
var response = await api.PostFileAsync<UploadResponse>(
    "upload",
    fileData,
    "image.png",
    fieldName: "file"
);

// File upload with additional form fields
var response = await api.PostFileAsync<UploadResponse>(
    "upload",
    fileData,
    "image.png",
    fieldName: "file",
    formFields: new Dictionary<string, string>
    {
        { "description", "Profile picture" },
        { "category", "avatar" }
    }
);

// Multipart form data
var formData = new List<IMultipartFormSection>
{
    new MultipartFormFileSection("file", fileData, "image.png", "image/png"),
    new MultipartFormDataSection("description", "My file")
};
var response = await api.PostMultipartAsync<UploadResponse>("upload", formData);
```

### Header Management

```csharp
// Set default header (included in all requests)
api.SetDefaultHeader("Authorization", "Bearer token123");

// Set API key (convenience method)
api.SetApiKey("your-api-key");                    // Uses "x-api-key" header
api.SetApiKey("your-api-key", "X-API-Key");       // Custom header name

// Remove default header
api.RemoveDefaultHeader("Authorization");
```

### Handling Responses

```csharp
var response = await api.GetAsync<UserModel>("users/123");

// Check success
if (response.Success)
{
    UserModel user = response.Data;
    Debug.Log($"User: {user.Name}");
}
else
{
    Debug.LogError($"Error: {response.Error}");
}

// Implicit bool conversion
if (response)
{
    // Success
}

// Access response details
long statusCode = response.StatusCode;           // HTTP status code
string rawJson = response.RawResponse;           // Raw JSON string
float duration = response.RequestDurationMs;     // Request duration in ms
string error = response.Error;                   // Error message (if failed)
```

### Error Handling

```csharp
var response = await api.GetAsync<UserModel>("users/123");

if (!response.Success)
{
    switch (response.StatusCode)
    {
        case 400:
            Debug.LogError("Bad request");
            break;
        case 401:
            Debug.LogError("Unauthorized - please login");
            break;
        case 403:
            Debug.LogError("Forbidden - access denied");
            break;
        case 404:
            Debug.LogError("Not found");
            break;
        case 429:
            Debug.LogError("Rate limited - try again later");
            break;
        case >= 500:
            Debug.LogError($"Server error: {response.Error}");
            break;
        default:
            Debug.LogError($"Request failed: {response.Error}");
            break;
    }
}
```

### Cancellation

```csharp
private CancellationTokenSource _cts;

public async void StartRequest()
{
    _cts = new CancellationTokenSource();
    
    try
    {
        var response = await api.GetAsync<DataModel>("data", cancellationToken: _cts.Token);
        
        if (response.Success)
        {
            // Handle success
        }
    }
    catch (OperationCanceledException)
    {
        Debug.Log("Request was cancelled");
    }
}

public void CancelRequest()
{
    _cts?.Cancel();
}

void OnDestroy()
{
    _cts?.Cancel();
    _cts?.Dispose();
}
```

### Search with Debounce Pattern

```csharp
private CancellationTokenSource _searchCts;

public async void OnSearchInputChanged(string query)
{
    // Cancel previous search
    _searchCts?.Cancel();
    _searchCts = new CancellationTokenSource();
    
    try
    {
        // Debounce - wait 300ms before searching
        await Task.Delay(300, _searchCts.Token);
        
        var response = await api.GetAsync<SearchResult[]>(
            $"search?q={Uri.EscapeDataString(query)}",
            cancellationToken: _searchCts.Token
        );
        
        if (response.Success)
        {
            DisplayResults(response.Data);
        }
    }
    catch (OperationCanceledException)
    {
        // Expected - new search started
    }
}
```

## Configuration Options

### ApiClientSettings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TimeoutSeconds` | float | 30 | Request timeout in seconds |
| `MaxRetries` | int | 3 | Maximum retry attempts (1 = no retries) |
| `RetryDelaySeconds` | float | 1 | Initial delay between retries |
| `RetryBackoffMultiplier` | float | 2 | Multiplier for exponential backoff |
| `EnableLogging` | bool | true | Enable debug logging |
| `LogResponseOnError` | bool | true | Log response body on error |

### Preset Configurations

```csharp
// Default settings
ApiClientSettings.Default

// Development (longer timeout, verbose logging)
ApiClientSettings.Development
// TimeoutSeconds = 60, MaxRetries = 1, EnableLogging = true

// Production (optimized, minimal logging)
ApiClientSettings.Production
// TimeoutSeconds = 30, MaxRetries = 3, EnableLogging = false
```

## Route Management with ApiConfiguration

### Setting Up Routes

1. Create an `ApiConfiguration` asset
2. Add routes in the Inspector:

| Name | Route |
|------|-------|
| GetUsers | users |
| GetUser | users/{id} |
| CreateUser | users |
| Login | auth/login |

3. Click **Generate Static API Class** in the context menu

### Using Generated Routes

```csharp
// Auto-generated ApiRoutes class
public static class ApiRoutes
{
    public static string BaseUrl => "https://api.example.com";
    public static string GetUsers => "users";
    public static string GetUser => "users/{id}";
    public static string CreateUser => "users";
    public static string Login => "auth/login";
    
    public static ApiController CreateClient();
}

// Usage
var api = ApiRoutes.CreateClient();
var response = await api.GetAsync<User[]>(ApiRoutes.GetUsers);
```

## Custom JSON Serializer

Implement `IJsonSerializer` to use a different JSON library:

```csharp
public class UnityJsonSerializer : IJsonSerializer
{
    public string Serialize<T>(T obj)
    {
        return JsonUtility.ToJson(obj);
    }

    public T Deserialize<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }

    public bool TryDeserialize<T>(string json, out T result, out string error)
    {
        try
        {
            result = JsonUtility.FromJson<T>(json);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            result = default;
            error = ex.Message;
            return false;
        }
    }
}

// Usage
var api = new ApiController("https://api.example.com", new UnityJsonSerializer());
```

## Samples

Import samples via Package Manager:

1. Open **Window > Package Manager**
2. Select **BumiMobile API**
3. Expand **Samples**
4. Click **Import** next to "Usage Example"

## Best Practices

### 1. Always Dispose the Client

```csharp
void OnDestroy()
{
    _api?.Dispose();
}
```

### 2. Use CancellationTokens

```csharp
// Cancel requests when leaving a scene or destroying objects
private CancellationTokenSource _cts;

void OnEnable()
{
    _cts = new CancellationTokenSource();
}

void OnDisable()
{
    _cts?.Cancel();
    _cts?.Dispose();
}
```

### 3. Use Interface for Testing

```csharp
public class MyService
{
    private readonly IApiClient _api;
    
    // Inject via constructor for easy mocking in tests
    public MyService(IApiClient api)
    {
        _api = api;
    }
}
```

### 4. Handle All Response States

```csharp
var response = await api.GetAsync<Data>("endpoint");

if (response.Success)
{
    // Handle success
}
else if (response.StatusCode == 0)
{
    // Network error or cancelled
}
else if (response.StatusCode >= 400 && response.StatusCode < 500)
{
    // Client error
}
else
{
    // Server error
}
```

### 5. Use ScriptableObject Config for Different Environments

Create separate `ApiConfiguration` assets:
- `ApiConfig_Development.asset` - Local/dev server
- `ApiConfig_Staging.asset` - Staging server
- `ApiConfig_Production.asset` - Production server

## Troubleshooting

### Request Timeout

Increase the timeout in settings:

```csharp
var settings = new ApiClientSettings { TimeoutSeconds = 60f };
```

### SSL/Certificate Errors

For development with self-signed certificates, you may need to implement a custom certificate handler (not recommended for production).

### CORS Issues (WebGL)

Ensure your server sends proper CORS headers for WebGL builds.

## License

Copyright (c) 2026 BumiMobile. All rights reserved.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.
