using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using RSM.Integrations.Dataverse.Common;
using RSM.Integrations.Dataverse.Common.ExtensionMethods;
using RSM.Integrations.Dataverse.Common.Json;
using RSM.Integrations.Dataverse.Models;
using RSM.Integrations.Dataverse.Models.Enums;
using RSM.Integrations.Dataverse.Models.Messages;

namespace RSM.Integrations.Dataverse.Services
{
    /// <summary>
    /// Dataverse plugin integration logic for Azure Service Bus.
    /// </summary>
    public class ServiceBusService : IDisposable
    {
        public int RetryInterval { get; set; } = 1_000;
        public int MaxRetries { get; set; } = 3;

        private readonly ITracingService _tracingSvc;
        private readonly ServiceBusIntegrationConfig _integrationConfig;
        private readonly HttpClient _svcBusClient;

        public ServiceBusService(ITracingService tracingSvc, ServiceBusIntegrationConfig svcBusIntegrationConfig)
        {
            _svcBusClient = new HttpClient
            {
                BaseAddress = new Uri(svcBusIntegrationConfig.BaseUrl),
                DefaultRequestHeaders =
                {
                    Authorization = new AuthenticationHeaderValue(
                        "SharedAccessSignature",
                        GetSasToken(svcBusIntegrationConfig)
                    )
                }
            };

            _integrationConfig = svcBusIntegrationConfig;
            _tracingSvc = tracingSvc;
        }

        public void Dispose()
        {
            _svcBusClient.Dispose();
        }

        public void SendMessage(string messageJson, Guid correlationId)
        {
            var retryCount = 0;
            try
            {
                if (_svcBusClient.DefaultRequestHeaders.Contains(Constants.Http.Headers.ServiceBus.BrokerProperties))
                {
                    _svcBusClient.DefaultRequestHeaders.Remove(Constants.Http.Headers.ServiceBus.BrokerProperties);
                }

                var brokerProps = new ServiceBusQueueBrokerProperties
                {
                    CorrelationId = correlationId,
                    SessionId = _integrationConfig.SessionId
                };

                _tracingSvc.Trace(
                    $"Setting BrokerProperties:\n\tCorrelationId: {brokerProps.CorrelationId}\n\tSequenceNumber: {brokerProps.SequenceNumber}\n\tSessionId: {brokerProps.SessionId}"
                );

                _svcBusClient.DefaultRequestHeaders.Add(
                    Constants.Http.Headers.ServiceBus.BrokerProperties,
                    brokerProps.BuildJson()
                );

                var response = _svcBusClient.PostAsync(
                        $"/{_integrationConfig.QueueName}/messages",
                        new StringContent(messageJson, Encoding.UTF8, "application/json")
                    )
                    .GetAwaiter()
                    .GetResult();

                if (response.IsSuccessStatusCode)
                {
                    _tracingSvc.Trace("Message sent successfully.");
                    return;
                }

                _tracingSvc.Trace("Failed to send message to Service Bus. Parsing error response");
                var errResponse = new ServiceBusErrorResponseBody(response.Content);
                _tracingSvc.Trace("Error response parsed.");

                #region Retry logic

                while (retryCount < MaxRetries)
                {
                    _tracingSvc.Trace("Status code: {0}, Reason: {1}", response.StatusCode, errResponse.Detail);

                    Task.Delay(RetryInterval).GetAwaiter().GetResult();

                    _tracingSvc.Trace("Retrying message send...");

                    response = _svcBusClient.PostAsync(
                            $"/{_integrationConfig.QueueName}/messages",
                            new StringContent(messageJson, Encoding.UTF8, Constants.Http.Headers.MediaType.ApplicationJson)
                        )
                        .GetAwaiter()
                        .GetResult();

                    if (response.IsSuccessStatusCode)
                    {
                        _tracingSvc.Trace("Message sent successfully after retry.");
                        return;
                    }

                    _tracingSvc.Trace("Failed to send message to Service Bus. Parsing error response");
                    errResponse = new ServiceBusErrorResponseBody(response.Content);
                    _tracingSvc.Trace("Error response parsed.");
                    retryCount++;
                }

                #endregion

                throw new Exception(
                    $"Failed to send message to Service Bus. Status code: {response.StatusCode}, Reason: {errResponse.Detail}"
                );
            }
            catch (Exception ex)
            {
                _tracingSvc.Trace("An error occurred while sending a message to the Service Bus queue: {0}", ex);
                throw;
            }
        }

        /// <summary>
        /// Serializes the entity collection and event context into a JSON message.
        /// </summary>
        public static string GetSerializedEntityMessage(ServiceBusEntityMessage message)
        {
            // TODO: Document and enforce descriptive alias names, prefixed with parent lookup attr name
            return JsonConvert.SerializeObject(message, SerializerSettings.DataverseEntity);
        }

        // TODO: Consider using managed id instead
        private static string GetSasToken(ServiceBusIntegrationConfig busIntegrationConfig)
        {
            var expiryUnixTime = DateTime.UtcNow.Add(busIntegrationConfig.ExpiryTime).ToUnixTimeSeconds();
            var url = $"{busIntegrationConfig.BaseUrl}/{busIntegrationConfig.QueueName}";
            return
                $"sr={WebUtility.UrlEncode(url)}&sig={WebUtility.UrlEncode(GetSignature(url, busIntegrationConfig.SharedAccessKey, expiryUnixTime))}&se={expiryUnixTime}&skn={busIntegrationConfig.SharedAccessKeyName}";
        }

        private static string GetSignature(string url, string key, long expiryEpoch)
        {
            var encoding = new UTF8Encoding();

            using (var hmacsha256 = new HMACSHA256(encoding.GetBytes(key)))
            {
                var buffer = encoding.GetBytes($"{WebUtility.UrlEncode(url)}\n{expiryEpoch}");
                return Convert.ToBase64String(hmacsha256.ComputeHash(buffer));
            }
        }
    }
}