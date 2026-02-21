using System.Text.Json.Serialization;

namespace PatentsViewAPI.Models.Grok
{
    public class PatentAnalysisResponse
    {
        [JsonPropertyName("total_patents")]
        public int TotalPatents { get; set; }

        [JsonPropertyName("search_keywords")]
        public List<string> SearchKeywords { get; set; } = new();

        [JsonPropertyName("application")]
        public string? Application { get; set; }

        [JsonPropertyName("patents")]
        public List<PatentWithAnalysis> Patents { get; set; } = new();
    }

    public class PatentWithAnalysis
    {
        [JsonPropertyName("patent_id")]
        public string? PatentId { get; set; }

        [JsonPropertyName("patent_title")]
        public string? PatentTitle { get; set; }

        [JsonPropertyName("patent_abstract")]
        public string? PatentAbstract { get; set; }

        [JsonPropertyName("patent_date")]
        public string? PatentDate { get; set; }

        [JsonPropertyName("analysis")]
        public GrokPatentAnalysis Analysis { get; set; } = new();
    }

    public class GrokPatentAnalysis
    {
        [JsonPropertyName("similarity_score")]
        public int SimilarityScore { get; set; }

        [JsonPropertyName("similarity_note")]
        public string? SimilarityNote { get; set; }

        [JsonPropertyName("risk_level")]
        public string? RiskLevel { get; set; }
    }
}
