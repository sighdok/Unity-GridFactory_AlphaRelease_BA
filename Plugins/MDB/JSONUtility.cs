//***************************************************************************************
// Writer: Stylish Esper
//***************************************************************************************

using System;
#if INSTALLED_NEWTONSOFTJSON
using Newtonsoft.Json;
#endif

namespace Esper.MemoryDB
{
    public static class JSONUtility
    {
#if INSTALLED_NEWTONSOFTJSON
        private static JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
        };
#endif

        /// <summary>
        /// Converts an object to JSON and returns it.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The object in JSON format.</returns>
        public static string ToJson(this object obj)
        {
#if INSTALLED_NEWTONSOFTJSON
            return JsonConvert.SerializeObject(obj, Formatting.Indented, serializerSettings);
#else
            return null;
#endif
        }

        /// <summary>
        /// Converts a JSON string to an object.
        /// </summary>
        /// <typeparam name="T">The object.</typeparam>
        /// <param name="json">The JSON string.</param>
        /// <returns>The deserialized object.</returns>
        public static T ToObject<T>(this string json)
        {
#if INSTALLED_NEWTONSOFTJSON
            return JsonConvert.DeserializeObject<T>(json, serializerSettings);
#else
            return default;
#endif
        }

        /// <summary>
        /// Converts a JSON string to an object.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="type">The type to convert to.</param>
        /// <returns>The deserialized object.</returns>
        public static object ToObject(this string json, Type type)
        {
#if INSTALLED_NEWTONSOFTJSON
            return JsonConvert.DeserializeObject(json, type, serializerSettings);
#else
            return null;
#endif
        }
    }
}