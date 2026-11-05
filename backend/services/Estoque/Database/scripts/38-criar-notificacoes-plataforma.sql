-- Etapa M19e: notificações (o "sino") do SuperAdmin.
--
-- identidade.NotificacoesPlataforma: uma linha por acontecimento dos CLIENTES
-- (cadastro novo, assinatura, renovação, troca de plano, teste ou plano vencido).
-- LidaEm nulo = não lida (é o número do badge). Chave só vem preenchida nos
-- vencimentos, e é única: é ela que impede o worker de avisar duas vezes.
-- Sem FK pra Restaurantes de propósito (é histórico, igual a auditoria).
--
-- Pode rodar de novo sem problema. Não tem USE: rode conectado no RestauranteEstoqueDb.

SET QUOTED_IDENTIFIER ON;

IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Conecte no banco RestauranteEstoqueDb antes de rodar este script.', 16, 1);
    RETURN;
END

IF OBJECT_ID('identidade.NotificacoesPlataforma') IS NULL
BEGIN
    CREATE TABLE identidade.NotificacoesPlataforma
    (
        Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_NotificacoesPlataforma PRIMARY KEY,
        Tipo          NVARCHAR(50)     NOT NULL,
        Titulo        NVARCHAR(150)    NOT NULL,
        Mensagem      NVARCHAR(500)    NOT NULL,
        RestauranteId UNIQUEIDENTIFIER NULL,
        Chave         NVARCHAR(150)    NULL,
        OcorridoEm    DATETIME2        NOT NULL,
        LidaEm        DATETIME2        NULL
    );

    -- A lista do sino vem da mais nova pra mais antiga.
    CREATE INDEX IX_NotificacoesPlataforma_OcorridoEm ON identidade.NotificacoesPlataforma (OcorridoEm DESC);
    -- O badge conta só as não lidas.
    CREATE INDEX IX_NotificacoesPlataforma_NaoLidas ON identidade.NotificacoesPlataforma (OcorridoEm) WHERE LidaEm IS NULL;
    -- Um vencimento, um aviso.
    CREATE UNIQUE INDEX UX_NotificacoesPlataforma_Chave ON identidade.NotificacoesPlataforma (Chave) WHERE Chave IS NOT NULL;
END
