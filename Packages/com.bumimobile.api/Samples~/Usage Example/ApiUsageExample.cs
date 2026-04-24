using System;
using System.Threading;
using UnityEngine;

namespace BumiMobile.Api.Examples
{
    /// <summary>
    /// Example usage of the ApiController.
    /// This file demonstrates common patterns and best practices.
    /// </summary>
    public class ApiUsageExample : MonoBehaviour
    {
        [SerializeField] private ApiConfiguration _apiConfig;
        
        private IApiClient _apiClient;
        private CancellationTokenSource _cts;

        #region Lifecycle

        private void Awake()
        {
            // Option 1: Create from configuration (recommended)
            if (_apiConfig != null)
            {
                _apiClient = _apiConfig.CreateClient();
            }
            
            // Option 2: Create directly with URL
            // _apiClient = new ApiController("https://api.example.com");
            
            // Option 3: Create with custom settings
            // var settings = new ApiClientSettings
            // {
            //     TimeoutSeconds = 60f,
            //     MaxRetries = 5,
            //     EnableLogging = true
            // };
            // _apiClient = new ApiController("https://api.example.com", new NewtonsoftJsonSerializer(), settings);
        }

        private void Start()
        {
            // Set API key if needed
            _apiClient?.SetApiKey("your-api-key-here");
            
            // Set default headers
            _apiClient?.SetDefaultHeader("Accept", "application/json");
        }

        private void OnDestroy()
        {
            // Cancel any pending requests
            _cts?.Cancel();
            _cts?.Dispose();
            
            // Dispose the client
            _apiClient?.Dispose();
        }

        #endregion

        #region Example: GET Request

        public async void FetchUserData(int userId)
        {
            if (_apiClient == null) return;
            
            _cts = new CancellationTokenSource();
            
            try
            {
                var response = await _apiClient.GetAsync<UserResponse>(
                    $"users/{userId}",
                    cancellationToken: _cts.Token);
                
                if (response.Success)
                {
                    Debug.Log($"User: {response.Data.Name}, Email: {response.Data.Email}");
                }
                else
                {
                    Debug.LogError($"Failed to fetch user: {response.Error}");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Request was cancelled");
            }
        }

        #endregion

        #region Example: POST Request

        public async void CreateUser(string name, string email)
        {
            if (_apiClient == null) return;
            
            var request = new CreateUserRequest
            {
                Name = name,
                Email = email
            };
            
            var response = await _apiClient.PostAsync<CreateUserRequest, UserResponse>(
                "users",
                request);
            
            if (response)  // Implicit bool conversion
            {
                Debug.Log($"Created user with ID: {response.Data.Id}");
            }
            else
            {
                HandleError(response);
            }
        }

        #endregion

        #region Example: File Upload

        public async void UploadAvatar(byte[] imageData)
        {
            if (_apiClient == null) return;
            
            var response = await _apiClient.PostFileAsync<UploadResponse>(
                "upload/avatar",
                imageData,
                "avatar.png",
                fieldName: "image",
                formFields: new System.Collections.Generic.Dictionary<string, string>
                {
                    { "description", "User avatar" }
                });
            
            if (response.Success)
            {
                Debug.Log($"Uploaded! URL: {response.Data.Url}");
            }
        }

        #endregion

        #region Example: With Cancellation

        private CancellationTokenSource _searchCts;
        
        public async void SearchWithDebounce(string query)
        {
            // Cancel previous search
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            
            try
            {
                // Wait for debounce
                await System.Threading.Tasks.Task.Delay(300, _searchCts.Token);
                
                var response = await _apiClient.GetAsync<SearchResponse>(
                    $"search?q={Uri.EscapeDataString(query)}",
                    cancellationToken: _searchCts.Token);
                
                if (response.Success)
                {
                    Debug.Log($"Found {response.Data.Results.Length} results");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when search is cancelled by new query
            }
        }

        #endregion

        #region Error Handling

        private void HandleError<T>(ApiResponse<T> response)
        {
            switch (response.StatusCode)
            {
                case 401:
                    Debug.LogError("Unauthorized - please login again");
                    break;
                case 403:
                    Debug.LogError("Forbidden - you don't have access");
                    break;
                case 404:
                    Debug.LogError("Not found");
                    break;
                case 429:
                    Debug.LogError("Rate limited - please wait");
                    break;
                case >= 500:
                    Debug.LogError($"Server error: {response.Error}");
                    break;
                default:
                    Debug.LogError($"Request failed: {response.Error}");
                    break;
            }
        }

        #endregion
    }

    #region Example Data Models

    [Serializable]
    public class UserResponse
    {
        public int Id;
        public string Name;
        public string Email;
    }

    [Serializable]
    public class CreateUserRequest
    {
        public string Name;
        public string Email;
    }

    [Serializable]
    public class UploadResponse
    {
        public string Url;
        public string FileName;
    }

    [Serializable]
    public class SearchResponse
    {
        public SearchResult[] Results;
    }

    [Serializable]
    public class SearchResult
    {
        public string Title;
        public string Description;
    }

    #endregion
}
