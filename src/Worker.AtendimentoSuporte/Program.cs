using Worker.AtendimentoSuporte;
using Worker.AtendimentoSuporte.Email;
using Worker.AtendimentoSuporte.Inbox;
using Worker.AtendimentoSuporte.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<RabbitMqConsumerOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqConsumerOptions.SectionName));

builder.Services
    .AddOptions<SmtpNotificacaoOptions>()
    .Bind(builder.Configuration.GetSection(SmtpNotificacaoOptions.SectionName));

builder.Services
    .AddOptions<SqlInboxOptions>()
    .Bind(builder.Configuration.GetSection(SqlInboxOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "SqlInbox:ConnectionString é obrigatória.")
    .ValidateOnStart();

builder.Services.AddSingleton<IInboxMessageRepository, SqlInboxMessageRepository>();
builder.Services.AddSingleton<IAtendimentoSuporteEmailSender, MailKitAtendimentoSuporteEmailSender>();
builder.Services.AddHostedService<AtendimentoFilaConsumerHostedService>();

var host = builder.Build();
host.Run();
