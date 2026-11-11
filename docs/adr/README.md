# Registros de decisão de arquitetura (ADRs)

Cada arquivo registra UMA decisão importante do GestaoRestaurante: o contexto, o que foi decidido, o que se ganha e o que se perde, e o que foi descartado. Formato curto, no modelo de Michael Nygard.

Decisão nova vira arquivo novo com o próximo número. Decisão revista: o ADR antigo muda o status para `Substituído por NNNN` (histórico não se apaga).

| Nº | Decisão | Status | Data |
|---|---|---|---|
| 0001 | [Tabelas de negócio criadas por script SQL manual](0001-tabelas-por-script-sql.md) | Aceito | 2026-08 (Etapa 3) |
| 0002 | [Erros esperados por exception mapeada no middleware, sem Result<T>](0002-sem-result-t.md) | Aceito | 2026-09 (Reforma de padrões) |
| 0003 | [ASP.NET Core Identity no lugar de User próprio com BCrypt](0003-aspnet-identity.md) | Aceito | 2026-09-18 (Etapas 23-25) |
| 0004 | [Multi-tenant com Global Query Filter do EF Core](0004-global-query-filter-multitenant.md) | Aceito | 2026-10-14 (Etapa 34) |
| 0005 | [Value Objects para conceitos com regra própria](0005-value-objects.md) | Aceito | 2026-10-17 (Etapa 37) |
| 0006 | [Mapeamento Entidade para DTO por extension method, sem AutoMapper](0006-mapeamento-sem-automapper.md) | Aceito | 2026-10-21 (Etapa 47) |
| 0007 | [Projeto Core compartilhado por referência de projeto](0007-core-compartilhado.md) | Aceito | 2026-10-28 (Etapa M1) |
| 0008 | [Um banco só, um schema por API e nada de FK ou JOIN entre APIs](0008-schema-por-api.md) | Aceito | 2026-10-26 (Roadmap de microserviços) |
| 0009 | [RabbitMQ no lugar do Azure Service Bus](0009-rabbitmq.md) | Aceito | 2026-10-29 (Etapa M6) |
| 0010 | [Outbox para publicar e MensagensProcessadas para consumir uma vez só](0010-outbox-idempotencia.md) | Aceito | 2026-10-29 e 2026-10-30 (Etapas M6 e M8) |
| 0011 | [API Gateway com YARP](0011-gateway-yarp.md) | Aceito | 2026-11-03 (Etapa M17) |
