using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BumiMobile.Api
{
    /// <summary>
    /// Interface for API client operations. Enables dependency injection and unit testing.
    /// </summary>
    public interface IApiClient : IDisposable
    {
        /// <summary>
        /// Set a default header that will be included in all requests
        /// </summary>
        void SetDefaultHeader(string key, string value);

        /// <summary>
        /// Remove a default header
        /// </summary>
        void RemoveDefaultHeader(string key);

        /// <summary>
        /// Set API key as a default header
        /// </summary>
        void SetApiKey(string apiKey, string headerName = "x-api-key");

        /// <summary>
        /// GET request to retrieve an object of type T
        /// </summary>
        Task<ApiResponse<T>> GetAsync<T>(
            string endpoint,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// GET request to retrieve raw string response
        /// </summary>
        Task<ApiResponse<string>> GetRawAsync(
            string endpoint,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// POST request with typed request and response
        /// </summary>
        Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest data,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// POST request with typed request, returns raw string
        /// </summary>
        Task<ApiResponse<string>> PostAsync<TRequest>(
            string endpoint,
            TRequest data,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// POST multipart/form-data request
        /// </summary>
        Task<ApiResponse<TResponse>> PostMultipartAsync<TResponse>(
            string endpoint,
            List<UnityEngine.Networking.IMultipartFormSection> formData,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// POST with file upload
        /// </summary>
        Task<ApiResponse<TResponse>> PostFileAsync<TResponse>(
            string endpoint,
            byte[] fileData,
            string fileName,
            string fieldName = "file",
            Dictionary<string, string> formFields = null,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// PUT request with typed request and response
        /// </summary>
        Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(
            string endpoint,
            TRequest data,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// DELETE request
        /// </summary>
        Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(
            string endpoint,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// PATCH request with typed request and response
        /// </summary>
        Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(
            string endpoint,
            TRequest data,
            Dictionary<string, string> customHeaders = null,
            CancellationToken cancellationToken = default);
    }
}
