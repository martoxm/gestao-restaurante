-- Etapa M19d: quando o restaurante assinou e quando pagou pela última vez.
-- O painel do SuperAdmin mostra quem é assinante, desde quando e até quando vale.
-- Idempotente; sem USE (rodar conectado no RestauranteEstoqueDb).
IF DB_NAME() = 'master' BEGIN RAISERROR('Conecte no RestauranteEstoqueDb, não no master.', 16, 1); RETURN; END;

IF COL_LENGTH('identidade.Restaurantes', 'AssinadoEm') IS NULL
    ALTER TABLE identidade.Restaurantes ADD AssinadoEm DATETIME2 NULL;
IF COL_LENGTH('identidade.Restaurantes', 'UltimoPagamentoEm') IS NULL
    ALTER TABLE identidade.Restaurantes ADD UltimoPagamentoEm DATETIME2 NULL;
