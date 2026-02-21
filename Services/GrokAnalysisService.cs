using OpenAI.Chat;
using PatentsViewAPI.Models.Grok;
using System.Net;

namespace PatentsViewAPI.Services
{
    /// <summary>
    /// Servicio para analizar similitudes entre patentes usando Grok (xAI)
    /// </summary>
    public class GrokAnalysisService
    {
        private readonly ChatClient _chatClient;
        private readonly ILogger<GrokAnalysisService> _logger;

        public GrokAnalysisService(ChatClient chatClient, ILogger<GrokAnalysisService> logger)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("GrokAnalysisService inicializado correctamente");
        }

        public async Task<string> TestMinimalRequestAsync()
        {
                var messages = new List<ChatMessage>
            {
                new UserChatMessage("Di hola")
            };
            var completion = await _chatClient.CompleteChatAsync(messages);
            return completion.Value.Content[0].Text;
        }

        /// <summary>
        /// Analiza la similitud entre una patente principal y múltiples patentes de comparación
        /// </summary>
        /// <param name="request">Solicitud con información de las patentes</param>
        /// <returns>Análisis de similitud para cada patente comparada</returns>
        public async Task<PatentSimilarityResponse> AnalyzePatentSimilarityAsync(PatentSimilarityRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Patent == null)
                throw new ArgumentException("La patente principal no puede ser nula", nameof(request.Patent));

            if (string.IsNullOrWhiteSpace(request.Patent.Title) && string.IsNullOrWhiteSpace(request.Patent.Abstract))
                throw new ArgumentException("La patente principal debe tener título o resumen", nameof(request.Patent));

            if (request.ComparisonPatents == null || !request.ComparisonPatents.Any())
                throw new ArgumentException("Debe proporcionar al menos una patente para comparar", nameof(request.ComparisonPatents));

