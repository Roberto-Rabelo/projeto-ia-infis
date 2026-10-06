using Api.Atendimento.Clients;
using Api.Atendimento.Options;
using Api.Atendimento.Services;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
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
