using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Vivint.ServiceBus.RequestResponse;

namespace ServiceBusTest
{
    class Program
    {
        private static readonly Dictionary<string, string> config = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            Console.WriteLine("Service Bus Test Tool");
            Console.WriteLine("=============================");

            if (!Config.ParseArgs(args, Console.Out))
                return;

            try
            {
                var factory = new RequestResponseFactory(Config.ConnectionString, Config.QueueSendName, Config.QueueRespondName);
                factory.EnsureEntitiesExist().GetAwaiter().GetResult();

                if (Config.SendRequest)
                {
                    TimeOperation(() => SendRequests(factory)).GetAwaiter().GetResult();
                }
                else if (Config.SendResponse)
                {
                    RunConsumer(factory).GetAwaiter().GetResult();
                }
                else
                {
                    throw new Exception("Fatal: Unrecognized action initiated");
                }
            }
            catch (Exception ex)
            {
                var origColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = origColor;
            }
        }

        static async Task TimeOperation(Func<Task> action)
        {
            var sw = new Stopwatch();

            sw.Start();
            await action();
            sw.Stop();

            var origColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Execution time (sec): {sw.Elapsed.TotalSeconds}");
            Console.ForegroundColor = origColor;
        }

        static async Task SendRequests(RequestResponseFactory factory)
        {
            // get number of requests to make
            var requestsToSend = GetFirstNumberInActionArgs();

            var sender = factory.GetSender<GetLoanOptionsRequestPayload, GetLoanOptionsResponsePayload>();
            sender.RequestSending += m => Console.WriteLine($"SessionId = {m.ReplyToSessionId}");
            var random = new Random();
            var outstandingRequests = new Task[requestsToSend];

            Console.WriteLine("Sending requests...");

            for (var i = 0; i < requestsToSend; i++)
            {
                var payload = new GetLoanOptionsRequestPayload
                {
                    CreditScore = random.Next(300, 801)
                };

                Console.Write($"{i:000} Request: CreditScore = {payload.CreditScore}, ");

                var rememberI = i;
                outstandingRequests[i] = sender.SendRequest(payload).ContinueWith(async t => {
                    var response = await t;
                    Console.WriteLine($"{rememberI:000} Response: Provider = {response.Provider}, Loan = {response.LoanAmount}");
                });
            }

            await Task.WhenAll(outstandingRequests);
        }

        static async Task RunConsumer(RequestResponseFactory factory)
        {
            // get number of consumers to run
            var consumerCount = GetFirstNumberInActionArgs();

            var consumers = new Task[consumerCount];

            for (int i = 0; i < consumerCount; i++)
            {
                consumers[i] = Task.Run(() =>
                {
                    var responder = factory.GetResponder<GetLoanOptionsRequestPayload, GetLoanOptionsResponsePayload>(request =>
                    {
                        return new GetLoanOptionsResponsePayload
                        {
                            Provider = "BRINKLE",
                            LoanAmount = request.CreditScore * 4
                        };
                    });
                    responder.RequestReceived += m => Console.Write($"SessionId = {m.ReplyToSessionId}...");
                    responder.ResponseSending += m => Console.WriteLine("Sending response");
                    responder.ExceptionOccurred += args => Console.WriteLine($"Fatal: {args.Exception.GetType().Name} {args.Exception.Message}");

                    return responder.RespondToRequests();
                });
            }

            Console.WriteLine("Listening on the queue...");
            await Task.WhenAll(consumers);
        }

        static int GetFirstNumberInActionArgs()
        {
            var arg = Config.ActionArgs.FirstOrDefault() ?? "1";
            if (!int.TryParse(arg, out int number))
            {
                throw new Exception($"Value '{arg}' is not an integer");
            }
            return number;
        }
    }
}
