using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vivint.ServiceBus.RequestResponse
{
    public class Responder<TRequest, TResponse>
    {
        private IQueueClient clientRecieve;
        private readonly Func<TRequest, TResponse> generateResponse;
        private readonly string connectionStr;
        private readonly string queue;

        public event Action<Message> RequestReceived;
        public event Action<Message> ResponseSending;
        public event Action<ExceptionReceivedEventArgs> ExceptionOccurred;

        public bool IsListening { get; private set; }

        internal Responder(string connectionString, string queueListen, Func<TRequest, TResponse> requestHandler)
        {
            generateResponse = requestHandler;
            connectionStr = connectionString;
            queue = queueListen;
        }

        public async Task RespondToRequests()
        {
            clientRecieve = new QueueClient(connectionStr, queue);

            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };

            clientRecieve.RegisterMessageHandler(ProcessMessageAsync, messageHandlerOptions);

            IsListening = true;

            while (true)
            {
                await Task.Delay(1);
            }
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs args)
        {
            ExceptionOccurred?.Invoke(args);
            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(Message m, CancellationToken token)
        {
            RequestReceived?.Invoke(m);

            var jsonBody = Encoding.UTF8.GetString(m.Body);
            var request = JsonConvert.DeserializeObject<TRequest>(jsonBody);

            await clientRecieve.CompleteAsync(m.SystemProperties.LockToken);

            await SendResponse(request, m.ReplyTo, m.ReplyToSessionId);
        }

        private async Task SendResponse(TRequest request, string replyToQueue, string sessionId)
        {
            var response = generateResponse(request);

            var message = Helpers.CreateMessage(response);
            message.SessionId = sessionId;

            ResponseSending?.Invoke(message);

            var clientRespond = new QueueClient(connectionStr, replyToQueue);
            await clientRespond.SendAsync(message);
            await clientRespond.CloseAsync();
        }
    }
}
