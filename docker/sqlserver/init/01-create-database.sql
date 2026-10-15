IF DB_ID(N'bd-custos-ia') IS NULL
BEGIN
    CREATE DATABASE [bd-custos-ia];
END
GO

USE [bd-custos-ia];
GO

IF OBJECT_ID(N'dbo.ConsumoTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsumoTokens
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ConsumoTokens PRIMARY KEY,
        Agente NVARCHAR(100) NOT NULL,
        Modelo NVARCHAR(100) NOT NULL,
        Usuario NVARCHAR(200) NOT NULL,
        DataHora DATETIME2 NOT NULL,
        QtdTokensEntrada INT NOT NULL,
        QtdTokensSaida INT NOT NULL,
        CustoTokensEntrada DECIMAL(18, 6) NOT NULL,
        CustoTokensSaida DECIMAL(18, 6) NOT NULL,
        CustoTotal DECIMAL(18, 6) NOT NULL,
        IdConversa NVARCHAR(64) NOT NULL
    );

    CREATE INDEX IX_ConsumoTokens_Usuario_DataHora
        ON dbo.ConsumoTokens (Usuario, DataHora DESC);
END
GO

IF OBJECT_ID(N'dbo.OutboxMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboxMessage
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_OutboxMessage PRIMARY KEY,
        DataHoraGravacao DATETIME2 NOT NULL,
        Transmitido BIT NOT NULL CONSTRAINT DF_OutboxMessage_Transmitido DEFAULT (0),
        DataHoraTransmissao DATETIME2 NULL,
        NomeFila NVARCHAR(200) NULL,
        Usuario NVARCHAR(200) NOT NULL,
        Descricao NVARCHAR(MAX) NOT NULL
    );

    CREATE INDEX IX_OutboxMessage_Transmitido_DataHoraGravacao
        ON dbo.OutboxMessage (Transmitido, DataHoraGravacao);
END
GO

IF OBJECT_ID(N'dbo.InboxMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InboxMessage
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_InboxMessage PRIMARY KEY,
        MensagemFilaId NVARCHAR(200) NOT NULL,
        DataHora DATETIME2 NOT NULL,
        NomeFila NVARCHAR(200) NOT NULL,
        Usuario NVARCHAR(200) NOT NULL,
        Descricao NVARCHAR(MAX) NOT NULL,
        StatusEnvioEmail NVARCHAR(500) NOT NULL
    );

    CREATE INDEX IX_InboxMessage_DataHora ON dbo.InboxMessage (DataHora DESC);
END
GO
