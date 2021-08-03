using System;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Net.Client;
using System.Threading;
using Grpc.Core;
using System.Collections.Generic;

namespace SearchSvc
{
    class Good
    {
        static SearchEngine.SearchEngineClient webclient, imgclient, vidclient;

        static public void Run()
        {
            // The port number(5001) must match the port of the gRPC server.
            using var webchannel = GrpcChannel.ForAddress("https://localhost:5001");
            webclient = new SearchEngine.SearchEngineClient(webchannel);

            using var imgchannel = GrpcChannel.ForAddress("https://localhost:5002");
            imgclient = new SearchEngine.SearchEngineClient(imgchannel);

            using var vidchannel = GrpcChannel.ForAddress("https://localhost:5003");
            vidclient = new SearchEngine.SearchEngineClient(vidchannel);


            Test_1_Sync();

            Test_2_Parallel().Wait();

            Test_3_Deadline().Wait();
        } 

        static void Test_1_Sync()
        {
            DateTime start;
            // 1. sync call
            Console.WriteLine("=========Sync call");
            for (int i = 0; i < 5; i++)
            {
                start = DateTime.UtcNow;
                var res = searchSync();
                Console.WriteLine($"Time to process: {(DateTime.UtcNow - start).TotalMilliseconds} msec / total_time: {res[0].Time + res[1].Time} msec");
                Console.WriteLine($"Results: {res[0].Log}, {res[1].Log}");
            }
        }

        static async Task Test_2_Parallel()
        {
            DateTime start;
            // 2. async call
            Console.WriteLine("========Parallel call/Fan-out");
            for (int i = 0; i < 5; i++)
            {
                start = DateTime.UtcNow;
                var res = await searchAsync();
                Console.WriteLine($"Time to process: {(DateTime.UtcNow - start).TotalMilliseconds} msec / total_time: {res[0].Time + res[1].Time} msec");
                Console.WriteLine($"Results: {res[0].Log}, {res[1].Log}");
            }
        }

        static async Task Test_3_Deadline()
        {
            Console.WriteLine("========Deadline");
            // deadline
            for (int i = 0; i < 5; i++)
            {
                var res = await SeachWithDealine(webclient, 800);
                Console.WriteLine($"{res}");
            }
        }

        static async Task<List<SResult>> searchAsync(string query = "dotnet")
        {
            List<SResult> res = new List<SResult>();

            AsyncUnaryCall<SResult> res1 = webclient.SearchAsync(new SRequest { Query = query });
            AsyncUnaryCall<SResult> res2 = imgclient.SearchAsync(new SRequest { Query = query });

            res.Add(await res1);
            res.Add(await res2);
            /**/
            /*Parallel.Invoke(
                () => res.Add(webclient.Search( new Request { Query = "dotnet" } )),
                () => res.Add(imgclient.Search( new Request { Query = "dotnet" } ))
            );/**/

            return res;
        }

        static List<SResult> searchSync(string query = "dotnet")
        {
            List<SResult> res = new List<SResult>();

            res.Add(webclient.Search(new SRequest { Query = query }));
            res.Add(imgclient.Search(new SRequest { Query = query }));

            return res;
        }

        static async Task<string> SeachWithDealine(SearchEngine.SearchEngineClient client, int timeout)
        {
            try
            {
                var d = DateTime.UtcNow;
                SResult res = await client.SearchAsync(new SRequest { Query = "dotnet" }, deadline: d.AddMilliseconds(timeout));

                return res.Log;

            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                //Console.WriteLine($"Exception: {ex.Message}");
                return "DeadlineExceeded";
            }
        }

    }
}