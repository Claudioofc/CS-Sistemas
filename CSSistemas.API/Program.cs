using CSSistemas.API.Extensions;
using CSSistemas.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Railway e outros clouds injetam PORT; Kestrel deve escutar nela.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port) && int.TryParse(port, out var portNum))
    builder.WebHost.UseUrls($"http://0.0.0.0:{portNum}");

// Configurações por contexto (extensões)
builder.Services.AddSwaggerConfig();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApiAuthorization();
builder.Services.AddApiValidators();
builder.Services.AddApiCors();
builder.Services.AddApiRateLimiting();
builder.Services.AddApiControllers();

var app = builder.Build();

await app.EnsureDatabaseAndSeedAsync();
app.UseApiPipeline();

app.Run();
