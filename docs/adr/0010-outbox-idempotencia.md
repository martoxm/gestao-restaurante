# 0010. Outbox para publicar e MensagensProcessadas para consumir uma vez só

- **Status:** Aceito
- **Data:** 2026-10-29 e 2026-10-30 (Etapas M6 e M8)

## Contexto

Gravar no banco e publicar na fila são duas operações: se a API cair entre elas, perde-se a mensagem ou publica-se algo que não foi salvo. Do lado do consumidor, o broker pode entregar a mesma mensagem mais de uma vez.

## Decisão

Quem publica grava uma `OutboxMensagem` no mesmo `SaveChanges` do dado de negócio; o `PublicacaoOutboxWorker` publica depois (15 s, 50 por rodada, com backoff). Quem consome registra o `MessageId` em `MensagensProcessadas` no mesmo `SaveChanges` do efeito e ignora repetidas. Quando o próprio dado já é idempotente (catálogo por `OcorridoEm`, pagamento por chave `venda-{Id}`), a tabela não é usada. Limpeza após 7 dias (Q3).

## Consequências

**Prós**

- Nenhuma mensagem perdida nem efeito duplicado; a saga venda, estoque e pagamento se apoia nisso.
- Mesmo padrão da empresa.

**Contras**

- Atraso de alguns segundos; o worker consultando o banco gasta a franquia gratuita do Azure SQL.
- Duas tabelas a mais por serviço (as classes são genéricas, no `Core.Persistencia`).

## Alternativas descartadas

- Publicar direto no Handler: perde mensagem em falha.
- Transação distribuída: não existe entre SQL Server e RabbitMQ.
