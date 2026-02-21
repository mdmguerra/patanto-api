using System.Text.Json.Serialization;

namespace PatentsViewAPI.Models.Grok
{
    /// <summary>
    /// Solicitud para analizar similitud entre patentes
    /// </summary>
    public class PatentSimilarityRequest
    {
        /// <summary>
        /// Información de la patente principal a comparar
        /// </summary>
        [JsonPropertyName("patent")]
        public PatentInfo Patent { get; set; } = new();

        /// <summary>
        /// Lista de patentes a comparar con la principal
        /// </summary>
        [JsonPropertyName("comparison_patents")]
        public List<PatentInfo> ComparisonPatents { get; set; } = new();
    }

    /// <summary>
    /// Información básica de una patente
    /// </summary>
    public class PatentInfo
    {
        /// <summary>
        /// Número de patente
        /// </summary>
        [JsonPropertyName("patent_number")]
        public string? PatentNumber { get; set; }

        /// <summary>
        /// Título de la patente
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Resumen o descripción de la patente
        /// </summary>
        [JsonPropertyName("abstract")]
        public string? Abstract { get; set; }

      
    }
}