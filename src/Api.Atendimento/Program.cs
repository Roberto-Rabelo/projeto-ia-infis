using Api.Atendimento.Clients;
using Api.Atendimento.Custos;
using Api.Atendimento.Historico;
using Api.Atendimento.Options;
using Api.Atendimento.Services;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using MongoDB.Driver;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Api.Atendimento",
        Version = "v1",
        Description = "API orquestradora de atendimento corporativo."
    });
});

builder.Services
    .AddOptions<McpConhecimentoOptions>()
    .Bind(builder.Configuration.GetSection(McpConhecimentoOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _),
        "Mcp:Conhecimento:Endpoint deve ser uma URI absoluta.")
    .ValidateOnStart();

builder.Services
    .AddOptions<OpenAiOptions>()
    .Bind(builder.Configuration.GetSection(OpenAiOptions.SectionName));

builder.Services
    .AddOptions<MongoHistoricoOptions>()
    .Bind(builder.Configuration.GetSection(MongoHistoricoOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        "MongoHistorico:ConnectionString é obrigatória.")
    .ValidateOnStart();

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<MongoHistoricoOptions>>().Value;
    return new MongoClient(options.ConnectionString);
});

builder.Services.AddSingleton<IConversacaoHistoricoRepository, MongoConversacaoHistoricoRepository>();

builder.Services
    .AddOptions<SqlCustosOptions>()
    .Bind(builder.Configuration.GetSection(SqlCustosOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        "SqlCustos:ConnectionString é obrigatória.")
    .ValidateOnStart();

builder.Services.AddSingleton<IConsumoTokensRepository, SqlConsumoTokensRepository>();

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("OpenAI");
    var apiKey = ResolverChaveOpenAi(options.ApiKey, logger);

    return new ChatClient(options.ChatModel, apiKey);
});

builder.Services.AddSingleton<IConhecimentoMcpClient, ConhecimentoMcpClient>();
builder.Services.AddSingleton<IAtendimentoService, OpenAiAtendimentoService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api.Atendimento v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.MapControllers();

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
