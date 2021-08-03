using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace apiapp.Models
{

    [Serializable()]
    public class SearchResult
    {
        public IList<httpResult> results { get; set; }
        public int total_time { get; set; }

        public SearchResult()
        {
            results = new List<httpResult>();
        }

    }

    [Serializable()]
    public class httpResult
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("snippet")]
        public string Snippet { get; set; }
        [JsonPropertyName("log")]
        public string Log { get; set; }
        [JsonPropertyName("time")]
        public int Time { get; set; }

    }

}