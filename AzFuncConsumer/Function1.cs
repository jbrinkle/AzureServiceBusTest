using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using SBRRTest;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace AzFuncConsumer
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([ServiceBusTrigger("thequeue", AccessRights.Manage, Connection = "ServiceBusConnection")]BrokeredMessage myQueueItem, TraceWriter log)
        {
            log.Info($"[{myQueueItem.MessageId}] Incoming message");

            var request = GetRequest(myQueueItem, log).GetAwaiter().GetResult();

            var response = new GetLoanOptionsResponsePayload
            {
                Provider = "FUNCBRINKLE",
                LoanAmount = request.CreditScore * 4
            };

            log.Info($"Will respond on {myQueueItem.ReplyTo} with session {myQueueItem.ReplyToSessionId}");

            SendResponse(myQueueItem.MessageId, response, myQueueItem.ReplyTo, myQueueItem.ReplyToSessionId, log).GetAwaiter().GetResult();
        }

        private static async Task<GetLoanOptionsRequestPayload> GetRequest(BrokeredMessage incomingMsg, TraceWriter log)
        {
            try
            {
                var stream = incomingMsg.GetBody<Stream>();

                using (var reader = new StreamReader(stream))
                {
                    var body = await reader.ReadToEndAsync();
                    log.Info($"{incomingMsg.MessageId} Body: {body}");
                    return JsonConvert.DeserializeObject<GetLoanOptionsRequestPayload>(body);
                }
            }
            catch (Exception e)
            {
                log.Error($"[{incomingMsg.MessageId}] Error reading message body: " + e.Message);
                return null;
            }
        }

        private static async Task SendResponse(string messageId, GetLoanOptionsResponsePayload response, string replyTo, string replyToSessionId, TraceWriter log)
        {
            var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnection");

            log.Info($"[{messageId}] Service bus connection for response {(serviceBusConnectionString == null ? "NOT FOUND" : "FOUND")}");

            var responsePayload = JsonConvert.SerializeObject(response);

            try
            {
                var responseClient = QueueClient.CreateFromConnectionString(serviceBusConnectionString, replyTo);

                var responseMessage = new BrokeredMessage(responsePayload)
                {
                    SessionId = replyToSessionId
                };

                await responseClient.SendAsync(responseMessage);

                log.Info($"[{messageId}] Response sent!");

                await responseClient.CloseAsync();
            }
            catch (Exception e)
            {
                log.Error($"[{messageId}] Error sending response: " + e.Message);
            }
        }

        private class FakeXmlSerializer : System.Runtime.Serialization.XmlObjectSerializer
        {
            public override bool IsStartObject(XmlDictionaryReader reader) => false;

            public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName) => throw new NotImplementedException();

            public override void WriteEndObject(XmlDictionaryWriter writer)
            {
                
            }

            public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
            {
                
            }

            public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
            {
                
            }
        }
    }
}
