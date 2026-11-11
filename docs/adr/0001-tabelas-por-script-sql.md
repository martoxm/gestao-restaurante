# 0001. Tabelas de negócio criadas por script SQL manual

- **Status:** Aceito
- **Data:** 2026-08 (Etapa 3)

## Contexto

O projeto é de estudo e o autor quer aprender a criar tabelas, chaves e índices na mão. O EF Core oferece Migrations, que geram o schema a partir do modelo.

## Decisão

Tabelas de negócio nascem por scripts numerados em `backend/services/*/Database/scripts/`, idempotentes quando possível. O EF Core só mapeia (Fluent API) para o que já existe. Única exceção: as tabelas `AspNet*` do Identity, criadas por uma Migration (ver ADR 0003).

## Consequências

**Prós**

- Controle total do SQL (índices filtrados, `sp_rename`, transações, travas contra rodar no `master`).
- Aprendizado de SQL de verdade.
- O banco não muda sozinho em produção.

**Contras**

- Modelo e banco podem divergir sem aviso; cada mudança de entidade exige lembrar do script.
- A ordem de execução dos scripts é responsabilidade de quem sobe o ambiente (documentada no README).

## Alternativas descartadas

- EF Core Migrations para tudo: descartado pelo objetivo de aprendizado.
