using System.Text.Json.Serialization;

namespace PatentsViewAPI.Models.Grok
{
    /// <summary>
    /// Respuesta del análisis de similitud entre patentes
    /// </summary>
    public class PatentSimilarityResponse
    {
        /// <summary>
        /// Lista de análisis de similitud para cada patente comparada
        /// </summary>
        [JsonPropertyName("similarity_analysis")]
        public List<PatentSimilarityAnalysis> SimilarityAnalysis { get; set; } = new();
    }

    /// <summary>
    /// Análisis de similitud para una patente específica
    /// </summary>
    public class PatentSimilarityAnalysis
    {
        /// <summary>
        /// Número de la patente comparada
        /// </summary>
        [JsonPropertyName("patent_number")]
        public string? PatentNumber { get; set; }

        /// <summary>
        /// Nota de una frase sobre la similitud con la patente principal
        /// </summary>
        [JsonPropertyName("similarity_note")]
        public string? SimilarityNote { get; set; }

        /// <summary>
        /// Nivel de similitud estimado (0-100)
        /// </summary>
        [JsonPropertyName("similarity_score")]
        public int SimilarityScore { get; set; }
    }
}