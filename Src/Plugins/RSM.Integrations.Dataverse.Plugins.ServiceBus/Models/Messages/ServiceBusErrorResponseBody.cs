using System;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

namespace RSM.Integrations.Dataverse.Models.Messages
{
    public class ServiceBusErrorResponseBody
    {
        public int Code { get; private set; }
        public string Detail { get; private set; }

        public ServiceBusErrorResponseBody(HttpContent errorResponseContent)
        {
            try
            {
                var rootErrorElement = XDocument.Parse(errorResponseContent.ReadAsStringAsync().GetAwaiter().GetResult())
                    .Descendants("Error")
                    .FirstOrDefault();

                Code = int.Parse(rootErrorElement?.Descendants("Code").FirstOrDefault()?.Value ?? "0");
                Detail = rootErrorElement?.Descendants("Detail").FirstOrDefault()?.Value ?? "Unknown error";
            }
            catch (Exception)
            {
                Code = 0;
                Detail = "Unknown error";
            }
        }
    }
}