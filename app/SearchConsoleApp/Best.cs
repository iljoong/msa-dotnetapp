using System;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Net.Client;
using System.Threading;
using Grpc.Core;
using System.Collections.Generic;

using Polly;
using System.Linq;

namespace SearchSvc
{
    class Best
    {
        static SearchEngine.SearchEngineClient webclient, imgclient, vidclient;
        static SearchEngine.SearchEngineClient webclient2, imgclient2, vidclient2;

        static public void Run()
        {
            using var webchannel = GrpcChannel.ForAddress("https://localhost:5001");
            webclient = new SearchEngine.SearchEngineClient(webchannel);

            using var imgchannel = GrpcChannel.ForAddress("https://localhost:5002");
            imgclient = new SearchEngine.SearchEngineClient(imgchannel);

            using var vidchannel = GrpcChannel.ForAddress("https://localhost:5003");
            vidclient = new SearchEngine.SearchEngineClient(vidchannel);


            using var webchannel2 = GrpcChannel.ForAddress("https://localhost:6001");
            webclient2 = new SearchEngine.SearchEngineClient(webchannel2);

            using var imgchannel2 = GrpcChannel.ForAddress("https://localhost:6002");
            imgclient2 = new SearchEngine.SearchEngineClient(imgchannel2);

            using var vidchannel2 = GrpcChannel.ForAddress("https://localhost:6003");
            vidclient2 = new SearchEngine.SearchEngineClient(vidchannel2);


            for (int i = 0; i < 5; i++)
            {
                GetSearchResult();
            }
        }

        static public void Retry()
        {
            using var webchannel = GrpcChannel.ForAddress("https://localhost:5001");
            webclient = new SearchEngine.SearchEngineClient(webchannel);

            //http://taswar.zeytinsoft.com/retry-pattern-using-polly-in-c/
            //https://anthonygiretti.com/2020/03/31/grpc-asp-net-core-3-1-resiliency-with-polly/
            for (int i = 0; i < 10; i++)
            {
                var reponse = Policy
                .Handle<RpcException>()
                .Retry(3)
                .Execute(() => webclient.Search(new SRequest { Query = "dotnet - web1", Test = 1 }));

                Console.WriteLine($"{i} Retry: {reponse.Log}");
            }

            for (int i = 0; i < 10; i++)
            {
                var reponse = Policy
                .Handle<RpcException>()
                .WaitAndRetry(new[]
                            {
                                TimeSpan.FromSeconds(1),
                                TimeSpan.FromSeconds(3),
                                TimeSpan.FromSeconds(9)
                            }, (result, timeSpan, retryCount, context) =>
                            {
                                Console.WriteLine($"Request failed with {result.Message}. Retry count = {retryCount}. Waiting {timeSpan} before next retry. ");
                            })
                .Execute(() => webclient.Search(new SRequest { Query = "dotnet - web1", Test = 1 }));

                Console.WriteLine($"{i} Retry: {reponse.Log}");
            }
        }

        static public async Task<SResult> GetFastest(Func<SResult> fn1, Func<SResult> fn2)
        {
            var tasks = new List<Task<SResult>>();
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    return fn1();
                }
                catch (RpcException ex) //when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine($"----fn1{ex.Message}");

                    // TODO: instead of waiting other task complete the request, retry! no best way to gracefull end
                    Task.Delay(1000).Wait();
                    return null;
                }

            }));
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    return fn2();
                }
                catch (RpcException ex) //when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine($"----fn2{ex.Message}");
                    Task.Delay(1000).Wait();
                    return null;
                }
            }));

            var r = await Task.WhenAny(tasks.ToArray()).Result;

            return r;
        }

        static void GetSearchResult()
        {
            DateTime start = DateTime.UtcNow;

            var tasks = new List<Task<SResult>>();
            tasks.Add(Task.Run(() =>
            {
                return GetFastest(
                    () => webclient.Search(new SRequest { Query = "dotnet - web1", Test = 1 }),
                    () => webclient2.Search(new SRequest { Query = "dotnet - web2", Test = 1 })
                );
            }));

            tasks.Add(Task.Run(() =>
            {
                return GetFastest(
                    () => imgclient.Search(new SRequest { Query = "dotnet - img1", Test = 1 }),
                    () => imgclient2.Search(new SRequest { Query = "dotnet - img2", Test = 1 })
                );
            }));

            tasks.Add(Task.Run(() =>
            {
                return GetFastest(
                    () => vidclient.Search(new SRequest { Query = "dotnet - vid1", Test = 1 }),
                    () => vidclient2.Search(new SRequest { Query = "dotnet - vid2", Test = 1 })
                );
            }));

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine($"\nTime to process: {(DateTime.UtcNow - start).TotalMilliseconds} msec");

            foreach (var t in tasks)
            {
                Console.WriteLine($"Task id_{t.Id}: {t.Status}, succ: {t.IsCompletedSuccessfully}");
                Console.WriteLine($"    {(t.Result != null ? t.Result.Log : "N/A")}");
            }
        }
    }
}