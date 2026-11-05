-- Etapa M18f: auditoria das ações do SuperAdmin (dono da plataforma).
--
-- identidade.AuditoriasPlataforma: uma linha por ação (mudar plano, estender
-- teste, ativar/desativar restaurante, forçar logout...). Só INSERT — ninguém
-- edita nem apaga. Sem FK pra Restaurantes/AspNetUsers de propósito: a
-- auditoria tem que continuar existindo mesmo se o alvo sumir.
--
-- Pode rodar de novo sem problema (confere antes de criar). Não tem USE:
-- rode conectado no RestauranteEstoqueDb.

SET QUOTED_IDENTIFIER ON;

IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Conecte no banco RestauranteEstoqueDb antes de rodar este script.', 16, 1);
    RETURN;
END

IF OBJECT_ID('identidade.AuditoriasPlataforma') IS NULL
BEGIN
    CREATE TABLE identidade.AuditoriasPlataforma
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AuditoriasPlataforma PRIMARY KEY,
        SuperAdminId    UNIQUEIDENTIFIER NOT NULL,
        EmailSuperAdmin NVARCHAR(200)    NOT NULL,
        Acao            NVARCHAR(100)    NOT NULL,
        RestauranteId   UNIQUEIDENTIFIER NULL,
        UsuarioAlvoId   UNIQUEIDENTIFIER NULL,
        ValorAntes      NVARCHAR(500)    NULL,
        ValorDepois     NVARCHAR(500)    NULL,
        OcorridoEm      DATETIME2        NOT NULL
    );

    -- A tela de auditoria lista da mais nova pra mais antiga.
    CREATE INDEX IX_AuditoriasPlataforma_OcorridoEm ON identidade.AuditoriasPlataforma (OcorridoEm DESC);
    -- E o drill-down de um restaurante mostra só o histórico dele.
    CREATE INDEX IX_AuditoriasPlataforma_RestauranteId ON identidade.AuditoriasPlataforma (RestauranteId);
END
