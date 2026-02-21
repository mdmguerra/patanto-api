using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PatentsViewAPI.Models.Request
{
    /// <summary>
    /// Direct search request allowing full control over PatentsView API query parameters
    /// </summary>
    public class DirectSearchRequest
    {
        /// <summary>
        /// Query criteria - JSON object with search conditions
        /// Example: { "_text_all": { "patent_abstract": "machine learning" } }
        /// </summary>
        [Required(ErrorMessage = "q (query) is required")]
        [JsonPropertyName("q")]
        public object Q { get; set; } = new();

        /// <summary>
        /// Fields to return - array of field names
        /// Example: ["patent_id", "patent_title", "patent_abstract"]
        /// </summary>
        [JsonPropertyName("f")]
        public List<string>? F { get; set; }

        /// <summary>
        /// Sort order - array of sort specifications
        /// Example: [{ "patent_date": "desc" }]
        /// </summary>
        [JsonPropertyName("s")]
        public List<object>? S { get; set; }

        /// <summary>
        /// Options - pagination and other options
        /// Example: { "size": 50, "after": "some_value" }
        /// </summary>
        [JsonPropertyName("o")]
        public Dictionary<string, object>? O { get; set; }
    }
}
