using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivint.ServiceBus.RequestResponse
{
    public class Sender<TRequest, TResponse>
    {
        private readonly IQueueClient client;
        private readonly string connectionStr;
        private readonly string receiveResponseQueueName;

        public event Action<TRequest, Message> RequestSending;

        internal Sender(string connectionString, string queueSend, string queueReceive)
        {
            client = new QueueClient(connectionString, queueSend);
            connectionStr = connectionString;
            receiveResponseQueueName = queueReceive;
        }

        public async Task<TResponse> SendRequest(TRequest request)
        {
            var sessionId = Guid.NewGuid().ToString("N");

            var message = Helpers.CreateMessage(request);
            message.ReplyTo = receiveResponseQueueName;
            message.ReplyToSessionId = sessionId;

            // SEND!
            RequestSending?.Invoke(request, message);
            await client.SendAsync(message);

            // RECEIVE!
            var sessionClient = new SessionClient(connectionStr, receiveResponseQueueName);
            var messageSession = await sessionClient.AcceptMessageSessionAsync(sessionId);
            var responseMsg = await messageSession.ReceiveAsync(TimeSpan.FromSeconds(20));
            await messageSession.CloseAsync();

            return Helpers.DecodeMessage<TResponse>(responseMsg);
        }
    }
}
