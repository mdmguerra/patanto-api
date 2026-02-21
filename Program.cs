using PatentsViewAPI.Extensions;
using PatentsViewAPI.Middleware;
using OpenAI;
using OpenAI.Chat;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Grok (xAI)
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();

    var apiKey = builder.Configuration["GrokSettings:ApiKey"]
        ?? throw new InvalidOperationException("GrokSettings:ApiKey no está configurada en appsettings.json");

    logger.LogInformation("Configurando cliente Grok con endpoint: https://api.x.ai/v1");

    var client = new OpenAI.OpenAIClient(
    new System.ClientModel.ApiKeyCredential(apiKey),
    new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri("https://api.groq.com/openai/v1")  
    });

    var chatClient = client.GetChatClient("llama-3.3-70b-versatile");  

    logger.LogInformation("Cliente Grok configurado exitosamente con grok-2-1212");

    return chatClient;
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PatentsView API",
        Version = "v1",
        Description = "API for analyzing patents using PatentsView API"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add PatentsView services
builder.Services.AddPatentsViewServices(builder.Configuration);

// Add Grok Analysis service
builder.Services.AddScoped<PatentsViewAPI.Services.GrokAnalysisService>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Global exception handling middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PatentsView API v1");
    options.RoutePrefix = string.Empty;
});

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();