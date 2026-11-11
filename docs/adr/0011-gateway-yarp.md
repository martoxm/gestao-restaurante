# 0011. API Gateway com YARP

- **Status:** Aceito
- **Data:** 2026-11-03 (Etapa M17)

## Contexto

O front tinha uma URL por API, e CORS e limite de requisições estavam espalhados. A empresa usa Azure API Management, que não é gratuito.

## Decisão

`backend/services/Gateway/` com `Yarp.ReverseProxy`: rotas `/identidade/*`, `/estoque/*`, `/vendas/*` e só `/pagamentos/api/v1/assinaturas/*`; `/api/v1/interno/*` bloqueado; CORS e rate limiting só no gateway; o JWT é conferido na entrada. As APIs continuam validando o JWT (defesa em profundidade). O gateway não junta respostas (isso seria BFF).

## Consequências

**Prós**

- Front com uma URL só; preocupações transversais num lugar.
- Grátis e configurável no `appsettings.json`.

**Contras**

- Mais um salto de rede e mais um processo para subir.
- Swagger pelo gateway não funciona (usa-se a porta da própria API).

## Alternativas descartadas

- Azure API Management: pago.
- BFF: discutido e descartado pelo autor.
- Ficar sem gateway: era o estado até a M16.
