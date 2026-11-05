-- Etapa M18e: a confirmação de e-mail passou a valer para quem se cadastra
-- daqui pra frente. As contas que já existiam nasceram com EmailConfirmed = 0
-- (ninguém nunca pediu confirmação), e sem este script todas iam ver o aviso
-- "confirme seu e-mail". Então a gente marca as antigas como confirmadas.
-- Pode rodar de novo sem problema (só mexe em quem ainda está 0).
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

IF DB_NAME() = 'master'
BEGIN
    RAISERROR('Rode este script no banco RestauranteEstoqueDb, não no master.', 16, 1);
    RETURN;
END

UPDATE identidade.AspNetUsers
SET EmailConfirmed = 1
WHERE EmailConfirmed = 0;

SELECT COUNT(*) AS ContasNaoConfirmadas FROM identidade.AspNetUsers WHERE EmailConfirmed = 0;
