using System;
using Newtonsoft.Json;
using UnityEngine;

namespace BumiMobile.Api
{
    /// <summary>
    /// JSON serializer implementation using Newtonsoft.Json.
    /// Provides robust serialization with full feature support.
    /// </summary>
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public NewtonsoftJsonSerializer() : this(CreateDefaultSettings())
        {
        }

        public NewtonsoftJsonSerializer(JsonSerializerSettings settings)
        {
            _settings = settings ?? CreateDefaultSettings();
        }

        private static JsonSerializerSettings CreateDefaultSettings()
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
        }

        public string Serialize<T>(T obj)
        {
            if (obj == null)
                return "null";

            return JsonConvert.SerializeObject(obj, _settings);
        }

        public T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            return JsonConvert.DeserializeObject<T>(json, _settings);
        }

        public bool TryDeserialize<T>(string json, out T result, out string error)
        {
            result = default;
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "JSON string is null or empty";
                return false;
            }

            try
            {
                result = JsonConvert.DeserializeObject<T>(json, _settings);
                return true;
            }
            catch (JsonException ex)
            {
                error = ex.Message;
                Debug.LogWarning($"[JsonSerializer] Failed to deserialize: {ex.Message}");
                return false;
            }
        }
    }
}
