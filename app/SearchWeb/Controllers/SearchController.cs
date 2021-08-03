using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using apiapp.Models;

namespace apiapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        static private readonly httpResult[] _schresult = {
                new httpResult
                {
                    Title = "web",
                    Url = "https://dotnet.microsoft.com/",
                    Snippet = "Free. Cross-platform. Open source."
                },
                new httpResult
                {
                    Title = "images",
                    Url = "https://en.wikipedia.org/wiki/.NET_Core#/media/File:.NET_Core_Logo.svg",
                    Snippet = ".NET Core Logo."
                },
                new httpResult
                {
                    Title = "vidoes",
                    Url = "https://www.youtube.com/channel/UCvtT19MZW8dq5Wwfu6B0oxw",
                    Snippet = "dotNET channel"
                }
        };

        private readonly string _port;
        private readonly ILogger<SearchController> _logger;
        private readonly IEHService _ehservice;

        public SearchController(ILogger<SearchController> logger, IEHService ehservice)
        {
            _logger = logger;
            _ehservice = ehservice;
            _port = System.Environment.GetEnvironmentVariable("APP_PORT") ?? "80";
        }

        // curl -i "http://localhost:5000/api/search?type=videos&query=dotnetx&delay=false&fault=false"
        [HttpGet()]
        public ActionResult<httpResult> Get([FromQuery]string type = "web",
            [FromQuery]string query = "dotnet",
            [FromQuery]bool delay = false, [FromQuery]bool fault = false,
            [FromQuery]bool log = false, [FromQuery]bool sync = true)
        {
            httpResult hr = Search(type, query, delay, fault, log, sync);
            if (hr == null)
                return StatusCode(500, "Simulated Internal Error");

            return hr;
        }

        // curl -i "http://localhost:5000/api/search/web?query=dotnetx&delay=false&fault=false"
        [HttpGet("web")]
        public ActionResult<httpResult> GetWeb([FromQuery]string query = "dotnet",
            [FromQuery]bool delay = false, [FromQuery]bool fault = false,
            [FromQuery]bool log = false, [FromQuery]bool sync = true)
        {
            httpResult hr = Search("web", query, delay, fault, log, sync);
            if (hr == null)
                return StatusCode(500, "Simulated Internal Error");

            return hr;
        }

        [HttpGet("images")]
        public ActionResult<httpResult> GetImg([FromQuery]string query = "dotnet",
            [FromQuery]bool delay = false, [FromQuery]bool fault = false,
            [FromQuery]bool log = false, [FromQuery]bool sync = true)
        {
            httpResult hr = Search("images", query, delay, fault, log, sync);
            if (hr == null)
                return StatusCode(500, "Simulated Internal Error");

            return hr;
        }

        [HttpGet("videos")]
        public ActionResult<httpResult> GetVid([FromQuery]string query = "dotnet",
            [FromQuery]bool delay = false, [FromQuery]bool fault = false,
            [FromQuery]bool log = false, [FromQuery]bool sync = true)
        {
            httpResult hr = Search("videos", query, delay, fault, log, sync);
            if (hr == null)
                return StatusCode(500, "Simulated Internal Error");

            return hr;
        }

        public httpResult Search(string type = "web", string query = "dotnet", 
            bool delay = false, bool fault = false,
            bool log = false, bool sync = true)
        {
            Random rand = new Random();
            int r = rand.Next(10);

            int delaytime = 50 + r * 5; // [50 ~ 95]
            if (delay)
            {
                Thread.Sleep(delaytime);
                //Task.Delay(delaytime).Wait();
                //Task.Delay(delaytime).GetAwaiter().GetResult();
            }

            if (r == 0 && fault) // 10% error
                return null;

            httpResult result;

            switch (type)
            {
                case "web":
                    result = _schresult[0];
                    break;
                case "images":
                    result = _schresult[1];
                    break;
                case "videos":
                    result = _schresult[2];
                    break;
                default:
                    result = _schresult[0];
                    break;
            }

            string dt = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");

            result.Time = delay ? delaytime : 0;
            result.Log = $"{dt}, result for \"{query}\", from backend {_port}, process time {result.Time} msec";

            if (log)
            {
                if (sync)
                    _ehservice.SendMessageSync(result.Log);
                else
                    _ehservice.SendMessageAsync(result.Log);
            }

            return result;
        }

        [HttpGet("log")]
        public ActionResult<string> GetStatus()
        {
            int buffsize =  _ehservice.GetBufferSize();

            return StatusCode(200, $"buffer left: {buffsize}\n");
        }

    }
}

