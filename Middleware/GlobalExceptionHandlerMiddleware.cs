using System.Net;
using System.Text.Json;

namespace PatentsViewAPI.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var problemDetails = new ProblemDetails
            {
                Instance = context.Request.Path
            };

            switch (exception)
            {
                case ArgumentException:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "Bad Request";
                    problemDetails.Detail = exception.Message;
                    break;

                case TimeoutException:
                    problemDetails.Status = StatusCodes.Status504GatewayTimeout;
                    problemDetails.Title = "Gateway Timeout";
                    problemDetails.Detail = "The request timed out";
                    break;

                case HttpRequestException:
                    problemDetails.Status = StatusCodes.Status502BadGateway;
                    problemDetails.Title = "Bad Gateway";
                    problemDetails.Detail = "Error communicating with external service";
                    break;

                default:
                    problemDetails.Status = StatusCodes.Status500InternalServerError;
                    problemDetails.Title = "Internal Server Error";
                    problemDetails.Detail = _environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred";
                    break;
            }

            context.Response.StatusCode = problemDetails.Status.Value;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(problemDetails, options);
            await context.Response.WriteAsync(json);
        }

        private class ProblemDetails
        {
            public int? Status { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Detail { get; set; } = string.Empty;
            public string Instance { get; set; } = string.Empty;
        }
    }
}
