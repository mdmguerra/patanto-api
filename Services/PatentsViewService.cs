using Microsoft.Extensions.Options;

using PatentsViewAPI.Configuration;
using PatentsViewAPI.Models.PatentsView;
using PatentsViewAPI.Models.Request;
using PatentsViewAPI.Services.Interfaces;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PatentsViewAPI.Services
{
    public class PatentsViewService : IPatentsViewService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private static readonly JsonSerializerOptions JsonOptionsIgnoreNull = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<PatentsViewService> _logger;
        private readonly PatentsViewOptions _options;

        public PatentsViewService(
            HttpClient httpClient,
            IOptions<PatentsViewOptions> options,
            ILogger<PatentsViewService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }

        public async Task<string> AnalizePatentsAsync(
            AnalizeRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Starting patent analysis for keywords: {Keywords}, Application: {Application}",
                string.Join(", ", request.PalabrasClaves),
                request.Aplicacion ?? "N/A");

            var query = BuildQuery(request);

            var apiRequest = new PatentsViewApiRequest
            {
                Q = query,
                F = new List<string>
                {
                    "patent_id",
                    "patent_title",
                    "patent_abstract",
                    "patent_date",
                },
                O = new QueryOptions
                {
                    Size = 5
                }
            };

            var jsonContent = JsonSerializer.Serialize(apiRequest, JsonOptions);
            return await ExecutePostAsync(jsonContent, "patent analysis", cancellationToken);
        }


        public async Task<string> SearchDirectAsync(
            DirectSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting direct patent search with custom parameters");

            var jsonContent = JsonSerializer.Serialize(request, JsonOptionsIgnoreNull);
            return await ExecutePostAsync(jsonContent, "direct patent search", cancellationToken);
        }

        /// <summary>
        /// Ejecuta el POST a la API de PatentsView y maneja la respuesta/errores.
        /// </summary>
        private async Task<string> ExecutePostAsync(
            string jsonContent,
            string operationContext,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("PatentsView API Request: {Request}", jsonContent);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "PatentsView API returned error. Status: {StatusCode}, Content: {Content}",
                        response.StatusCode,
                        errorContent);

                    throw new HttpRequestException(
                        $"PatentsView API request failed with status {response.StatusCode}: {errorContent}");
                }

                // Agregar  manejo de IA para analizar el contenido de las patentes
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogInformation("Successfully received response from PatentsView API");

                return responseContent;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request to PatentsView API timed out");
                throw new TimeoutException("The request to PatentsView API timed out", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while calling PatentsView API");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during {Operation}", operationContext);
                throw;
            }
        }

        /// <summary>
        /// Construye la query con _text_all para cada palabra clave.
        /// Si existe Aplicacion, se agrega como una palabra clave más.
        /// </summary>
        private object BuildQuery(AnalizeRequest request)
        {
            var allKeywords = new List<string>(request.PalabrasClaves
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim()));

            // Agregar la aplicación como palabra clave si existe
            if (!string.IsNullOrWhiteSpace(request.Aplicacion))
            {
                allKeywords.Add(request.Aplicacion.Trim());
            }

            // Crear condiciones _text_all para cada keyword
            var textAllConditions = allKeywords
                .Select(keyword => new TextAllQuery
                {
                    TextAll = new Dictionary<string, string>
                    {
                        { "patent_abstract", keyword }
                    }
                })
                .Cast<object>()
                .ToList();

            return new AndQuery
            {
                And = textAllConditions
            };
        }
    }
}
