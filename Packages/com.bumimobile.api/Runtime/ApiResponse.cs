using System;

namespace BumiMobile.Api
{
    /// <summary>
    /// Generic response wrapper for API calls.
    /// Provides consistent structure for success and error handling.
    /// </summary>
    /// <typeparam name="T">The type of data returned on success</typeparam>
    [Serializable]
    public class ApiResponse<T>
    {
        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The deserialized response data (null on failure)
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Error message if request failed
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// HTTP status code from the response
        /// </summary>
        public long StatusCode { get; set; }

        /// <summary>
        /// Raw response body (useful for debugging)
        /// </summary>
        public string RawResponse { get; set; }

        /// <summary>
        /// Time taken for the request in milliseconds
        /// </summary>
        public float RequestDurationMs { get; set; }

        /// <summary>
        /// Create a successful response
        /// </summary>
        public static ApiResponse<T> Ok(T data, long statusCode, string rawResponse = null, float durationMs = 0)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                StatusCode = statusCode,
                RawResponse = rawResponse,
                RequestDurationMs = durationMs
            };
        }

        /// <summary>
        /// Create a failed response
        /// </summary>
        public static ApiResponse<T> Fail(string error, long statusCode = 0, string rawResponse = null, float durationMs = 0)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Error = error,
                StatusCode = statusCode,
                RawResponse = rawResponse,
                RequestDurationMs = durationMs
            };
        }

        /// <summary>
        /// Create a timeout response
        /// </summary>
        public static ApiResponse<T> Timeout(float timeoutSeconds)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Error = $"Request timed out after {timeoutSeconds}s",
                StatusCode = 408 // HTTP 408 Request Timeout
            };
        }

        /// <summary>
        /// Create a cancelled response
        /// </summary>
        public static ApiResponse<T> Cancelled()
        {
            return new ApiResponse<T>
            {
                Success = false,
                Error = "Request was cancelled",
                StatusCode = 0
            };
        }

        /// <summary>
        /// Implicit bool conversion for easy success checking
        /// </summary>
        public static implicit operator bool(ApiResponse<T> response) => response?.Success ?? false;

        public override string ToString()
        {
            return Success
                ? $"[ApiResponse] Success (Status: {StatusCode}, Duration: {RequestDurationMs:F0}ms)"
                : $"[ApiResponse] Failed: {Error} (Status: {StatusCode})";
        }
    }
}
