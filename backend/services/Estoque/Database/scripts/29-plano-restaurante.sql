-- Etapa M18a: o restaurante ganha a "assinatura": Plano (Teste/Basico/Pro, como
-- texto), TesteTerminaEm (fim do período grátis, só no plano Teste) e Ativo.
--
-- Os restaurantes que já existem viram "Pro", sem prazo: são as contas criadas
-- antes dos planos existirem, e a gente não quer que a M18d (bloqueio com teste
-- vencido) tranque ninguém do nada. Restaurante novo nasce "Teste" pelo código
-- (Restaurante.Criar), não pelo DEFAULT daqui.
--
-- Pode rodar de novo sem problema: só mexe se a coluna Plano ainda não existe.
-- Não tem USE: rode conectado no RestauranteEstoqueDb. Os ALTER/UPDATE vão em
-- EXEC porque o SQL Server compila o lote inteiro antes de rodar, e o UPDATE
-- ainda não "enxergaria" as colunas novas.

IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Conecte no banco RestauranteEstoqueDb antes de rodar este script.', 16, 1);
    RETURN;
END

IF COL_LENGTH('identidade.Restaurantes', 'Plano') IS NULL
BEGIN
    BEGIN TRANSACTION;

    EXEC('ALTER TABLE identidade.Restaurantes ADD
        Plano NVARCHAR(20) NOT NULL CONSTRAINT DF_Restaurantes_Plano DEFAULT ''Teste'',
        TesteTerminaEm DATETIME2 NULL,
        Ativo BIT NOT NULL CONSTRAINT DF_Restaurantes_Ativo DEFAULT 1;');

    EXEC('UPDATE identidade.Restaurantes SET Plano = ''Pro'', TesteTerminaEm = NULL;');

    COMMIT TRANSACTION;
END

-- Conferência (também em EXEC, pelo mesmo motivo de compilação).
EXEC('SELECT Plano, Ativo, COUNT(*) AS Quantidade
FROM identidade.Restaurantes
GROUP BY Plano, Ativo;');
