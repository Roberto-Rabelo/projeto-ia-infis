using Api.Atendimento.Options;

namespace Api.Atendimento.Configuration;

public sealed class OpenAiChaveGestor : IOpenAiChaveGestor
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<OpenAiChaveGestor> _logger;
    private readonly object _lock = new();
    private string? _chaveSessao;

    public OpenAiChaveGestor(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IHostEnvironment hostEnvironment,
        ILogger<OpenAiChaveGestor> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public OpenAiChaveInfo ObterInfo(bool incluirChaveCompleta = false)
    {
        var (chave, origem) = ResolverSemExcecao();
        return CriarInfo(chave, origem, incluirChaveCompleta);
    }

    public string ObterChaveEfetiva()
    {
        var (chave, origem) = ResolverSemExcecao();
        if (string.IsNullOrWhiteSpace(chave))
        {
            throw new InvalidOperationException(
                "Configure a chave da OpenAI em appsettings.Local.json, User Secrets (OpenAI:ApiKey) ou OPENAI_API_KEY.");
        }

        var sufixo = chave.Length >= 4 ? chave[^4..] : "????";
        _logger.LogDebug("Chave OpenAI efetiva de {Origem}, sufixo {Sufixo}.", origem, sufixo);
        return chave;
    }

    public async Task AtualizarAsync(
        string apiKey,
        OpenAiChavePersistencia persistirEm,
        CancellationToken cancellationToken)
    {
        if (!_hostEnvironment.IsDevelopment())
        {
            throw new InvalidOperationException("Gestão de chave via API só está disponível em Development.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("A ApiKey não pode ser vazia.", nameof(apiKey));
        }

        var chaveNormalizada = apiKey.Trim();

        if (persistirEm is OpenAiChavePersistencia.Local or OpenAiChavePersistencia.Ambos)
        {
            await OpenAiChaveArquivoLocal.SalvarApiKeyAsync(
                _environment.ContentRootPath,
                chaveNormalizada,
                cancellationToken);
        }

        if (persistirEm is OpenAiChavePersistencia.UserSecrets or OpenAiChavePersistencia.Ambos)
        {
            await OpenAiUserSecretsStore.SalvarApiKeyAsync(chaveNormalizada, cancellationToken);
        }

        lock (_lock)
        {
            _chaveSessao = chaveNormalizada;
        }

        var info = ObterInfo();
        _logger.LogInformation(
            "Chave OpenAI atualizada via API ({Persistencia}). Sufixo {Sufixo}.",
            persistirEm,
            info.Sufixo);
    }

    private OpenAiChaveInfo CriarInfo(string? chave, string origem, bool incluirChaveCompleta)
    {
        var configurada = !string.IsNullOrWhiteSpace(chave);
        var sufixo = configurada && chave!.Length >= 4 ? chave[^4..] : string.Empty;
        var mascara = configurada ? Mascarar(chave!) : string.Empty;
        var caminhoSecrets = _hostEnvironment.IsDevelopment()
            ? OpenAiUserSecretsStore.ObterCaminhoArquivo()
            : null;

        return new OpenAiChaveInfo(
            configurada,
            origem,
            sufixo,
            mascara,
            GerenciavelPorApi: _hostEnvironment.IsDevelopment(),
            ApiKeyCompleta: incluirChaveCompleta && configurada ? chave : null,
            CaminhoUserSecrets: caminhoSecrets);
    }

    private (string? Chave, string Origem) ResolverSemExcecao()
    {
        lock (_lock)
        {
            if (!string.IsNullOrWhiteSpace(_chaveSessao))
            {
                return (_chaveSessao, "appsettings.Local.json (atualizada na sessão)");
            }
        }

        var daConfig = _configuration.GetSection(OpenAiOptions.SectionName)["ApiKey"];
        if (!string.IsNullOrWhiteSpace(daConfig))
        {
            var origem = OrigemConfiguracao();
            return (daConfig, origem);
        }

        var daEnv = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(daEnv))
        {
            return (daEnv, "OPENAI_API_KEY");
        }

        return (null, "não configurada");
    }

    private string OrigemConfiguracao()
    {
        var noLocal = OpenAiChaveArquivoLocal.LerApiKeyDoArquivo(_environment.ContentRootPath);
        if (!string.IsNullOrWhiteSpace(noLocal))
        {
            return OpenAiChaveArquivoLocal.NomeArquivo;
        }

        var nosSecrets = OpenAiUserSecretsStore.LerApiKey();
        if (!string.IsNullOrWhiteSpace(nosSecrets))
        {
            return "dotnet user-secrets (OpenAI:ApiKey)";
        }

        return "OpenAI:ApiKey (appsettings)";
    }

    private static string Mascarar(string chave)
    {
        if (chave.Length <= 8)
        {
            return new string('*', chave.Length);
        }

        var prefixo = chave.StartsWith("sk-", StringComparison.Ordinal) ? "sk-..." : "...";
        return prefixo + chave[^4..];
    }
}
