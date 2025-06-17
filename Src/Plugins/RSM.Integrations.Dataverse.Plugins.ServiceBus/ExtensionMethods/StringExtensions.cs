using System;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace RSM.Integrations.Dataverse.Common.ExtensionMethods
{
    public static class StringExtensions
    {
        public static bool TryDeserialize<T>(this string jsonString, out T obj, ITracingService tracingSvc = null)
            where T : class
        {
            obj = null;

            try
            {
                obj = JsonConvert.DeserializeObject<T>(jsonString);
                return obj != null;
            }
            catch (Exception ex)
            {
                tracingSvc?.Trace("Error deserializing JSON: {0}", ex);
                return false;
            }
        }
    }
}