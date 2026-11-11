# 0009. RabbitMQ no lugar do Azure Service Bus

- **Status:** Aceito
- **Data:** 2026-10-29 (Etapa M6)

## Contexto

A empresa usa Azure Service Bus, que não tem plano gratuito. O plano inicial era usar o emulador dele.

## Decisão

RabbitMQ no Docker Desktop (`rabbitmq:4-management`, painel em `localhost:15672`) com o SDK puro `RabbitMQ.Client`, sem MassTransit, atrás de `IPublicadorMensagens` e `ConsumidorFilaBase<T>` no Core. Filas ponto a ponto com uma irmã `.dlq`; retry pelo cabeçalho `x-tentativas` (5 tentativas).

## Consequências

**Prós**

- Grátis, muito usado no mercado, um container só e painel web para estudar.
- A interface no Core permite trocar de broker depois.

**Contras**

- Mais um processo para subir localmente; exceção à regra de não usar Docker.
- Sem os recursos gerenciados do Service Bus.

## Alternativas descartadas

- Azure Service Bus real: pago.
- Emulador do Service Bus: trocado por decisão do autor.
- MassTransit: abstrai demais para quem está aprendendo os conceitos.
