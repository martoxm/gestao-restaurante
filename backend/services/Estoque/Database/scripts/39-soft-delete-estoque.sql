-- Etapa M20a: exclusão lógica (soft delete) em Produtos, Categorias e Fornecedores.
-- Excluir passa a só preencher ExcluidoEm; a API esconde a linha pelo Global
-- Query Filter "NaoExcluido". NULL = ativo, então as linhas que já existem
-- continuam ativas sem precisar de UPDATE.
-- Idempotente; sem USE (rodar conectado no RestauranteEstoqueDb).
IF DB_NAME() = 'master' BEGIN RAISERROR('Conecte no RestauranteEstoqueDb, não no master.', 16, 1); RETURN; END;

IF COL_LENGTH('estoque.Produtos', 'ExcluidoEm') IS NULL
    ALTER TABLE estoque.Produtos ADD ExcluidoEm DATETIME2 NULL;
IF COL_LENGTH('estoque.Categorias', 'ExcluidoEm') IS NULL
    ALTER TABLE estoque.Categorias ADD ExcluidoEm DATETIME2 NULL;
IF COL_LENGTH('estoque.Fornecedores', 'ExcluidoEm') IS NULL
    ALTER TABLE estoque.Fornecedores ADD ExcluidoEm DATETIME2 NULL;
