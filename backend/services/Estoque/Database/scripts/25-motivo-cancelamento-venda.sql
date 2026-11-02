-- Etapa M9b: a venda cancelada passa a guardar o MOTIVO (EstoqueInsuficiente,
-- PagamentoRecusado, SemPagamento) e o detalhe que veio da outra API. Antes isso
-- só ia pro log, e a tela não conseguia dizer ao vendedor o que aconteceu.
-- Vendas canceladas antes deste script ficam com as duas colunas nulas.
--
-- Sem USE no topo (igual o 15 a 24). Pode rodar mais de uma vez (idempotente).

SET QUOTED_IDENTIFIER ON;

IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Você está no banco master. Selecione o RestauranteEstoqueDb na janela e rode de novo.', 16, 1);
    SET NOEXEC ON;
END;
GO

IF COL_LENGTH('vendas.Vendas', 'MotivoCancelamento') IS NULL
    ALTER TABLE vendas.Vendas ADD MotivoCancelamento NVARCHAR(30) NULL;
GO

IF COL_LENGTH('vendas.Vendas', 'DetalheCancelamento') IS NULL
    ALTER TABLE vendas.Vendas ADD DetalheCancelamento NVARCHAR(500) NULL;
GO
