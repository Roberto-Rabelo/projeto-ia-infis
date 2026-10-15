using Api.Conhecimento.Configuration;
using Api.Conhecimento.Ingest;
using Api.Conhecimento.Options;
using Api.Conhecimento.Rag;
using Api.Conhecimento.Tools;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Npgsql;
using Pgvector.Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

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

builder.Services.AddSingleton<IOpenAiChaveGestor, OpenAiChaveGestor>();
builder.Services.AddSingleton<IOpenAiEmbeddingClientFactory, OpenAiEmbeddingClientFactory>();

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

{
    var gestor = app.Services.GetRequiredService<IOpenAiChaveGestor>();
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("OpenAI");
    gestor.ObterChaveEfetiva();
    var info = gestor.ObterInfo();
    logger.LogInformation(
        "Chave OpenAI carregada de {Origem}, sufixo {Sufixo}.",
        info.Origem,
        info.Sufixo);
}

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
