# Gestão Restaurante

Sistema de gestão para restaurantes — estoque, vendas, pagamentos e assinatura da plataforma — feito em **microserviços .NET** (CQRS + DDD) com front-end em **Next.js**. Vários restaurantes usam as mesmas APIs e o mesmo banco, e cada um só enxerga os próprios dados (multi-tenant). Projeto de estudo full stack, construído do zero em etapas.

**No ar:** https://gestao.martodev.online (front na Vercel; APIs no Azure App Service, banco no Azure SQL e RabbitMQ no CloudAMQP).

## Stack

**Backend** — C# / .NET 10, Minimal API, CQRS com MediatR, DDD (entidades ricas, Value Objects, Domain Services, Domain Events), Entity Framework Core sobre SQL Server (Azure SQL), FluentValidation, ASP.NET Core Identity + JWT com refresh token, RabbitMQ (Outbox, idempotência e dead-letter), API Gateway com YARP, Stripe em modo teste, OpenTelemetry + .NET Aspire, testes com xUnit e Testcontainers.

**Frontend** — Next.js (App Router) + TypeScript, Tailwind CSS, shadcn/ui, Axios, TanStack Query, Recharts, tema claro/escuro.

## Funcionalidades

**Restaurante**
- Conta da empresa: quem se cadastra cria o restaurante e já nasce `Admin`, com 14 dias de teste grátis
- Convite de funcionários por e-mail, com papel `Admin` ou `Funcionario` e limite de usuários por plano (Teste 3, Básico 5, Pro ilimitado)
- Tela de usuários (só Admin): trocar papel, desativar e reativar acesso
- Esqueci a senha, confirmação de e-mail e sessão renovada sozinha (refresh token)
- Produtos, Categorias e Fornecedores com busca, filtro, ordenação e paginação
- Exclusão lógica (soft delete): o item excluído some das listas, mas o histórico é preservado e o Admin pode reativá-lo
- Movimentação de estoque (entrada/saída) direto da linha do produto, com histórico filtrável
- Aviso por e-mail aos Admins quando um produto fica com estoque baixo
- Vendas numeradas por restaurante (#1, #2...), com baixa de estoque e cobrança automáticas; se faltar estoque ou o pagamento for recusado, a venda é cancelada e o estoque volta
- Dashboard com totais, gráficos, faturamento, ticket médio e produtos para repor
- Assinatura do plano Básico/Pro pelo Stripe Checkout (modo teste); com o teste ou o plano vencido, o login continua, mas a escrita fica bloqueada até pagar
- Layout responsivo (tabelas viram cartões no celular) e modo escuro

**Plataforma (SuperAdmin)**
- Painel com restaurantes por plano e situação, funil de conversão, MRR estimado e cadastros por dia
- Busca e filtros de restaurantes, com detalhes, mudança de plano, extensão do teste, ativar/desativar, reenviar convite e forçar logout
- Auditoria de toda ação do SuperAdmin e sino de notificações (cadastros, assinaturas, renovações e vencimentos)

## Arquitetura

```
frontend (Next.js) ──► Gateway (YARP) ──► Identidade · Estoque · Vendas · Pagamentos (/assinaturas)
                                             │          │         │          │
                                             └──── RabbitMQ (filas + Outbox) ┴──► Email
```

- `backend/services/Gateway` — porta única do front: repassa `/identidade`, `/estoque`, `/vendas` e `/pagamentos/api/v1/assinaturas`, confere o JWT na entrada, concentra o CORS, limita requisições por IP e esconde os endpoints internos
- `backend/services/Identidade` — usuários, roles, restaurantes, planos, convites e painel da plataforma; é a única API que gera o token
- `backend/services/Estoque` — produtos, categorias, fornecedores e movimentações
- `backend/services/Vendas` — vendas e itens; lê nome e preço de uma cópia local dos produtos, mantida pelo Estoque via fila
- `backend/services/Pagamentos` — cobrança das vendas (gateway falso ou Stripe em modo teste, com webhook assinado por HMAC) e do plano da assinatura
- `backend/services/Email` — só envia e-mails: escuta a `email-queue` e manda pelo SMTP
- `backend/shared/Core` e `Core.Persistencia` — código comum: `Entity`, Value Objects, validação, erros, JWT, CorrelationId, mensageria, Outbox e idempotência
- `backend/AppHost` e `backend/shared/ServiceDefaults` — .NET Aspire: sobe tudo junto, com OpenTelemetry e `/health`

Cada API segue Clean Architecture (`Domain` → `Application` → `Infrastructure` → `Api`, mais `Contracts` com os DTOs), com o código em português (linguagem ubíqua).

Alguns detalhes:

- **Saga da venda**: Vendas → baixa o estoque → cobra → confirma; qualquer recusa cancela e devolve o estoque (compensação), tudo por filas do RabbitMQ.
- **Outbox + idempotência**: a mensagem é gravada na mesma transação do dado e publicada depois por um worker; cada consumidor marca o que já processou, e o que falha 5 vezes vai para a dead-letter queue.
- **Multi-tenant**: o `restauranteId` vem do JWT e vira Global Query Filter do EF Core; testes de isolamento (inclusive em SQL Server real) garantem que um restaurante não lê dados de outro.
- **Banco**: um banco só, com um schema por API (`identidade`, `estoque`, `vendas`, `pagamentos`, `email`) e sem FK nem JOIN entre APIs. As tabelas de negócio são criadas por script SQL; só as do Identity usam Migration.
- **Concorrência otimista** (`rowversion`) em produtos e vendas, e retry automático em falhas passageiras do SQL.

- **Decisões de arquitetura** registradas como ADRs em [`docs/adr/`](docs/adr/README.md) (RabbitMQ, schema por API, Outbox, gateway, sem AutoMapper...).

O frontend fica em `frontend/src`, organizado por funcionalidade (`features/produtos`, `features/vendas`, `features/plataforma`, ...).

## Como rodar localmente

**Pré-requisitos**: .NET 10 SDK, Node.js, SQL Server (local ou Azure SQL), Docker Desktop (RabbitMQ e testes de integração) e `dotnet tool install --global dotnet-ef`.

### 1. Banco de dados

Rode os scripts de `backend/services/Estoque/Database/scripts/` em ordem numérica. Entre o `05` e o `07`, crie as tabelas do Identity:

```bash
dotnet ef database update --project backend/services/Identidade/Infrastructure --startup-project backend/services/Identidade/Api
```

Observações: o `06` é só um modelo para promover alguém a Admin (opcional); o `10` só é necessário se o banco tiver dados antigos sem restaurante. Os scripts sem `USE` devem ser rodados com o banco `RestauranteEstoqueDb` selecionado.

### 2. Configuração

Todas as APIs compartilham o mesmo *user-secrets*. Fora da sua máquina, nunca use os valores do `appsettings.json`: defina por variável de ambiente pelo menos `Jwt__Secret` (igual em todas as APIs), `ConnectionStrings__DefaultConnection` e `ApiInterna__Chave`.

Opcionais, sempre no user-secrets:

- **E-mail (Gmail)**: `Email:Usuario` e `Email:Senha` (uma [senha de app](https://myaccount.google.com/apppasswords)). Sem isso, as mensagens vão para a `email-queue.dlq`.
- **Stripe (modo teste)**: `Gateway:Provedor` = `Stripe`, `Stripe:ChaveSecreta` e `Stripe:SegredoWebhook`. Sem isso, usa o gateway falso.
- **SuperAdmin**: `SuperAdmin:Email` e `SuperAdmin:Senha` — a Identidade cria a conta ao subir.

```bash
dotnet user-secrets set "Email:Usuario" "seu.email@gmail.com" --project backend/services/Email/Api
```

### 3. RabbitMQ

```bash
docker run -d --name rabbitmq --restart unless-stopped -p 5672:5672 -p 15672:15672 rabbitmq:4-management
```

Painel em http://localhost:15672 (`guest`/`guest`).

### 4. Backend

Na primeira vez:

```bash
dotnet dev-certs https --trust
```

O jeito mais simples é subir tudo pelo Aspire:

```bash
dotnet run --project backend/AppHost
```

Ou cada API no seu terminal, com `--launch-profile https`:

| API | Projeto | HTTPS |
|---|---|---|
| Identidade | `backend/services/Identidade/Api` | `https://localhost:7010` |
| Estoque | `backend/services/Estoque/Api` | `https://localhost:7008` |
| Vendas | `backend/services/Vendas/Api` | `https://localhost:7012` |
| Pagamentos | `backend/services/Pagamentos/Api` | `https://localhost:7014` |
| Email | `backend/services/Email/Api` | `https://localhost:7016` |
| Gateway | `backend/services/Gateway/Api` | `https://localhost:7018` |

Cada API tem Swagger em `/swagger` (na própria porta, não pelo gateway) e `/health`. Cartões de teste do gateway falso: `tok_aprovado`, `tok_saldo_insuficiente`, `tok_fraude`, `tok_gateway_fora`.

### 5. Frontend

Crie `frontend/.env.local` apontando para o gateway:

```
NEXT_PUBLIC_API_URL=https://localhost:7018
```

```bash
npm install --prefix frontend
```

```bash
npm run dev --prefix frontend
```

O app sobe em `http://localhost:3000`. Crie a conta da empresa em `/registrar`.

## Testes

```bash
dotnet test backend/GestaoRestaurante.slnx
```

Cobrem regras de domínio, Handlers, isolamento entre restaurantes, **regras de arquitetura** (NetArchTest: o Domain não depende de EF/MediatR/ASP.NET, o Application não enxerga o Infrastructure...), **contratos das mensagens** das filas (o JSON não muda de nome e mensagens antigas continuam sendo lidas) e, com o Docker aberto, testes de integração num SQL Server em container (Testcontainers) — sem Docker, eles são pulados.

## Deploy

- **Front**: Vercel (plano Hobby), lendo a pasta `frontend` da branch `prod`, com `NEXT_PUBLIC_API_URL` apontando para o gateway.
- **APIs**: 6 Web Apps Linux no Azure App Service (Identidade, Estoque, Vendas, Pagamentos, Email e Gateway), publicadas pelo Visual Studio. Segredos e URLs vêm das App Settings (`Jwt__Secret`, `ConnectionStrings__DefaultConnection`, `RabbitMq__Url`, `ApiInterna__Chave`, clusters do YARP...), nunca do repositório.
- **Banco**: Azure SQL com a oferta gratuita (pausa sozinho quando fica parado).
- **Mensageria**: RabbitMQ gerenciado no CloudAMQP (`RabbitMq__Url` com `amqps://`).
- **Saúde**: `/alive` (não toca no banco, usado pelo health check do App Service) e `/health` (banco e RabbitMQ).
