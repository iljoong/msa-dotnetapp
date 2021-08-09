using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using apiapp.Models;

namespace apiapp.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class WebController : ControllerBase
    {
        private const string _version = "v1";

        private readonly ILogger<WebController> _logger;
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _webendpoint;
        private readonly string _imgendpoint;
        private readonly string _videndpoint;
	    private readonly string _appinstkey;
        private readonly string _ehConn;
        private readonly string _appPort;

        public WebController(ILogger<WebController> logger, IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            _logger = logger;

	        _appinstkey = config["ApplicationInsights:InstrumentationKey"];
            _appinstkey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY") ?? _appinstkey;

            _ehConn = config["eh:eventhubconn"];
            _ehConn = System.Environment.GetEnvironmentVariable("EVENTHUB_CONN") ?? _ehConn;
            _appPort = System.Environment.GetEnvironmentVariable("APP_PORT") ?? "80";
            //string temp = config["http:endpoint"];
            // read from enviornment
            string temp = System.Environment.GetEnvironmentVariable("HTTP_ENDPOINT") ?? "http://localhost:5000/api/test";


            if (!temp.Contains(";"))
            {
                _webendpoint = temp;
            }
            else
            {
                string[] endpoints = temp.Split(";");
                _webendpoint = endpoints[0];
                _imgendpoint = endpoints[1];
                _videndpoint = endpoints[2];
            }
        }

        [HttpGet("/debug")]
        public string Debug()
        {
            bool _use_eh = (System.Environment.GetEnvironmentVariable("USE_EH") ?? "false").ToLower() == "true";

            return $"Debug: {_webendpoint},{_imgendpoint},{_videndpoint},{_appinstkey},{_ehConn},USE_EH={_use_eh}";
        }

        [HttpGet()]
        public string Get()
        {
            return $"Hello - {_version}";
        }

        [HttpGet("/")]
        public string GetPing()
        {
            return "pong";
        }

        [HttpGet("all")]
        public async Task<ActionResult<SearchResult>> GetAll(
            [FromQuery] bool retry = false,
            [FromQuery] bool delay = false, [FromQuery] bool fault = false)
        {
            DateTime start = DateTime.UtcNow;
            SearchResult result = new SearchResult();

            string clienttype = (retry) ? "retry" : "default";
            var client = _httpFactory.CreateClient(clienttype);
            string query = $"?delay={delay}&fault={fault}";

            result.results = new List<httpResult>();

            try
            {
                result.results.Add(await SearchAsync(client, $"http://localhost:{_appPort}/api/search/web" + query));
                result.results.Add(await SearchAsync(client, $"http://localhost:{_appPort}/api/search/images" + query));
                result.results.Add(await SearchAsync(client, $"http://localhost:{_appPort}/api/search/videos" + query));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{{\"message\": \"{ex.Message}\"}}");
            }

            result.total_time = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            return result;
        }

        [HttpGet("v1")]
        public async Task<string> GetBadSearch()
        {
            //https://www.stevejgordon.co.uk/httpclient-connection-pooling-in-dotnet-core
            /*var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = 50
            };

            httpClient = new HttpClient(socketsHandler);
            */

            // BAD Example
            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync(_webendpoint);

                return response;
            }
        }

        [HttpGet("v2")]
        public async Task<ActionResult<SearchResult>> GetGoodSearch(
            [FromQuery]bool delay = false, [FromQuery]bool fault = false,
            [FromQuery]bool log = false, [FromQuery]bool sync = true)
        {           
            DateTime start = DateTime.UtcNow;
            SearchResult result = new SearchResult();

            var client = _httpFactory.CreateClient("default");
            string query = $"?delay={delay}&fault={fault}&log={log}&sync={sync}";

            try
            {
                result.results.Add( await SearchAsync(client, _webendpoint + query) );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{{\"message\": \"{ex.Message}\"}}");
            }

            result.total_time = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            return result;
        }

        [HttpGet("v2/retry")]
        public async Task<ActionResult<SearchResult>> GetRetry(
            [FromQuery]bool delay = false, [FromQuery]bool fault = false,
            [FromQuery]bool log = false, [FromQuery]bool sync = true)
        {
            DateTime start = DateTime.UtcNow;
            SearchResult result = new SearchResult();

            var client = _httpFactory.CreateClient("retry");
            string query = $"?delay={delay}&fault={fault}&log={log}&sync={sync}";

            try
            {
                result.results.Add( await SearchAsync(client, _webendpoint + query) );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{{\"message\": \"{ex.Message}\"}}");
            }

            result.total_time = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            return result;
        }

        [HttpGet("seq")]
        [HttpGet("v3/seq")]
        public async Task<ActionResult<SearchResult>> GetSequentially(
            [FromQuery] bool retry = false,
            [FromQuery] bool delay = false, [FromQuery] bool fault = false)
        {
            DateTime start = DateTime.UtcNow;
            SearchResult result = new SearchResult();

            string clienttype = (retry) ? "retry" : "default";
            var client = _httpFactory.CreateClient(clienttype);
            string query = $"?delay={delay}&fault={fault}";

            result.results = new List<httpResult>();

            try
            {
                result.results.Add(await SearchAsync(client, _webendpoint + query));
                result.results.Add(await SearchAsync(client, _imgendpoint + query));
                result.results.Add(await SearchAsync(client, _videndpoint + query));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{{\"message\": \"{ex.Message}\"}}");
            }

            result.total_time = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            return result;
        }

        //https://www.dotnetcurry.com/dotnet/1360/concurrent-programming-dotnet-core
        [HttpGet("para")]
        [HttpGet("v3/para")]
        public ActionResult<SearchResult> GetParallelv2(
            [FromQuery] bool retry = false,
            [FromQuery] bool delay = false, [FromQuery] bool fault = false)
        {
            DateTime start = DateTime.UtcNow;
            SearchResult result = new SearchResult();

            string clienttype = (retry) ? "retry" : "default";
            var client = _httpFactory.CreateClient(clienttype);
            string query = $"?delay={delay}&fault={fault}";

            try
            {
                var tasks = new List<Task<httpResult>>();
                tasks.Add(Task.Run(async () =>
                    await SearchAsync(client, _webendpoint + query)
                ));
                tasks.Add(Task.Run(async () =>
                    await SearchAsync(client, _imgendpoint + query)
                ));
                tasks.Add(Task.Run(async () =>
                    await SearchAsync(client, _videndpoint + query)
                ));
                Task.WaitAll(tasks.ToArray());

                foreach (var t in tasks)
                {
                    result.results.Add((t.Result));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{{\"message\": \"{ex.Message}\"}}");
            }

            result.total_time = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            return result;
        }

        //https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/how-to-make-multiple-web-requests-in-parallel-by-using-async-and-await
        [HttpGet("v4/para")]
        public async Task<ActionResult<SearchResult>> GetParallelv3(
            [FromQuery] bool retry = false,
            [FromQuery] bool delay = false,
            [FromQuery] bool fault = false
        )
        {
            DateTime start = DateTime.UtcNow;
            SearchResult result = new SearchResult();

            string clienttype = (retry) ? "retry" : "default";
            var client = _httpFactory.CreateClient(clienttype);
            string query = $"?delay={delay}&fault={fault}";

            try
            {
                Task<httpResult> call1 = SearchAsync(client, _webendpoint + query);
                Task<httpResult> call2 = SearchAsync(client, _imgendpoint + query);
                Task<httpResult> call3 = SearchAsync(client, _videndpoint + query);

                result.results.Add(await call1);
                result.results.Add(await call2);
                result.results.Add(await call3);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{{\"message\": \"{ex.Message}\"}}");
            }

            result.total_time = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            return result;
        }

        async Task<httpResult> SearchAsync(HttpClient client, string url)
        {
            var response = await client.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException("Error during search");

            var result = JsonSerializer.Deserialize<httpResult>(await response.Content.ReadAsStringAsync());

            return result;
        }

        [HttpGet("v4/paradual")]
        public async Task<ActionResult<SearchResult>> GetParallelDual(
            [FromQuery] bool retry = false,
            [FromQuery] bool delay = false,
            [FromQuery] bool fault = false
        )
        {
            DateTime start = DateTime.UtcNow;
            SearchResult result = new SearchResult();

            string clienttype = (retry) ? "retry" : "default";
            var client = _httpFactory.CreateClient(clienttype);
            string query = $"?delay={delay}&fault={fault}";

            try
            {
                Task<httpResult> call1 = SearchDualAsync(client, _webendpoint + query, _webendpoint + query);
                Task<httpResult> call2 = SearchDualAsync(client, _imgendpoint + query, _imgendpoint + query);
                Task<httpResult> call3 = SearchDualAsync(client, _videndpoint + query, _videndpoint + query);

                result.results.Add(await call1);
                result.results.Add(await call2);
                result.results.Add(await call3);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{{\"message\": \"{ex.Message}\"}}");
            }

            result.total_time = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            return result;
        }

        async Task<httpResult> SearchDualAsync(HttpClient client, string url1, string url2)
        {
            var response = await client.GetAsync(url1);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException("Error during search");

            var tasks = new List<Task<HttpResponseMessage>>();
            tasks.Add(Task.Run(async () =>
            {
                return await client.GetAsync(url1);
            }));
            tasks.Add(Task.Run(async () =>
            {
                return await client.GetAsync(url2);
            }));

            var r = Task.WhenAny(tasks.ToArray()).Result;
            var result = JsonSerializer.Deserialize<httpResult>(await r.Result.Content.ReadAsStringAsync());

            return result;
        }
    }
}
