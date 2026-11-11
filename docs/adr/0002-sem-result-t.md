# 0002. Erros esperados por exception mapeada no middleware, sem Result<T>

- **Status:** Aceito
- **Data:** 2026-09 (Reforma de padrões)

## Contexto

O projeto Financeiro da empresa usa `Result<T>`/`ResultCode` para erros esperados, mas isso vem do `Prover.Core`, pacote NuGet privado que não pode ser referenciado num projeto pessoal.

## Decisão

Manter exceptions específicas (`ValidationException` 400, `NaoEncontradoException` 404, `DomainException` 400, `CredenciaisInvalidasException`/`SessaoInvalidaException` 401, `ConflitoConcorrenciaException` 409) convertidas em ProblemDetails num middleware único (`backend/shared/Core/Api/ExceptionHandlingExtensions.cs`).

## Consequências

**Prós**

- Handlers e entidades ficam curtos: quem quebra a regra lança e pronto.
- Um lugar só decide status HTTP e formato do erro (chaves camelCase, títulos em português).
- Sem dependência de pacote privado.

**Contras**

- Exception é mais cara que um retorno (irrelevante no volume deste projeto).
- O caminho de erro não aparece na assinatura do método.

## Alternativas descartadas

- Recriar um `Result<T>` próprio: descartado, o esquema de exceptions já cobria tudo.
