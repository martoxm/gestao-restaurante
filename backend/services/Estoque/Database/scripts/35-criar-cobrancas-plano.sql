-- Etapa M19c2: as cobranças de plano (o restaurante pagando mais 30 dias de
-- Básico ou Pro pelo Stripe Checkout, em modo teste). Fica no schema
-- "pagamentos", que é da API de Pagamentos. Sem FK pro restaurante: ele mora
-- no schema "identidade", e entre APIs não tem FK nem JOIN.
-- Pode rodar de novo sem problema.

SET QUOTED_IDENTIFIER ON;

-- Trava: sem USE no script, quem roda no banco errado (master) para aqui.
IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Você está no banco master. Troque para o RestauranteEstoqueDb e rode de novo.', 16, 1);
    RETURN;
END;

IF OBJECT_ID('pagamentos.CobrancasPlano') IS NULL
BEGIN
    CREATE TABLE pagamentos.CobrancasPlano (
        Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RestauranteId   UNIQUEIDENTIFIER NOT NULL,
        Plano           NVARCHAR(20)     NOT NULL,   -- "Basico" ou "Pro"
        Valor           DECIMAL(18,2)    NOT NULL,
        Dias            INT              NOT NULL,
        Status          NVARCHAR(20)     NOT NULL,   -- "Pendente" ou "Paga"
        CriadoEm        DATETIME2        NOT NULL,
        PagaEm          DATETIME2        NULL,
        IdSessaoGateway NVARCHAR(200)    NULL        -- "cs_test_..." no Stripe
    );

    -- Todo SELECT passa pelo Global Query Filter de restaurante.
    CREATE INDEX IX_CobrancasPlano_RestauranteId ON pagamentos.CobrancasPlano (RestauranteId);

    -- O webhook da M19c3 acha a cobrança pelo Id da sessão. O filtro deixa de
    -- fora as linhas ainda sem sessão (NULL), senão o UNIQUE barraria a segunda.
    CREATE UNIQUE INDEX IX_CobrancasPlano_IdSessaoGateway
        ON pagamentos.CobrancasPlano (IdSessaoGateway)
        WHERE IdSessaoGateway IS NOT NULL;
END;

SELECT name AS Indice FROM sys.indexes WHERE object_id = OBJECT_ID('pagamentos.CobrancasPlano') AND name IS NOT NULL;
