using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private int sendCount = 0;

        public event Action<TRequest, Message> RequestSending;

        public double MinRTTms { get; private set; }
        public double AverageRTTms { get; private set; }
        public double MaxRTTms { get; private set; }

        public void ResetAverageRTT()
        {
            sendCount = 0;
            AverageRTTms = 0;
            MinRTTms = 0;
            MaxRTTms = 0;
        }

        internal Sender(string connectionString, string queueSend, string queueReceive)
        {
            client = new QueueClient(connectionString, queueSend);
            connectionStr = connectionString;
            receiveResponseQueueName = queueReceive;
        }

        public async Task<TResponse> SendRequest(TRequest request)
        {
            var sw = new Stopwatch();
            sw.Start();

            var sessionId = Guid.NewGuid().ToString("N");

            var message = Helpers.CreateMessage(request);
            message.ReplyTo = receiveResponseQueueName;
            message.ReplyToSessionId = sessionId;

            var sessionClient = new SessionClient(connectionStr, receiveResponseQueueName);
            var messageSession = await sessionClient.AcceptMessageSessionAsync(sessionId);
            var receiveTask = messageSession.ReceiveAsync(TimeSpan.FromSeconds(20));

            // SEND!
            RequestSending?.Invoke(request, message);
            await client.SendAsync(message);

            // RECEIVE!
            var responseMsg = await receiveTask;
            await messageSession.CloseAsync();

            sw.Stop();
            AverageRTTms = (AverageRTTms * sendCount + sw.ElapsedMilliseconds) / (++sendCount);
            if (sw.ElapsedMilliseconds < MinRTTms || MinRTTms == 0) MinRTTms = sw.ElapsedMilliseconds;
            if (sw.ElapsedMilliseconds > MaxRTTms) MaxRTTms = sw.ElapsedMilliseconds;

            return Helpers.DecodeMessage<TResponse>(responseMsg);
        }
    }
}