            return await AnalyzePatentSimilarityInternalAsync(request);
        }

        /// <summary>
        /// Método interno para el análisis de similitud
        /// </summary>
        private async Task<PatentSimilarityResponse> AnalyzePatentSimilarityInternalAsync(PatentSimilarityRequest request)
        {
            var response = new PatentSimilarityResponse();

            foreach (var comparisonPatent in request.ComparisonPatents)
            {
                try
                {
                    var analysis = await AnalyzeSinglePatentSimilarityAsync(request.Patent, comparisonPatent);
                    response.SimilarityAnalysis.Add(analysis);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error analizando similitud para patente {PatentNumber}",
                        comparisonPatent.PatentNumber);

                    // Agregar análisis de error
                    response.SimilarityAnalysis.Add(new PatentSimilarityAnalysis
                    {
                        PatentNumber = comparisonPatent.PatentNumber,
                        SimilarityNote = "Error al analizar la similitud de esta patente.",
                        SimilarityScore = 0
                    });
                }
            }

            return response;
        }

        /// <summary>
        /// Analiza la similitud entre dos patentes específicas
        /// </summary>
        private async Task<PatentSimilarityAnalysis> AnalyzeSinglePatentSimilarityAsync(PatentInfo mainPatent, PatentInfo comparisonPatent)
        {
            var prompt = BuildSimilarityAnalysisPrompt(mainPatent, comparisonPatent);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("Eres un experto analista de patentes. Tu tarea es analizar la similitud entre dos patentes y proporcionar una evaluación concisa."),
                new UserChatMessage(prompt)
            };

            _logger.LogInformation("Enviando solicitud a Grok API con {MessageCount} mensajes", messages.Count);
            _logger.LogDebug("Prompt enviado: {Prompt}", prompt);

            ChatCompletion completion;
            string grokResponse;
            try
            {
                completion = await _chatClient.CompleteChatAsync(messages);
                grokResponse = completion.Content[0].Text;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP Error calling Grok API. Status: {StatusCode}, Message: {Message}",
                    httpEx.StatusCode, httpEx.Message);
                throw new HttpRequestException($"Error HTTP communicating with Grok API: {httpEx.StatusCode} - {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                string errorBody = "";
                if (ex is System.ClientModel.ClientResultException clientEx)
                    errorBody = clientEx.GetRawResponse()?.Content?.ToString() ?? "sin body";

                _logger.LogError(ex, "Error calling Grok API: {Message}. Type: {ExceptionType}. Body: {Body}",
                    ex.Message, ex.GetType().Name, errorBody);
                throw new HttpRequestException($"Error communicating with Grok API: {ex.Message}", ex);
            }

            // Extraer el score de similitud del response (esperamos que Grok incluya un porcentaje)
            var similarityScore = ExtractSimilarityScore(grokResponse);

            return new PatentSimilarityAnalysis
            {
                PatentNumber = comparisonPatent.PatentNumber,
                SimilarityNote = grokResponse.Trim(),
                SimilarityScore = similarityScore
            };
        }

        /// <summary>
        /// Construye el prompt para el análisis de similitud
        /// </summary>
        private string BuildSimilarityAnalysisPrompt(PatentInfo mainPatent, PatentInfo comparisonPatent)
        {
            return $@"
Analiza la similitud entre estas dos patentes y proporciona una evaluación en UNA SOLA FRASE concisa.

PATENTE PRINCIPAL:
- Número: {mainPatent.PatentNumber ?? "No especificado"}
- Título: {mainPatent.Title ?? "No especificado"}
- Resumen: {mainPatent.Abstract ?? "No especificado"}

PATENTE A COMPARAR:
- Número: {comparisonPatent.PatentNumber ?? "No especificado"}
- Título: {comparisonPatent.Title ?? "No especificado"}
- Resumen: {comparisonPatent.Abstract ?? "No especificado"}

INSTRUCCIONES:
1. Evalúa la similitud técnica, conceptual y de alcance entre ambas patentes.
2. Considera si las invenciones son similares en propósito, medios o resultado.
3. Proporciona tu evaluación en UNA SOLA FRASE concisa y clara.
4. Incluye un porcentaje estimado de similitud (0-100%) al final de la frase.

Ejemplo de respuesta: ""Esta patente muestra una similitud técnica del 75% con la principal debido a que ambas protegen mecanismos similares de procesamiento de datos biométricos.""

RESPUESTA (una sola frase):";
        }

        /// <summary>
        /// Analiza una patente encontrada contra la descripción del usuario (palabras clave + aplicación)
        /// </summary>
        public async Task<GrokPatentAnalysis> AnalyzePatentAgainstDescriptionAsync(
            string patentTitle,
            string patentAbstract,
            string patentId,
            List<string> keywords,
            string? application)
        {
            if (string.IsNullOrWhiteSpace(patentId))
                throw new ArgumentException("El ID de la patente no puede estar vacío", nameof(patentId));

            if (string.IsNullOrWhiteSpace(patentTitle) && string.IsNullOrWhiteSpace(patentAbstract))
                throw new ArgumentException("La patente debe tener título o resumen", nameof(patentTitle));

            if (keywords == null || !keywords.Any())
                throw new ArgumentException("Debe proporcionar al menos una palabra clave", nameof(keywords));

            return await AnalyzePatentAgainstDescriptionInternalAsync(patentTitle, patentAbstract, patentId, keywords, application);
        }

        /// <summary>
        /// Método interno para el análisis contra descripción
        /// </summary>
        private async Task<GrokPatentAnalysis> AnalyzePatentAgainstDescriptionInternalAsync(
            string patentTitle,
            string patentAbstract,
            string patentId,
            List<string> keywords,
            string? application)
        {
            try
            {
                var prompt = BuildDescriptionAnalysisPrompt(patentTitle, patentAbstract, patentId, keywords, application);

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(
                        "Eres un experto analista de propiedad intelectual y patentes. Responde TODO en español. " +
                        "Tu tarea es evaluar qué tan similar es una patente existente respecto a una idea o invención descrita por el usuario. " +
                        "Debes ser preciso, conciso y práctico en tu evaluación."),
                    new UserChatMessage(prompt)
                };

                _logger.LogInformation("Enviando solicitud a Grok API para análisis de descripción con {MessageCount} mensajes", messages.Count);
                _logger.LogDebug("Prompt enviado: {Prompt}", prompt);

                ChatCompletion completion;
                string grokResponse;
                try
                {
                    completion = await _chatClient.CompleteChatAsync(messages);
                    grokResponse = completion.Content[0].Text;
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError(httpEx, "HTTP Error calling Grok API. Status: {StatusCode}, Message: {Message}",
                        httpEx.StatusCode, httpEx.Message);
                    throw new HttpRequestException($"Error HTTP communicating with Grok API: {httpEx.StatusCode} - {httpEx.Message}", httpEx);
                }
                catch (Exception ex)
                {
                    string errorBody = "";
                    if (ex is System.ClientModel.ClientResultException clientEx)
                        errorBody = clientEx.GetRawResponse()?.Content?.ToString() ?? "sin body";

                    _logger.LogError(ex, "Error calling Grok API: {Message}. Type: {ExceptionType}. Body: {Body}",
                        ex.Message, ex.GetType().Name, errorBody);
                    throw new HttpRequestException($"Error communicating with Grok API: {ex.Message}", ex);
                }

                var similarityScore = ExtractSimilarityScore(grokResponse);
                var riskLevel = similarityScore switch
                {
                    >= 80 => "ALTO",
                    >= 50 => "MEDIO",
                    >= 25 => "BAJO",
                    _ => "MUY BAJO"
                };

                return new GrokPatentAnalysis
                {
                    SimilarityScore = similarityScore,
                    SimilarityNote = grokResponse.Trim(),
                    RiskLevel = riskLevel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analizando patente {PatentId} contra descripción del usuario", patentId);

                return new GrokPatentAnalysis
                {
                    SimilarityScore = 0,
                    SimilarityNote = "Error al analizar esta patente con IA.",
                    RiskLevel = "DESCONOCIDO"
                };
            }
        }

        private string BuildDescriptionAnalysisPrompt(
            string patentTitle,
            string patentAbstract,
            string patentId,
            List<string> keywords,
            string? application)
        {
            var keywordsText = string.Join(", ", keywords);
            var applicationText = string.IsNullOrWhiteSpace(application) ? "No especificada" : application;

            return $@"
Analiza la siguiente patente existente y determina qué tan similar es a la idea/invención descrita por el usuario.

IDEA DEL USUARIO:
- Palabras clave: {keywordsText}
- Aplicación/campo: {applicationText}

PATENTE ENCONTRADA:
- ID: {patentId}
- Título: {patentTitle}
- Resumen: {patentAbstract}

INSTRUCCIONES:
1. Evalúa la similitud técnica y conceptual entre la idea del usuario y la patente encontrada.
2. Determina si la patente podría representar un conflicto de propiedad intelectual.
3. Proporciona tu evaluación en UNA SOLA FRASE concisa.
4. Incluye un porcentaje estimado de similitud (0-100%) al final de la frase.
5. El contenido de los siguientes campos debe ser traducido al español: patentTitle, patentAbstract.

Ejemplo: ""Esta patente presenta una similitud del 65% con la idea del usuario ya que ambas abordan mecanismos de filtrado de agua mediante membranas cerámicas, aunque difieren en el método de fabricación.""

RESPUESTA (una sola frase):";
        }

        /// <summary>
        /// Extrae el porcentaje de similitud del texto de respuesta de Grok
        /// </summary>
        private int ExtractSimilarityScore(string grokResponse)
        {
            try
            {
                // Buscar un porcentaje en el texto (formato: XX% o XX %)
                var regex = new System.Text.RegularExpressions.Regex(@"(\d{1,3})%");
                var match = regex.Match(grokResponse);

                if (match.Success && int.TryParse(match.Groups[1].Value, out var score))
                {
                    return Math.Clamp(score, 0, 100);
                }

                // Si no se encuentra porcentaje, intentar estimar basado en palabras clave
                var lowerResponse = grokResponse.ToLowerInvariant();
                if (lowerResponse.Contains("muy similar") || lowerResponse.Contains("alta similitud"))
                    return 80;
                if (lowerResponse.Contains("similar") || lowerResponse.Contains("moderada"))
                    return 60;
                if (lowerResponse.Contains("baja similitud") || lowerResponse.Contains("poco similar"))
                    return 30;
                if (lowerResponse.Contains("muy diferente") || lowerResponse.Contains("distinta"))
                    return 10;

                // Valor por defecto
                return 50;
            }
            catch
            {
                return 50; // Valor por defecto en caso de error
            }
        }
    }
}