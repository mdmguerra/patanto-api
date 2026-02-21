using Microsoft.AspNetCore.Mvc;
using PatentsViewAPI.Models.Request;
using PatentsViewAPI.Services.Interfaces;
using System.Text.Json;

namespace PatentsViewAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IPatentsViewService _patentsViewService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(
            IPatentsViewService patentsViewService,
            ILogger<SearchController> logger)
        {
            _patentsViewService = patentsViewService;
            _logger = logger;
        }

       
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
        public async Task<IActionResult> Post(
            [FromBody] DirectSearchRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request model: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Direct search request received");

            try
            {
                var result = await _patentsViewService.SearchDirectAsync(request, cancellationToken);

                // Parse the JSON response to return as object
                var jsonDocument = JsonDocument.Parse(result);

                return Ok(jsonDocument.RootElement);
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout occurred during direct search");
                return StatusCode(StatusCodes.Status504GatewayTimeout, new ProblemDetails
                {
                    Title = "Gateway Timeout",
                    Detail = "The request to PatentsView API timed out",
                    Status = StatusCodes.Status504GatewayTimeout
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during direct search");
                return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
                {
                    Title = "Bad Gateway",
                    Detail = "Error communicating with PatentsView API",
                    Status = StatusCodes.Status502BadGateway
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during direct search");
                throw; // Let global exception handler deal with it
            }
        }
    }
}
