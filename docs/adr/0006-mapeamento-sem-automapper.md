# 0006. Mapeamento Entidade para DTO por extension method, sem AutoMapper

- **Status:** Aceito
- **Data:** 2026-10-21 (Etapa 47)

## Contexto

Os Handlers de leitura repetiam `new XxxDto(...)`. A pergunta era se valia adotar AutoMapper. Nenhum projeto da empresa usa AutoMapper/Mapster/Mapperly; o RH usa mapper manual (`ParaDto`).

## Decisão

Métodos de extensão `ParaDetalhesDto()`/`ParaItemListaDto()` numa `file static class <Entidade>Mapeamentos` no fim do `<Módulo>Handlers.cs`. Só copiam e achatam campos, sem regra. Command para Entidade continua pelo `Criar(...)`.

## Consequências

**Prós**

- Sem pacote nem configuração mágica; erro de mapeamento é erro de compilação.
- Fácil de achar e de depurar.

**Contras**

- Código manual para cada DTO novo.

## Alternativas descartadas

- AutoMapper: mapeamento por convenção esconde erros até a execução.
- `implicit operator` no DTO (estilo Financeiro): faria o Contracts depender do Domain.
