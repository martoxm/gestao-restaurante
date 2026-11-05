-- Etapa M18b: convites e Outbox na API de Identidade.
--
-- identidade.Convites: o convite que o Admin manda. Guarda só o HASH do token
-- (o token de verdade vai no link do e-mail), igual identidade.RefreshTokens.
-- O índice único no hash deixa a busca do link rápida; o outro índice serve a
-- listagem do Admin (filtrada por restaurante).
--
-- identidade.OutboxMensagens: a mesma tabela que Estoque/Vendas/Pagamentos já
-- têm (Etapa M6 + colunas de rastreio da M10). O e-mail do convite entra aqui
-- no mesmo SaveChanges do convite e o worker publica na email-queue.
--
-- Pode rodar de novo sem problema (confere antes de criar). Não tem USE:
-- rode conectado no RestauranteEstoqueDb.

SET QUOTED_IDENTIFIER ON;   -- índice filtrado exige (o sqlcmd vem com OFF)

IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Conecte no banco RestauranteEstoqueDb antes de rodar este script.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

IF OBJECT_ID('identidade.Convites') IS NULL
BEGIN
    CREATE TABLE identidade.Convites
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Convites PRIMARY KEY,
        RestauranteId   UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT FK_Convites_Restaurantes REFERENCES identidade.Restaurantes (Id),
        NomeRestaurante NVARCHAR(150)    NOT NULL,
        Email           NVARCHAR(200)    NOT NULL,
        Role            NVARCHAR(50)     NOT NULL,
        TokenHash       NVARCHAR(64)     NOT NULL,
        CriadoEm        DATETIME2        NOT NULL,
        ExpiraEm        DATETIME2        NOT NULL,
        AceitoEm        DATETIME2        NULL,
        CanceladoEm     DATETIME2        NULL
    );

    CREATE UNIQUE INDEX IX_Convites_TokenHash ON identidade.Convites (TokenHash);
    CREATE INDEX IX_Convites_RestauranteId ON identidade.Convites (RestauranteId, CriadoEm);
END;

IF OBJECT_ID('identidade.OutboxMensagens') IS NULL
BEGIN
    CREATE TABLE identidade.OutboxMensagens
    (
        Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OutboxMensagens_Identidade PRIMARY KEY,
        Fila               NVARCHAR(100)    NOT NULL,
        Tipo               NVARCHAR(200)    NOT NULL,
        Conteudo           NVARCHAR(MAX)    NOT NULL,
        Tentativas         INT              NOT NULL,
        CriadoEm           DATETIME2        NOT NULL,
        ProximaTentativaEm DATETIME2        NOT NULL,
        ProcessadoEm       DATETIME2        NULL,
        UltimoErro         NVARCHAR(2000)   NULL,
        CorrelationId      NVARCHAR(100)    NULL,
        TraceParent        NVARCHAR(100)    NULL
    );

    CREATE INDEX IX_OutboxMensagens_Pendentes
        ON identidade.OutboxMensagens (ProximaTentativaEm)
        WHERE ProcessadoEm IS NULL;
END;

COMMIT TRANSACTION;
