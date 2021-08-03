using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Grpc.Net.Client;
using Grpc.Core;
using SearchSvc;

using apiapp.Models;

namespace apiapp.Controllers
{
    [Serializable()]
    public class SearchRpcResult
    {
        public IList<SResult> results { get; set; }
        public int total_time { get; set; }

        public SearchRpcResult()
        {
            results = new List<SResult>();
        }

    }

    [ApiController]
    [Route("api/[controller]")]
    public class WebRpcController : ControllerBase
    {
        private readonly ILogger<WebRpcController> _logger;
        SearchEngine.SearchEngineClient _grpcclient;

        private readonly string _endpoint;

        public WebRpcController(ILogger<WebRpcController> logger, SearchEngine.SearchEngineClient grpcclient, IConfiguration config)
        {
            _logger = logger;
            _grpcclient = grpcclient;
            _endpoint = config["rpc:endpoint"];
        }

        [HttpGet]
        public async Task<ActionResult<SearchRpcResult>> Get([FromQuery]string query = "dotnet",
            [FromQuery]bool delay = false,
            [FromQuery]bool fault = false)
        {

            DateTime start = DateTime.UtcNow;
            SearchRpcResult result = new SearchRpcResult();

            try
            {
                SResult res = await _grpcclient.SearchAsync(new SRequest { Query = query, Delay = delay, Fault = fault });

                result.results.Add(res);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Error: {ex.Message}");
            }

            result.total_time = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            return result;
        }

    }
}
