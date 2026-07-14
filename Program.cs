using Task_2_TranscriptAnalysis.Services;

// Program.cs — the application entry point. It builds the web app,
// registers all services for dependency injection (DI), and starts the server.

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1. Register framework services
// ---------------------------------------------------------------------------
builder.Services.AddControllers();          // enables [ApiController] classes in /Controllers
builder.Services.AddEndpointsApiExplorer(); // needed by Swagger to discover endpoints
builder.Services.AddSwaggerGen();           // Swagger UI for manual testing in the browser

// ---------------------------------------------------------------------------
// 2. Register OUR services (dependency injection)
//
// "Register" means: when a class asks for an interface in its constructor,
// ASP.NET Core knows which implementation to create and hand over.
//
// Lifetimes used here:
//   - Singleton: ONE instance for the whole app. Used for AzureLanguageService
//     because the Azure TextAnalyticsClient is thread-safe and expensive to create.
//   - Scoped: one instance per HTTP request. A safe default for the other services.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IAzureLanguageService, AzureLanguageService>(); // Member 1: Azure connection
builder.Services.AddScoped<ITranscriptAnalysisService, TranscriptAnalysisService>(); // Member 2: PII extraction
builder.Services.AddScoped<ISpeakerRoleService, SpeakerRoleService>();               // Member 3: speaker roles
builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
var app = builder.Build();

// ---------------------------------------------------------------------------
// 3. Configure the HTTP request pipeline (middleware)
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    // Swagger UI is available at /swagger while running in Development.
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); // redirect http:// requests to https://

app.MapControllers(); // route incoming requests to the controllers in /Controllers

app.Run(); // start listening for requests (blocks until the app shuts down)

/// <summary>
/// This empty partial class makes the auto-generated Program class public so
/// the integration tests (/Tests) can boot the app with
/// WebApplicationFactory&lt;Program&gt;. Do not remove it.
/// </summary>
public partial class Program { }
