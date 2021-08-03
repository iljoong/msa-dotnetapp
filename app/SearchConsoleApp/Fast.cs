using System;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Net.Client;
using System.Threading;
using Grpc.Core;
using System.Collections.Generic;

namespace SearchSvc
{
    class Fast
    {
        static SearchEngine.SearchEngineClient webclient, imgclient, vidclient;

        static public void Run()
        {
            using var webchannel = GrpcChannel.ForAddress("https://localhost:5001");
            webclient = new SearchEngine.SearchEngineClient(webchannel);

            using var imgchannel = GrpcChannel.ForAddress("https://localhost:5002");
            imgclient = new SearchEngine.SearchEngineClient(imgchannel);

            using var vidchannel = GrpcChannel.ForAddress("https://localhost:5003");
            vidclient = new SearchEngine.SearchEngineClient(vidchannel);

            for (int i = 0; i < 5; i++)
                Test_Fastest();
        }

        static void Test_Fastest()
        {
            // Fan-out, takes only fastest task and cancels remaining request
            Console.WriteLine("========Fan-out and takes fastest");

            CancellationTokenSource source = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            CancellationToken token = source.Token;

            var tasks = new List<Task<SResult>>();
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    return webclient.Search(new SRequest { Query = "dotnet", Test = 0 }, cancellationToken: token);
                }
                catch (RpcException ex) //when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine($"Cancelled, web, {ex.Message}");
                    return null;
                }
            }));
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    return imgclient.Search(new SRequest { Query = "dotnet", Test = 0 }, cancellationToken: token);
                }
                catch (RpcException ex)// when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine($"Cancelled, img, {ex.Message}");
                    return null;
                }
            }));
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    return vidclient.Search(new SRequest { Query = "dotnet", Test = 0 }, cancellationToken: token);
                }
                catch (RpcException ex) //when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine($"Cancelled, vid, {ex.Message}");
                    return null;
                }
            }));

            //Task.WaitAny(tasks.ToArray());
            var r = Task.WhenAny(tasks.ToArray()).Result;
            Console.WriteLine($">>>>Task id_{r.Id}: {r.Status}:{(r.Result != null ? r.Result.Log : "N/A")}");

            source.Cancel(); // cancel any remaining task

            // can be more than one result (task finished at the same time)
            foreach (var t in tasks)
            {
                Console.WriteLine($"Task id_{t.Id}: {t.Status}");
                Console.WriteLine($"{(t.Result != null ? t.Result.Log : "N/A")}");
            }

        }
    }
}