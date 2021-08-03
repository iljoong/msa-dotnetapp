/*
 EH Limit: https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/azure-subscription-service-limits#event-hubs-basic-and-standard---quotas-and-limits
 V2 Sample: https://docs.microsoft.com/en-us/azure/event-hubs/get-started-dotnet-standard-send-v2
 Ref: https://stackoverflow.com/questions/36720702/how-to-use-client-side-event-batching-functionality-while-sending-to-microsoft-a
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using System.Collections.Concurrent;

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Threading;

namespace apiapp
{
    // Implemented Singleton EventHubClient
    public interface IEHService
    {
        void SendMessageSync(string message);
        void SendMessageAsync(string message);

        // for debug
        int GetBufferSize();
    }

    public class EHService : IEHService
    {

        private readonly string _ehConn;
        private readonly string _ehName;
        private readonly bool _use_eh;
        private readonly ILogger<EhsManager> _logger;

        private readonly EventHubProducerClient producerClient;
        //private readonly List<string> _buffer = new List<string>();
        private readonly ConcurrentQueue<string> _buffer = new ConcurrentQueue<string>();
        public EHService(ILogger<EhsManager> logger, IConfiguration config)
        {
            _ehConn = config["eh:eventhubconn"];
            _ehName = config["eh:eventhubname"];
            _ehConn = System.Environment.GetEnvironmentVariable("EVENTHUB_CONN") ?? _ehConn;
            _ehName = System.Environment.GetEnvironmentVariable("EVENTHUB_NAME") ?? _ehName;
            _use_eh = (System.Environment.GetEnvironmentVariable("USE_EH") ?? "false").ToLower() == "true";
            _logger = logger;

            _logger.LogInformation($">>>>>eh:eventhubconn {_ehConn}, {_use_eh}");

            if (_use_eh)
            {
                producerClient = new EventHubProducerClient(_ehConn, _ehName);
                StartSend();
            }
        }

        ~EHService()
        {
            // todo: make sure everything is flushed
        }
        public void StartSend()
        {
            _logger.LogInformation($"--auto flush started: {DateTime.Now}");

            //https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task?view=netcore-3.1
            //https://stackoverflow.com/questions/39071826/how-to-call-async-method-within-task-run
            Task.Run(async () =>
            {
                while (true)
                {
                    //https://stackoverflow.com/questions/20082221/when-to-use-task-delay-when-to-use-thread-sleep
                    //use Thread.Sleep in sync code, use Task.Delay in async code.
                    //await Task.Delay(10);
                    Thread.Sleep(100);

                    //_logger.LogInformation($"--auto flush: {DateTime.Now}");
                    await FlushBuffer();
                }
            });
        }

        public async Task FlushBuffer()
        {
            if (_buffer.Count > 0)
            {
                using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

                string msg;
                string allmsg = "";
                int count = 0;
                while (_buffer.TryDequeue(out msg))
                {
                    allmsg += $"{msg}\n";
                    //eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(msg)));
                    if (count++ >= 100)
                        break;
                }

                if (count != 0)
                {
                    eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(allmsg)));
                    //_logger.LogInformation($"-----send: {eventBatch.SizeInBytes} B / {eventBatch.MaximumSizeInBytes} B");
                    _logger.LogInformation($"-----flushed:{count-1}");
                    producerClient.SendAsync(eventBatch).Wait();
                }
            }
        }

        public void SendMessageAsync(string message)
        {
            // async sendmessage
            if (_use_eh)
                _buffer.Enqueue(message);
        }

        // async void - fire-and-forget
        public async void SendMessageSync(string message)
        {
            if (_use_eh)
            {
                //_logger.LogInformation($"-----sending:{message}");

                // Create a batch of events 
                using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

                // Add events to the batch. An event is a represented by a collection of bytes and metadata. 
                eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(message)));

                // Use the producer client to send the batch of events to the event hub
                await producerClient.SendAsync(eventBatch);
            }
        }

        public int GetBufferSize()
        {
            return _buffer.Count;
        }
    }

}