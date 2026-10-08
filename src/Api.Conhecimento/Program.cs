using Api.Conhecimento.Ingest;
using Api.Conhecimento.Options;
using Api.Conhecimento.Rag;
using Api.Conhecimento.Tools;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Npgsql;
using OpenAI.Embeddings;
using Pgvector.Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2 * 1024 * 1024 + 64 * 1024;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Api.Conhecimento",
        Version = "v1",
        Description = "API de conhecimento corporativo."
    });
});

builder.Services
    .AddOptions<OpenAiOptions>()
    .Bind(builder.Configuration.GetSection(OpenAiOptions.SectionName));

builder.Services
    .AddOptions<RagOptions>()
    .Bind(builder.Configuration.GetSection(RagOptions.SectionName));

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("OpenAI");
    var apiKey = ResolverChaveOpenAi(options.ApiKey, logger);

    return new EmbeddingClient(options.EmbeddingModel, apiKey);
});

builder.Services.AddSingleton(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Rag")
        ?? throw new InvalidOperationException("ConnectionStrings:Rag não configurada.");

    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.UseVector();
    return dataSourceBuilder.Build();
});

builder.Services.AddSingleton<IMarkdownChunker, MarkdownChunker>();
builder.Services.AddSingleton<IDocumentLoader, MarkdownDocumentLoader>();
builder.Services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();
builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();
builder.Services.AddSingleton<IConhecimentoSearch, ConhecimentoSearchService>();
builder.Services.AddSingleton<IPdfTextExtractor, PdfSharpTextExtractor>();
builder.Services.AddSingleton<IChunkRepository, PostgresChunkRepository>();
builder.Services.AddSingleton<IIngestService, IngestService>();
builder.Services.AddHostedService<DocumentIngestionHostedService>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<ConhecimentoTools>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api.Conhecimento v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapMcp("/mcp");

app.Run();

static string ResolverChaveOpenAi(string? apiKeyConfigurada, ILogger logger)
{
    var apiKey = apiKeyConfigurada;
    var origem = "OpenAI:ApiKey (appsettings/user-secrets)";

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        origem = "OPENAI_API_KEY";
    }

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException(
            "Configure a chave da OpenAI em User Secrets (OpenAI:ApiKey) ou na variável OPENAI_API_KEY.");
    }

    var sufixo = apiKey.Length >= 4 ? apiKey[^4..] : "????";
    logger.LogInformation("Chave OpenAI carregada de {Origem}, sufixo {Sufixo}.", origem, sufixo);
    return apiKey;
}
