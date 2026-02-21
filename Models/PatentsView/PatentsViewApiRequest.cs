using System.Text.Json.Serialization;

namespace PatentsViewAPI.Models.PatentsView
{
    public class PatentsViewApiRequest
    {
        /// <summary>
        /// Query criteria
        /// </summary>
        [JsonPropertyName("q")]
        public object Q { get; set; } = new();

        /// <summary>
        /// Fields to return
        /// </summary>
        [JsonPropertyName("f")]
        public List<string>? F { get; set; }



        /// <summary>
        /// Options (pagination, etc)
        /// </summary>
        [JsonPropertyName("o")]
        public QueryOptions? O { get; set; }
    }

    public class QueryOptions
    {
        [JsonPropertyName("size")]
        public int Size { get; set; } = 100;

       
    }

    public class TextAllQuery
    {
        [JsonPropertyName("_text_all")]
        public Dictionary<string, string> TextAll { get; set; } = new();
    }

    public class AndQuery
    {
        [JsonPropertyName("_or")]
        public List<object> Or { get; set; } = new();
    }
}
