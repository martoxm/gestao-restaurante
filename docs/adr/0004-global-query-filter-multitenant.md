# 0004. Multi-tenant com Global Query Filter do EF Core

- **Status:** Aceito
- **Data:** 2026-10-14 (Etapa 34)

## Contexto

Desde a Etapa 30 cada Repository repetia `.Where(RestauranteId == ...)`. Esquecer um só vira vazamento entre restaurantes, o ponto crítico de segurança do projeto.

## Decisão

`HasQueryFilter` no `AppDbContext` de cada API, apontando para uma propriedade do contexto (`RestauranteIdAtual`), nunca para um valor copiado (ficaria congelado no cache do modelo). A escrita continua explícita (o Handler passa `RestauranteId` ao `Criar`). Desde a M20a os filtros têm nome (`Restaurante`, `NaoExcluido`), para poder ignorar só um deles.

## Consequências

**Prós**

- O filtro existe uma vez só; Repository novo já nasce protegido.
- Testes de isolamento (InMemory e SQL Server real) provam o comportamento.

**Contras**

- `Include` de relação obrigatória vira INNER JOIN e pode esconder linhas.
- Rotinas sem usuário (workers, consumidores, painel da plataforma) precisam definir o restaurante (`IDefinidorRestauranteAtual`) ou usar `IgnoreQueryFilters` de propósito, e esse ponto exige revisão.

## Alternativas descartadas

- `Where` manual em cada Repository: solução anterior, frágil.
- Banco por restaurante: custo e complexidade incompatíveis com o plano gratuito.
