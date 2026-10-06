using Api.Conhecimento.Options;
using Api.Conhecimento.Rag;
using Api.Conhecimento.Tools;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using OpenAI.Embeddings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
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
    var apiKey = string.IsNullOrWhiteSpace(options.ApiKey)
        ? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        : options.ApiKey;

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException(
            "Configure a chave da OpenAI em OpenAI:ApiKey (User Secrets) ou na variável OPENAI_API_KEY.");
    }

    return new EmbeddingClient(options.EmbeddingModel, apiKey);
});

builder.Services.AddSingleton<IMarkdownChunker, MarkdownChunker>();
builder.Services.AddSingleton<IDocumentLoader, MarkdownDocumentLoader>();
builder.Services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();
builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();
builder.Services.AddSingleton<IConhecimentoSearch, ConhecimentoSearchService>();
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
