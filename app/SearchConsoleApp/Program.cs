using System;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Net.Client;
using System.Threading;
using Grpc.Core;
using System.Collections.Generic;

namespace SearchSvc
{
    class Program
    {
        static void Main(string[] args)
        {
            //Good.Run();

            //Fast.Run();

            Best.Run();
        }

    }
}
