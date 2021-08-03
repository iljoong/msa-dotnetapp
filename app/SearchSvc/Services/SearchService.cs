using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace SearchSvc
{
    public class SearchService : SearchEngine.SearchEngineBase
    {
        private readonly ILogger<SearchService> _logger;
        private readonly Random _random;

        private readonly List<SResult> _schresult;
        public SearchService(ILogger<SearchService> logger)
        {
            _logger = logger;
            _random = new Random();
            _schresult = new List<SResult>();

            Init();
        }

        public override Task<SResult> Search(SRequest request, ServerCallContext context)
        {
            int rnd = _random.Next(1, 10);

            //_logger.LogInformation($"*********** rnd:{rnd}, query: {request.Query}");            
            int delaytime = 0;
            if (request.Delay)
            {
                delaytime = 50 + rnd * 5;
                Thread.Sleep(delaytime); //Task.Delay(delay).GetAwaiter().GetResult();
            }

            if (rnd == 0 && request.Fault)
            {
                _logger.LogInformation($"*********** random error");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal Error: {request.Query}"));
            }

            if (context.CancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"*********** Cancelled");
                throw new RpcException(new Status(StatusCode.Cancelled, "Canceled by CancellationToken"));
            }

            return Task.FromResult(GetSearchResult(request.Type, request.Query, delaytime));
        }

        private SResult GetSearchResult(string type, string query, int delay)
        {
            SResult result;
            switch (type)
            {
                case "web":
                    result = _schresult[0];
                    break;
                case "images":
                    result = _schresult[1];
                    break;
                case "vidoes":
                    result = _schresult[2];
                    break;
                default:
                    result = _schresult[0];
                    break;
            }
            result.Time = delay;
            result.Log = $"result for \"{query}\" from backend, process time {delay} msec";
            return result;
        }

        private void Init()
        {
            _schresult.Add(
                new SResult
                {
                    Title = "web",
                    Url = "https://dotnet.microsoft.com/",
                    Snippet = "Free. Cross-platform. Open source."
                });
            _schresult.Add(
                new SResult
                {
                    Title = "images",
                    Url = "https://en.wikipedia.org/wiki/.NET_Core#/media/File:.NET_Core_Logo.svg",
                    Snippet = ".NET Core Logo."
                });
            _schresult.Add(
                new SResult
                {
                    Title = "vidoes",
                    Url = "https://www.youtube.com/channel/UCvtT19MZW8dq5Wwfu6B0oxw",
                    Snippet = "dotNET channel"
                });
        }
    }
}
