# 0003. ASP.NET Core Identity no lugar de User próprio com BCrypt

- **Status:** Aceito
- **Data:** 2026-09-18 (Etapas 23-25)

## Contexto

Até a Etapa 22 havia uma entidade `User` no Domain com hash BCrypt. O autor viu que a empresa usa ASP.NET Core Identity em produção (`AspNetUsers`, `AspNetRoles`...).

## Decisão

`ApplicationUser : IdentityUser<Guid>` e `ApplicationRole` no Infrastructure (nunca no Domain). A Application conversa só com `IIdentityService` (tipos primitivos). `AddIdentityCore` (sem cookie); o JWT continua emitido por nós. As 7 tabelas do Identity vêm por uma Migration, única exceção ao ADR 0001. Hoje tudo isso mora na API de Identidade, schema `identidade`.

## Consequências

**Prós**

- Hash de senha, bloqueio, tokens de redefinição e de confirmação de e-mail e roles prontos e testados.
- Mesmo padrão da empresa.

**Contras**

- Dependência pesada; tabelas com schema fixo do framework.
- Uma Migration convivendo com scripts manuais.

## Alternativas descartadas

- Manter `User` + BCrypt: revertido.
- `AddIdentity` completo: descartado, a API só fala JWT Bearer.
- Replicar a tabela `AspNetUsersDetails` da empresa: descartado, extensão vira propriedade no `ApplicationUser`.
