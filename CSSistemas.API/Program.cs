using CSSistemas.API.Extensions;
using CSSistemas.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
