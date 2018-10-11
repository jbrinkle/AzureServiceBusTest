﻿using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Vivint.ServiceBus.RequestResponse
{
    public class RequestResponseFactory
    {
        /*  Using this architectural strategy
         *  http://cloudcasts.azurewebsites.net/devguide/Default.aspx?id=13051
         */


        private readonly string connectionString;
        private readonly string qRequest;
        private readonly string qResponse;
        private readonly TextWriter output;

        public RequestResponseFactory(string connectionString, string qRequest, string qResponse)
        {
            this.connectionString = connectionString;
            this.qRequest = qRequest;
            this.qResponse = qResponse;
        }

        public Responder<TRequest, TResponse> GetResponder<TRequest, TResponse>(Func<TRequest, TResponse> handler)
        {
            return new Responder<TRequest, TResponse>(connectionString, qRequest, handler);
        }

        public Sender<TRequest, TResponse> GetSender<TRequest, TResponse>()
        {
            return new Sender<TRequest, TResponse>(connectionString, qRequest, qResponse);
        }

        public async Task EnsureEntitiesExist()
        {
            var mc = new ManagementClient(connectionString);
            
            if (! await mc.QueueExistsAsync(qRequest))
            {
                await CreateRequestQueue(mc);
            }
            else if ((await mc.GetQueueAsync(qRequest)).RequiresSession)
            {
                await mc.DeleteQueueAsync(qRequest);
                await CreateRequestQueue(mc);
            }

            if (!await mc.QueueExistsAsync(qResponse))
            {
                await CreateResponseQueue(mc);
            }
            else if (false == (await mc.GetQueueAsync(qResponse)).RequiresSession)
            {
                await mc.DeleteQueueAsync(qResponse);
                await CreateResponseQueue(mc);
            }
        }

        private async Task CreateRequestQueue(ManagementClient mc)
        {
            var description = new QueueDescription(qRequest)
            {
                DefaultMessageTimeToLive = TimeSpan.FromMinutes(20),
                EnablePartitioning = true,
                RequiresDuplicateDetection = false,
                RequiresSession = false,
            };

            await mc.CreateQueueAsync(description);
        }

        private async Task CreateResponseQueue(ManagementClient mc)
        {
            var description = new QueueDescription(qResponse)
            {
                DefaultMessageTimeToLive = TimeSpan.FromMinutes(20),
                EnablePartitioning = true,
                RequiresDuplicateDetection = false,
                RequiresSession = true,
            };

            await mc.CreateQueueAsync(description);
        }
    }
}
