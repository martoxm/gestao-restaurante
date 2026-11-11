# 0008. Um banco só, um schema por API e nada de FK ou JOIN entre APIs

- **Status:** Aceito
- **Data:** 2026-10-26 (Roadmap de microserviços)

## Contexto

Regra de custo zero: só existe um Azure SQL na oferta gratuita. Ao mesmo tempo, cada microserviço precisa ser dono dos seus dados.

## Decisão

Um banco (`RestauranteEstoqueDb`) com schemas `identidade`, `estoque`, `vendas`, `pagamentos` e `email`. Cada API lê e grava só o próprio schema. As FKs de `RestauranteId` foram removidas (script 14). Dado de outra API chega por HTTP ou por mensagem (ex.: catálogo de produtos copiado na Vendas, M8d).

## Consequências

**Prós**

- Custo zero com fronteiras claras; separar em bancos depois é só trocar a connection string.
- Igual à arquitetura da empresa (um banco, cada API dona das suas tabelas).

**Contras**

- Sem integridade referencial entre APIs: a consistência passa a ser eventual.
- Nada no banco impede uma API de ler o schema da outra; a regra é disciplina.

## Alternativas descartadas

- Banco por API: custo.
- Manter FKs entre schemas: acoplaria os serviços no banco.
