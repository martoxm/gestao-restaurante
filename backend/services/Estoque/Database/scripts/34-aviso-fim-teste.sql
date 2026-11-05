-- Etapa M19c1: guarda qual foi o último aviso de "seu teste está acabando"
-- que já saiu pro restaurante (3 = o de 3 dias antes, 1 = o de 1 dia antes).
-- É o que impede o worker da Identidade de mandar o mesmo e-mail duas vezes.
-- NULL = nenhum aviso enviado ainda (vale pra todos os restaurantes atuais).
-- Pode rodar de novo sem problema.

SET QUOTED_IDENTIFIER ON;

-- Trava: sem USE no script, quem roda no banco errado (master) para aqui.
IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Você está no banco master. Troque para o RestauranteEstoqueDb e rode de novo.', 16, 1);
    RETURN;
END;

IF COL_LENGTH('identidade.Restaurantes', 'UltimoAvisoFimTesteDias') IS NULL
    ALTER TABLE identidade.Restaurantes ADD UltimoAvisoFimTesteDias INT NULL;

-- Em EXEC porque o SQL Server confere o lote inteiro antes de rodar, e nessa
-- hora a coluna ainda não existe.
EXEC('
    SELECT Plano, COUNT(*) AS Restaurantes, COUNT(UltimoAvisoFimTesteDias) AS JaAvisados
    FROM identidade.Restaurantes
    GROUP BY Plano;
');
