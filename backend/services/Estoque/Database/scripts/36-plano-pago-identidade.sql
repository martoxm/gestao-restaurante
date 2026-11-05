-- Etapa M19c3: o plano pago passa a ter validade, e a Identidade passa a
-- consumir uma fila (plano-pago-queue), então ganha a tabela de idempotência.
--   1. identidade.Restaurantes.PlanoValidoAte — até quando o plano pago vale.
--      NULL nos que já existem: Teste usa o TesteTerminaEm, e os Pro que o
--      SuperAdmin deu na mão (M18a/M18f) não vencem.
--   2. identidade.MensagensProcessadas — a mesma cobrança nunca soma duas vezes.
-- Pode rodar de novo sem problema.

SET QUOTED_IDENTIFIER ON;

-- Trava: sem USE no script, quem roda no banco errado (master) para aqui.
IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Você está no banco master. Troque para o RestauranteEstoqueDb e rode de novo.', 16, 1);
    RETURN;
END;

IF COL_LENGTH('identidade.Restaurantes', 'PlanoValidoAte') IS NULL
    ALTER TABLE identidade.Restaurantes ADD PlanoValidoAte DATETIME2 NULL;

IF OBJECT_ID('identidade.MensagensProcessadas', 'U') IS NULL
BEGIN
    CREATE TABLE identidade.MensagensProcessadas
    (
        Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MensagensProcessadas_Identidade PRIMARY KEY,
        Fila         NVARCHAR(100)    NOT NULL,
        ProcessadoEm DATETIME2        NOT NULL
    );
END;

SELECT
    COL_LENGTH('identidade.Restaurantes', 'PlanoValidoAte') AS ColunaPlanoValidoAte,
    OBJECT_ID('identidade.MensagensProcessadas', 'U')       AS TabelaMensagensProcessadas;
