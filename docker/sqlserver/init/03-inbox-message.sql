USE [bd-custos-ia];
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
