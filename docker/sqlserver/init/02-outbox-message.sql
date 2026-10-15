USE [bd-custos-ia];
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
