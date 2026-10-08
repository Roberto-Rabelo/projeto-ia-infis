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
