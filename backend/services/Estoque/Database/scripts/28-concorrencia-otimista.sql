-- Etapa Q1: concorrência otimista. Uma coluna rowversion em estoque.Produtos e
-- vendas.Vendas: o SQL Server troca o valor sozinho a cada UPDATE, e o EF Core
-- manda o valor que leu no WHERE. Se alguém salvou no meio do caminho, o UPDATE
-- não acha a linha e a API devolve 409 em vez de sobrescrever.
--
-- Pode rodar de novo sem problema (confere antes de criar). Não tem USE: rode
-- conectado no RestauranteEstoqueDb. E repara que não precisa de UPDATE pras
-- linhas antigas: rowversion nunca é nulo, o próprio ADD já preenche todas.

IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Conecte no banco RestauranteEstoqueDb antes de rodar este script.', 16, 1);
    RETURN;
END

IF COL_LENGTH('estoque.Produtos', 'VersaoLinha') IS NULL
    ALTER TABLE estoque.Produtos ADD VersaoLinha ROWVERSION NOT NULL;

IF COL_LENGTH('vendas.Vendas', 'VersaoLinha') IS NULL
    ALTER TABLE vendas.Vendas ADD VersaoLinha ROWVERSION NOT NULL;
