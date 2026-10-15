using Api.Atendimento.Options;

namespace Api.Atendimento.Configuration;

public sealed class GeminiChaveGestor : IGeminiChaveGestor
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<GeminiChaveGestor> _logger;
    private readonly object _lock = new();
    private string? _chaveSessao;

    public GeminiChaveGestor(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IHostEnvironment hostEnvironment,
        ILogger<GeminiChaveGestor> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public GeminiChaveInfo ObterInfo(bool incluirChaveCompleta = false)
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
                "Configure a chave do Gemini em appsettings.Local.json, User Secrets (Gemini:ApiKey / GEMINI_API_KEY) ou variáveis GEMINI_API_KEY / GOOGLE_API_KEY.");
        }

        var sufixo = chave.Length >= 4 ? chave[^4..] : "????";
        _logger.LogDebug("Chave Gemini efetiva de {Origem}, sufixo {Sufixo}.", origem, sufixo);
        return chave;
    }

    public async Task AtualizarAsync(
        string apiKey,
        GeminiChavePersistencia persistirEm,
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

        if (persistirEm is GeminiChavePersistencia.Local or GeminiChavePersistencia.Ambos)
        {
            await GeminiChaveArquivoLocal.SalvarApiKeyAsync(
                _environment.ContentRootPath,
                chaveNormalizada,
                cancellationToken);
        }

        if (persistirEm is GeminiChavePersistencia.UserSecrets or GeminiChavePersistencia.Ambos)
        {
            await GeminiUserSecretsStore.SalvarApiKeyAsync(chaveNormalizada, cancellationToken);
        }

        lock (_lock)
        {
            _chaveSessao = chaveNormalizada;
        }

        var info = ObterInfo();
        _logger.LogInformation(
            "Chave Gemini atualizada via API ({Persistencia}). Sufixo {Sufixo}.",
            persistirEm,
            info.Sufixo);
    }

    private GeminiChaveInfo CriarInfo(string? chave, string origem, bool incluirChaveCompleta)
    {
        var configurada = !string.IsNullOrWhiteSpace(chave);
        var sufixo = configurada && chave!.Length >= 4 ? chave[^4..] : string.Empty;
        var mascara = configurada ? Mascarar(chave!) : string.Empty;
        var caminhoSecrets = _hostEnvironment.IsDevelopment()
            ? GeminiUserSecretsStore.ObterCaminhoArquivo()
            : null;

        return new GeminiChaveInfo(
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
                return (_chaveSessao, "sessão (atualizada via API)");
            }
        }

        var daConfig = _configuration.GetSection(GeminiOptions.SectionName)["ApiKey"];
        if (!string.IsNullOrWhiteSpace(daConfig))
        {
            return (daConfig, OrigemConfiguracao());
        }

        var daConfigEnv = _configuration["GEMINI_API_KEY"];
        if (!string.IsNullOrWhiteSpace(daConfigEnv))
        {
            return (daConfigEnv, OrigemGeminiApiKeyConfig());
        }

        var daConfigGoogle = _configuration["GOOGLE_API_KEY"];
        if (!string.IsNullOrWhiteSpace(daConfigGoogle))
        {
            return (daConfigGoogle, "GOOGLE_API_KEY (configuração)");
        }

        var daEnv = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(daEnv))
        {
            return (daEnv, "GEMINI_API_KEY (variável de ambiente)");
        }

        var daEnvGoogle = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        if (!string.IsNullOrWhiteSpace(daEnvGoogle))
        {
            return (daEnvGoogle, "GOOGLE_API_KEY (variável de ambiente)");
        }

        return (null, "não configurada");
    }

    private string OrigemConfiguracao()
    {
        var noLocal = GeminiChaveArquivoLocal.LerApiKeyDoArquivo(_environment.ContentRootPath);
        if (!string.IsNullOrWhiteSpace(noLocal))
        {
            return OpenAiChaveArquivoLocal.NomeArquivo + " (Gemini:ApiKey)";
        }

        var nosSecrets = GeminiUserSecretsStore.LerApiKey();
        if (!string.IsNullOrWhiteSpace(nosSecrets))
        {
            return "dotnet user-secrets (Gemini:ApiKey / GEMINI_API_KEY)";
        }

        return "Gemini:ApiKey (appsettings)";
    }

    private string OrigemGeminiApiKeyConfig()
    {
        var nosSecrets = GeminiUserSecretsStore.LerApiKey();
        if (!string.IsNullOrWhiteSpace(nosSecrets))
        {
            return "dotnet user-secrets (GEMINI_API_KEY)";
        }

        return "GEMINI_API_KEY (configuração)";
    }

    private static string Mascarar(string chave)
    {
        if (chave.Length <= 8)
        {
            return new string('*', chave.Length);
        }

        return "..." + chave[^4..];
    }
}
