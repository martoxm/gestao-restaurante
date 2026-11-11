# 0007. Projeto Core compartilhado por referência de projeto

- **Status:** Aceito
- **Data:** 2026-10-28 (Etapa M1)

## Contexto

Ao quebrar em microserviços, `Entity`, `ValidationBehavior`, `IEndpointModule`, autenticação JWT, `IUsuarioAtualService`, middleware de erros e CorrelationId seriam copiados em cada API. A empresa resolve isso com o pacote NuGet `Prover.Core`.

## Decisão

`GestaoRestaurante.Core` em `backend/shared/Core/`, referenciado por projeto (não NuGet); o Domain de cada serviço também o referencia. Depois (Q5) veio `Core.Persistencia`, separado para o EF Core não chegar no Domain. Os contratos das mensagens de fila também moram no Core.

## Consequências

**Prós**

- Uma correção vale para todas as APIs (ex.: o bug do `ValidationBehavior` corrigido na M12).
- Sem feed de pacotes para manter; o monorepo compila tudo junto.

**Contras**

- Acoplamento: mudar o Core pode quebrar todos os serviços (mitigado pelos testes de arquitetura da M20c e de contrato da M20d).
- Os serviços não versionam o Core de forma independente.

## Alternativas descartadas

- Pacote NuGet privado: opção futura, sem necessidade agora.
- Core separado por camada ou kernel duplicado em cada serviço: discutidos e descartados pelo autor.
