# 0005. Value Objects para conceitos com regra própria

- **Status:** Aceito
- **Data:** 2026-10-17 (Etapa 37)

## Contexto

Preço, quantidade e e-mail eram `decimal`/`int`/`string` soltos, com a validação repetida na entidade (`ValidarPreco`, `ValidarQuantidade`).

## Decisão

`Dinheiro`, `Quantidade` e `Email` como `sealed record` com construtor privado e `Criar()` que lança `DomainException` (hoje em `backend/shared/Core/Domain/ValueObjects/`). Commands, DTOs e JSON continuam primitivos; o Handler converte na fronteira; o banco usa `HasConversion`, sem script novo.

## Consequências

**Prós**

- A regra mora num lugar só; valor inválido não existe dentro do domínio.
- Igualdade por valor de graça com `record`.

**Contras**

- Conversão na fronteira e `HasConversion` para lembrar (inclusive no `TestDbContext`).
- O FluentValidation continua validando os mesmos campos para dar mensagem por campo, então a regra aparece duas vezes.

## Alternativas descartadas

- Classe base `ValueObject`: desnecessária com `record`.
- VO também nos Contracts: faria o contrato da API depender do domínio.
