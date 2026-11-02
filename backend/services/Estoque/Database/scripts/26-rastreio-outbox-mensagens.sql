-- Etapa M10: as 3 tabelas de Outbox (estoque, vendas e pagamentos) passam a
-- guardar o CorrelationId e o traceparent da requisição que criou a linha. O
-- worker publica depois, fora da requisição; sem essas colunas, o "número de
-- protocolo" e o trace se perdiam no meio da saga. Linhas antigas ficam nulas.
--
-- Sem USE no topo (igual o 15 a 25). Pode rodar mais de uma vez (idempotente).

SET QUOTED_IDENTIFIER ON;

IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Você está no banco master. Selecione o RestauranteEstoqueDb na janela e rode de novo.', 16, 1);
    SET NOEXEC ON;
END;
GO

IF COL_LENGTH('estoque.OutboxMensagens', 'CorrelationId') IS NULL
    ALTER TABLE estoque.OutboxMensagens ADD CorrelationId NVARCHAR(100) NULL, TraceParent NVARCHAR(100) NULL;
GO

IF COL_LENGTH('vendas.OutboxMensagens', 'CorrelationId') IS NULL
    ALTER TABLE vendas.OutboxMensagens ADD CorrelationId NVARCHAR(100) NULL, TraceParent NVARCHAR(100) NULL;
GO

IF COL_LENGTH('pagamentos.OutboxMensagens', 'CorrelationId') IS NULL
    ALTER TABLE pagamentos.OutboxMensagens ADD CorrelationId NVARCHAR(100) NULL, TraceParent NVARCHAR(100) NULL;
GO

SET NOEXEC OFF;
GO
