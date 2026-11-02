-- Etapa M11b: o schema da API de Email. Ela só tem uma tabela: a da
-- idempotência. Cada e-mail enviado deixa aqui o MessageId da mensagem da
-- email-queue; se a mesma mensagem chegar de novo, o Id já existe e o e-mail não
-- sai repetido. Sem FK pra nada: a API de Email não conhece restaurante.
--
-- Sem USE no topo (igual o 15 a 26). Pode rodar mais de uma vez (idempotente).

SET QUOTED_IDENTIFIER ON;

IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Você está no banco master. Selecione o RestauranteEstoqueDb na janela e rode de novo.', 16, 1);
    SET NOEXEC ON;
END;
GO

IF SCHEMA_ID('email') IS NULL
    EXEC('CREATE SCHEMA email');
GO

IF OBJECT_ID('email.MensagensProcessadas', 'U') IS NULL
BEGIN
    CREATE TABLE email.MensagensProcessadas
    (
        Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MensagensProcessadas_Email PRIMARY KEY,
        Fila         NVARCHAR(100)    NOT NULL,
        ProcessadoEm DATETIME2        NOT NULL
    );
END;
GO

SET NOEXEC OFF;
