using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BumiMobile.Api
{
    /// <summary>
    /// Production-ready API client for Unity.
    /// Features: async/await, cancellation, timeout, retry, logging, Newtonsoft.Json support.
    /// </summary>
    public class ApiController : IApiClient
    {
        #region Fields & Properties

        private readonly string _baseUrl;
        private readonly Dictionary<string, string> _defaultHeaders;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ApiClientSettings _settings;
        private bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new ApiController with default settings
        /// </summary>
        public ApiController(string baseUrl) 
            : this(baseUrl, new NewtonsoftJsonSerializer(), ApiClientSettings.Default)
        {
        }

        /// <summary>
        /// Create a new ApiController with custom JSON serializer
        /// </summary>
        public ApiController(string baseUrl, IJsonSerializer jsonSerializer)
            : this(baseUrl, jsonSerializer, ApiClientSettings.Default)
        {
        }

        /// <summary>
        /// Create a new ApiController with full customization
        /// </summary>
        public ApiController(string baseUrl, IJsonSerializer jsonSerializer, ApiClientSettings settings)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            _jsonSerializer = jsonSerializer ?? new NewtonsoftJsonSerializer();
            _settings = settings ?? ApiClientSettings.Default;
            _defaultHeaders = new Dictionary<string, string>();
        }

        #endregion

        #region Header Management

        public void SetDefaultHeader(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            _defaultHeaders[key] = value;
            Log($"Header set: {key}");
        }

        public void RemoveDefaultHeader(string key)
        {
            if (_defaultHeaders.Remove(key))
            {
                Log($"Header removed: {key}");
            }
        }

        public void SetApiKey(string apiKey, string headerName = "x-api-key")
        {
            SetDefaultHeader(headerName, apiKey);
        }

        private void ApplyHeaders(UnityWebRequest request, Dictionary<string, string> customHeaders = null, bool skipContentType = false)
        {
            foreach (var header in _defaultHeaders)
            {
                if (skipContentType && header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    continue;
                request.SetRequestHeader(header.Key, header.Value);
            }

            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    if (skipContentType && header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        continue;
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }
        }

        #endregion

        #region GET Requests

        public async Task<ApiResponse<T>> GetAsync<T>(
            string endpoint,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            var rawResponse = await GetRawAsync(endpoint, customHeaders, cancellationToken);
            
            if (!rawResponse.Success)
            {
                return ApiResponse<T>.Fail(rawResponse.Error, rawResponse.StatusCode, rawResponse.RawResponse, rawResponse.RequestDurationMs);
            }

            if (_jsonSerializer.TryDeserialize<T>(rawResponse.Data, out var data, out var error))
            {
                return ApiResponse<T>.Ok(data, rawResponse.StatusCode, rawResponse.RawResponse, rawResponse.RequestDurationMs);
            }

            return ApiResponse<T>.Fail($"Deserialization error: {error}", rawResponse.StatusCode, rawResponse.RawResponse, rawResponse.RequestDurationMs);
        }

        public async Task<ApiResponse<string>> GetRawAsync(
            string endpoint,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(
                () => ExecuteRequestAsync<string>(
                    HttpMethod.GET,
                    endpoint,
                    null,
                    customHeaders,
                    cancellationToken,
                    returnRawString: true),
                cancellationToken);
        }

        #endregion

        #region POST Requests

        public async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest data,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            string jsonData = _jsonSerializer.Serialize(data);
            return await ExecuteWithRetryAsync(
                () => ExecuteRequestAsync<TResponse>(
                    HttpMethod.POST,
                    endpoint,
                    jsonData,
                    customHeaders,
                    cancellationToken),
                cancellationToken);
        }

        public async Task<ApiResponse<string>> PostAsync<TRequest>(
            string endpoint,
            TRequest data,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            string jsonData = _jsonSerializer.Serialize(data);
            return await ExecuteWithRetryAsync(
                () => ExecuteRequestAsync<string>(
                    HttpMethod.POST,
                    endpoint,
                    jsonData,
                    customHeaders,
                    cancellationToken,
                    returnRawString: true),
                cancellationToken);
        }

        public async Task<ApiResponse<TResponse>> PostMultipartAsync<TResponse>(
            string endpoint,
            List<IMultipartFormSection> formData,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(
                () => ExecuteMultipartRequestAsync<TResponse>(endpoint, formData, customHeaders, cancellationToken),
                cancellationToken);
        }

        public async Task<ApiResponse<TResponse>> PostFileAsync<TResponse>(
            string endpoint,
            byte[] fileData,
            string fileName,
            string fieldName = "file",
            Dictionary<string, string> formFields = null,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            var formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection(fieldName, fileData, fileName, "application/octet-stream")
            };

            if (formFields != null)
            {
                foreach (var field in formFields)
                {
                    formData.Add(new MultipartFormDataSection(field.Key, field.Value));
                }
            }

            return await PostMultipartAsync<TResponse>(endpoint, formData, customHeaders, cancellationToken);
        }

        #endregion

        #region PUT/DELETE/PATCH Requests

        public async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(
            string endpoint,
            TRequest data,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            string jsonData = _jsonSerializer.Serialize(data);
            return await ExecuteWithRetryAsync(
                () => ExecuteRequestAsync<TResponse>(
                    HttpMethod.PUT,
                    endpoint,
                    jsonData,
                    customHeaders,
                    cancellationToken),
                cancellationToken);
        }

        public async Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(
            string endpoint,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(
                () => ExecuteRequestAsync<TResponse>(
                    HttpMethod.DELETE,
                    endpoint,
                    null,
                    customHeaders,
                    cancellationToken),
                cancellationToken);
        }

        public async Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(
            string endpoint,
            TRequest data,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            string jsonData = _jsonSerializer.Serialize(data);
            return await ExecuteWithRetryAsync(
                () => ExecuteRequestAsync<TResponse>(
                    HttpMethod.PATCH,
                    endpoint,
                    jsonData,
                    customHeaders,
                    cancellationToken),
                cancellationToken);
        }

        #endregion

        #region Core Request Execution

        private async Task<ApiResponse<T>> ExecuteRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            string jsonBody,
            Dictionary<string, string> customHeaders,
            CancellationToken cancellationToken,
            bool returnRawString = false)
        {
            string url = BuildUrl(endpoint);
            float startTime = Time.realtimeSinceStartup;

            Log($"[{method}] {url}");

            using (var request = CreateRequest(method, url, jsonBody))
            {
                ApplyHeaders(request, customHeaders);

                try
                {
                    await WaitForRequestAsync(request, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    request.Abort();
                    Log($"[{method}] Cancelled: {url}");
                    return ApiResponse<T>.Cancelled();
                }
                catch (TimeoutException)
                {
                    request.Abort();
                    LogWarning($"[{method}] Timeout: {url}");
                    return ApiResponse<T>.Timeout(_settings.TimeoutSeconds);
                }

                float duration = (Time.realtimeSinceStartup - startTime) * 1000f;
                string responseText = request.downloadHandler?.text ?? string.Empty;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string error = request.error ?? "Unknown error";
                    LogError($"[{method}] Failed: {url} - {error} (Status: {request.responseCode})");
                    
                    if (_settings.LogResponseOnError)
                    {
                        LogError($"Response: {responseText}");
                    }

                    return ApiResponse<T>.Fail(error, request.responseCode, responseText, duration);
                }

                Log($"[{method}] Success: {url} (Status: {request.responseCode}, Duration: {duration:F0}ms)");

                // Return raw string if requested
                if (returnRawString && typeof(T) == typeof(string))
                {
                    return ApiResponse<T>.Ok((T)(object)responseText, request.responseCode, responseText, duration);
                }

                // Deserialize response
                if (_jsonSerializer.TryDeserialize<T>(responseText, out var data, out var deserializeError))
                {
                    return ApiResponse<T>.Ok(data, request.responseCode, responseText, duration);
                }

                LogWarning($"Deserialization failed: {deserializeError}");
                return ApiResponse<T>.Fail($"Deserialization error: {deserializeError}", request.responseCode, responseText, duration);
            }
        }

        private async Task<ApiResponse<T>> ExecuteMultipartRequestAsync<T>(
            string endpoint,
            List<IMultipartFormSection> formData,
            Dictionary<string, string> customHeaders,
            CancellationToken cancellationToken)
        {
            string url = BuildUrl(endpoint);
            float startTime = Time.realtimeSinceStartup;

            Log($"[POST Multipart] {url}");

            using (var request = UnityWebRequest.Post(url, formData))
            {
                // Skip Content-Type for multipart (Unity sets it with boundary)
                ApplyHeaders(request, customHeaders, skipContentType: true);

                try
                {
                    await WaitForRequestAsync(request, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    request.Abort();
                    return ApiResponse<T>.Cancelled();
                }
                catch (TimeoutException)
                {
                    request.Abort();
                    return ApiResponse<T>.Timeout(_settings.TimeoutSeconds);
                }

                float duration = (Time.realtimeSinceStartup - startTime) * 1000f;
                string responseText = request.downloadHandler?.text ?? string.Empty;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    LogError($"[POST Multipart] Failed: {request.error}");
                    return ApiResponse<T>.Fail(request.error, request.responseCode, responseText, duration);
                }

                if (_jsonSerializer.TryDeserialize<T>(responseText, out var data, out var error))
                {
                    return ApiResponse<T>.Ok(data, request.responseCode, responseText, duration);
                }

                return ApiResponse<T>.Fail($"Deserialization error: {error}", request.responseCode, responseText, duration);
            }
        }

        #endregion

        #region Retry Logic

        private async Task<ApiResponse<T>> ExecuteWithRetryAsync<T>(
            Func<Task<ApiResponse<T>>> requestFunc,
            CancellationToken cancellationToken)
        {
            int attempt = 0;
            ApiResponse<T> lastResponse = null;

            while (attempt < _settings.MaxRetries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                lastResponse = await requestFunc();

                if (lastResponse.Success)
                {
                    return lastResponse;
                }

                // Don't retry on client errors (4xx) except specific ones
                if (lastResponse.StatusCode >= 400 && lastResponse.StatusCode < 500)
                {
                    // Only retry on 408 (timeout) and 429 (rate limit)
                    if (lastResponse.StatusCode != 408 && lastResponse.StatusCode != 429)
                    {
                        return lastResponse;
                    }
                }

                attempt++;

                if (attempt < _settings.MaxRetries)
                {
                    float delay = _settings.RetryDelaySeconds * Mathf.Pow(_settings.RetryBackoffMultiplier, attempt - 1);
                    Log($"Retry {attempt}/{_settings.MaxRetries} in {delay:F1}s...");
                    await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                }
            }

            return lastResponse ?? ApiResponse<T>.Fail("Request failed after max retries");
        }

        #endregion

        #region Helpers

        private string BuildUrl(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return _baseUrl;

            endpoint = endpoint.TrimStart('/');
            return $"{_baseUrl}/{endpoint}";
        }

        private UnityWebRequest CreateRequest(HttpMethod method, string url, string jsonBody)
        {
            UnityWebRequest request;

            switch (method)
            {
                case HttpMethod.GET:
                    request = UnityWebRequest.Get(url);
                    break;

                case HttpMethod.POST:
                case HttpMethod.PUT:
                case HttpMethod.PATCH:
                    request = new UnityWebRequest(url, method.ToString());
                    if (!string.IsNullOrEmpty(jsonBody))
                    {
                        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    }
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    break;

                case HttpMethod.DELETE:
                    request = UnityWebRequest.Delete(url);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    break;

                default:
                    throw new ArgumentException($"Unsupported HTTP method: {method}");
            }

            return request;
        }

        private async Task WaitForRequestAsync(UnityWebRequest request, CancellationToken cancellationToken)
        {
            var operation = request.SendWebRequest();
            float startTime = Time.realtimeSinceStartup;

            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Time.realtimeSinceStartup - startTime > _settings.TimeoutSeconds)
                {
                    throw new TimeoutException();
                }

                await Task.Yield();
            }
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_settings.EnableLogging)
            {
                Debug.Log($"[ApiClient] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (_settings.EnableLogging)
            {
                Debug.LogWarning($"[ApiClient] {message}");
            }
        }

        private void LogError(string message)
        {
            // Always log errors
            Debug.LogError($"[ApiClient] {message}");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed)
                return;

            _defaultHeaders?.Clear();
            _disposed = true;
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// HTTP methods supported by ApiController
    /// </summary>
    public enum HttpMethod
    {
        GET,
        POST,
        PUT,
        DELETE,
        PATCH
    }

    /// <summary>
    /// Configuration settings for ApiController
    /// </summary>
    [Serializable]
    public class ApiClientSettings
    {
        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public float TimeoutSeconds = 30f;

        /// <summary>
        /// Maximum number of retry attempts (1 = no retries)
        /// </summary>
        public int MaxRetries = 3;

        /// <summary>
        /// Initial delay between retries in seconds
        /// </summary>
        public float RetryDelaySeconds = 1f;

        /// <summary>
        /// Multiplier for exponential backoff (e.g., 2 = double delay each retry)
        /// </summary>
        public float RetryBackoffMultiplier = 2f;

        /// <summary>
        /// Enable debug logging
        /// </summary>
        public bool EnableLogging = true;

        /// <summary>
        /// Log response body on error (useful for debugging, disable in production)
        /// </summary>
        public bool LogResponseOnError = true;

        /// <summary>
        /// Default settings for production use
        /// </summary>
        public static ApiClientSettings Default => new ApiClientSettings();

        /// <summary>
        /// Settings optimized for development/debugging
        /// </summary>
        public static ApiClientSettings Development => new ApiClientSettings
        {
            TimeoutSeconds = 60f,
            MaxRetries = 1,
            EnableLogging = true,
            LogResponseOnError = true
        };

        /// <summary>
        /// Settings optimized for production (less logging, more retries)
        /// </summary>
        public static ApiClientSettings Production => new ApiClientSettings
        {
            TimeoutSeconds = 30f,
            MaxRetries = 3,
            RetryDelaySeconds = 1f,
            RetryBackoffMultiplier = 2f,
            EnableLogging = false,
            LogResponseOnError = false
        };
    }

    #endregion
}
