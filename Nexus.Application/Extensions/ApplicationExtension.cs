using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nexus.Application.Extensions
{
    public static class ApplicationExtension
    {
        public static T SafeDeserialize<T>(this string json)
        {
            try
            {
                var obj = JsonSerializer.Deserialize<T>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (obj == null)
                    throw new JsonException("Deserialized object is null");

                return obj;
            }
            catch (Exception ex)
            {
                throw new JsonException($"Failed to deserialize JSON to {typeof(T).Name}", ex);
            }
        }
    }
}
