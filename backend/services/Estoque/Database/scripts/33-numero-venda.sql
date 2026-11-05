-- Etapa M19b: número da venda pra exibição ("Venda #42").
-- O Id da venda continua sendo o Guid; o Numero é só pras pessoas lerem.
-- É por restaurante: cada cliente tem a sua Venda #1, #2, #3...
--
-- Repara na ordem: a coluna nasce aceitando NULL, a gente numera as vendas que
-- já existem, e só depois ela vira NOT NULL e ganha o índice único. Se fosse
-- NOT NULL direto, as vendas antigas não teriam valor nenhum pra colocar.
-- Pode rodar de novo sem problema (cada passo confere se já foi feito).

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

-- Trava: sem USE no script, quem roda no banco errado (master) para aqui.
IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Você está no banco master. Troque para o RestauranteEstoqueDb e rode de novo.', 16, 1);
    RETURN;
END;

BEGIN TRANSACTION;

IF COL_LENGTH('vendas.Vendas', 'Numero') IS NULL
    EXEC('ALTER TABLE vendas.Vendas ADD Numero INT NULL;');

-- Numera as vendas antigas na ordem em que foram criadas, reiniciando em cada
-- restaurante (é o PARTITION BY que faz isso).
EXEC('
    WITH Numeradas AS (
        SELECT Numero,
               ROW_NUMBER() OVER (PARTITION BY RestauranteId ORDER BY CriadoEm, Id) AS NovoNumero
        FROM vendas.Vendas
    )
    UPDATE Numeradas SET Numero = NovoNumero WHERE Numero IS NULL;
');

EXEC('ALTER TABLE vendas.Vendas ALTER COLUMN Numero INT NOT NULL;');

-- O índice único é quem barra duas vendas do mesmo restaurante com o mesmo
-- número (se dois pedidos chegarem juntos, o segundo é recusado e vira 409).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Vendas_RestauranteId_Numero' AND object_id = OBJECT_ID('vendas.Vendas'))
    EXEC('CREATE UNIQUE INDEX IX_Vendas_RestauranteId_Numero ON vendas.Vendas (RestauranteId, Numero);');

COMMIT TRANSACTION;

-- Também em EXEC: o SQL Server confere o lote inteiro antes de rodar, e nessa
-- hora a coluna Numero ainda não existe (daria "Invalid column name").
EXEC('
    SELECT RestauranteId, COUNT(*) AS Vendas, MIN(Numero) AS Menor, MAX(Numero) AS Maior
    FROM vendas.Vendas
    GROUP BY RestauranteId;
');
