using System;

namespace BumiMobile.Api
{
    /// <summary>
    /// Interface for JSON serialization. Allows swapping between different JSON libraries.
    /// </summary>
    public interface IJsonSerializer
    {
        /// <summary>
        /// Serialize an object to JSON string
        /// </summary>
        string Serialize<T>(T obj);

        /// <summary>
        /// Deserialize JSON string to object
        /// </summary>
        T Deserialize<T>(string json);

        /// <summary>
        /// Try to deserialize JSON string, returns false on failure
        /// </summary>
        bool TryDeserialize<T>(string json, out T result, out string error);
    }
}
