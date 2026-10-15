using Api.Atendimento.Clients;
using Api.Atendimento.Configuration;
using Api.Atendimento.Custos;
using Api.Atendimento.Historico;
using Api.Atendimento.Infrastructure;
using Api.Atendimento.Options;
using Api.Atendimento.Outbox;
using Api.Atendimento.Services;
using Api.Atendimento.SuporteHumano;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AtendimentoExceptionHandler>();
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
    .AddOptions<GeminiOptions>()
    .Bind(builder.Configuration.GetSection(GeminiOptions.SectionName));

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
builder.Services.AddSingleton<IOutboxMessageRepository, SqlOutboxMessageRepository>();

builder.Services
    .AddOptions<OutboxRelayOptions>()
    .Bind(builder.Configuration.GetSection(OutboxRelayOptions.SectionName));

builder.Services.AddSingleton<IOutboxAtendimentoPublisher, RabbitMqOutboxAtendimentoPublisher>();
builder.Services.AddHostedService<OutboxRelayBackgroundService>();
builder.Services.AddSingleton<EncaminhamentoSuportePlugin>();
builder.Services.AddSingleton<IEncaminhamentoSuporteHumanoService, EncaminhamentoSuporteHumanoService>();

builder.Services.AddSingleton<IOpenAiChaveGestor, OpenAiChaveGestor>();
builder.Services.AddSingleton<IGeminiChaveGestor, GeminiChaveGestor>();
builder.Services.AddSingleton<IOpenAiChatClientFactory, OpenAiChatClientFactory>();
builder.Services.AddSingleton<IOpenAiRespostaGerador, OpenAiRespostaGerador>();
builder.Services.AddSingleton<IGeminiFallbackGerador, GeminiFallbackGerador>();
builder.Services.AddSingleton<IConhecimentoMcpClient, ConhecimentoMcpClient>();
builder.Services.AddSingleton<IAtendimentoService, OpenAiAtendimentoService>();

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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api.Atendimento v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
