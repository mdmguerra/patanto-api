using Microsoft.AspNetCore.Mvc;
using PatentsViewAPI.Models.Request;
using PatentsViewAPI.Models.Grok;
using PatentsViewAPI.Services.Interfaces;
using PatentsViewAPI.Services;
using System.Text.Json;

namespace PatentsViewAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalizeController : ControllerBase
    {
        private readonly IPatentsViewService _patentsViewService;
        private readonly GrokAnalysisService _grokAnalysisService;
        private readonly ILogger<AnalizeController> _logger;

        public AnalizeController(
            IPatentsViewService patentsViewService,
            GrokAnalysisService grokAnalysisService,
            ILogger<AnalizeController> logger)
        {
            _patentsViewService = patentsViewService;
            _grokAnalysisService = grokAnalysisService;
            _logger = logger;
        }

        /// <summary>
        /// Analyzes patents based on keywords and optional application
        /// </summary>
        /// <param name="request">Request containing keywords and optional application</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Patents matching the search criteria from PatentsView API</returns>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
        public async Task<IActionResult> Post(
            [FromBody] AnalizeRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            _logger.LogInformation(
                "Analize request received with {KeywordCount} keywords",
                request.PalabrasClaves.Count);

            try
            {
                var result = await _patentsViewService.AnalizePatentsAsync(request, cancellationToken);

                var jsonDocument = JsonDocument.Parse(result);
                var root = jsonDocument.RootElement;

                if (!root.TryGetProperty("patents", out var patentsElement))
                {
                    return Ok(new PatentAnalysisResponse
                    {
                        TotalPatents = 0,
                        SearchKeywords = request.PalabrasClaves,
                        Application = request.Aplicacion
                    });
                }

                var patents = patentsElement.EnumerateArray().ToList();
                _logger.LogInformation("Found {Count} patents, starting Grok AI analysis", patents.Count);

                var analysisResponse = new PatentAnalysisResponse
                {
                    TotalPatents = patents.Count,
                    SearchKeywords = request.PalabrasClaves,
                    Application = request.Aplicacion
                };

                var analysisTasks = patents.Select(async patent =>
                {
                    var patentId = patent.TryGetProperty("patent_id", out var id) ? id.GetString() ?? "" : "";
                    var patentTitle = patent.TryGetProperty("patent_title", out var title) ? title.GetString() ?? "" : "";
                    var patentAbstract = patent.TryGetProperty("patent_abstract", out var abs) ? abs.GetString() ?? "" : "";
                    var patentDate = patent.TryGetProperty("patent_date", out var date) ? date.GetString() ?? "" : "";

                    var analysis = await _grokAnalysisService.AnalyzePatentAgainstDescriptionAsync(
                        patentTitle,
                        patentAbstract,
                        patentId,
                        request.PalabrasClaves,
                        request.Aplicacion);

                    return new PatentWithAnalysis
                    {
                        PatentId = patentId,
                        PatentTitle = patentTitle,
                        PatentAbstract = patentAbstract,
                        PatentDate = patentDate,
                        Analysis = analysis
                    };
                });

                var analyzedPatents = await Task.WhenAll(analysisTasks);
                analysisResponse.Patents = analyzedPatents
                    .OrderByDescending(p => p.Analysis.SimilarityScore)
                    .ToList();

                _logger.LogInformation("Completed Grok AI analysis for {Count} patents", analyzedPatents.Length);

                return Ok(analysisResponse);
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout occurred during patent analysis");
                return StatusCode(StatusCodes.Status504GatewayTimeout, new ProblemDetails
                {
                    Title = "Gateway Timeout",
                    Detail = "The request to PatentsView API timed out",
                    Status = StatusCodes.Status504GatewayTimeout
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during patent analysis");
                return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
                {
                    Title = "Bad Gateway",
                    Detail = "Error communicating with PatentsView API",
                    Status = StatusCodes.Status502BadGateway
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during patent analysis");
                throw; // Let global exception handler deal with it
            }
        }

        /// <summary>
        /// Analiza la similitud entre patentes usando Grok (xAI)
        /// </summary>
        /// <param name="request">Solicitud con información de las patentes a comparar</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Análisis de similitud entre las patentes</returns>
        [HttpPost("similarity")]
        [ProducesResponseType(typeof(PatentSimilarityResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AnalyzeSimilarity(
            [FromBody] PatentSimilarityRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid similarity analysis request model: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            _logger.LogInformation(
                "Similarity analysis request received for patent {MainPatent} with {ComparisonCount} patents to compare",
                request.Patent?.PatentNumber,
                request.ComparisonPatents?.Count ?? 0);

            try
            {
                var result = await _grokAnalysisService.AnalyzePatentSimilarityAsync(request);

                _logger.LogInformation("Successfully completed similarity analysis for {AnalysisCount} patent comparisons",
                    result.SimilarityAnalysis.Count);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during patent similarity analysis");
                throw; // Let global exception handler deal with it
            }
        }
    }
}
