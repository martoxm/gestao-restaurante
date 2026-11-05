# Restaurante Estoque

Sistema de controle de estoque para restaurantes — API em .NET com CQRS/DDD e front-end em Next.js. Vários restaurantes usam a mesma API e o mesmo banco, e cada um só enxerga o próprio estoque (multi-tenant). Projeto de estudo full stack, construído do zero em etapas.

## Stack

**Backend** — C# / .NET 10, Minimal API, CQRS com MediatR (Commands/Queries/Handlers), DDD com entidades ricas (regras de negócio na própria entidade, não em serviços externos), Entity Framework Core sobre SQL Server, FluentValidation, autenticação JWT com ASP.NET Core Identity, autorização por roles (`Admin`/`Funcionario`), Repository Pattern, API versionada (`/api/v1`), testes com xUnit.

**Frontend** — Next.js (App Router) + TypeScript, Tailwind CSS, shadcn/ui, Axios, TanStack Query, Recharts, tema claro/escuro.

## Funcionalidades

- Cadastro e login (JWT). Cada cadastro cria um restaurante novo, e o usuário só vê os dados dele
- CRUD completo de Produtos, Categorias e Fornecedores
- Movimentação de estoque (entrada/saída) com validação de saldo
- Aviso por e-mail aos administradores do restaurante quando um produto fica com estoque baixo
- Permissões por role: editar/excluir Categoria e Fornecedor e excluir Produto são exclusivos de `Admin`; todo cadastro novo nasce `Funcionario`
- Listagens paginadas, com filtro e ordenação
- Dashboard com totais, produtos com estoque baixo, nome do restaurante e gráficos (produtos por categoria e estoque por categoria)
- Data de criação (`CreatedAt`) em produtos, categorias, fornecedores e restaurantes
- Layout responsivo com menu mobile e modo escuro

## Arquitetura

O repositório está se preparando para virar microserviços:

- `backend/services/Identidade` — usuários, roles e restaurantes; é a única API que gera o token JWT
- `backend/services/Estoque` — produtos, categorias, fornecedores e movimentações; só valida o token
- `backend/services/Vendas` — vendas e itens de venda (nome e preço copiados do produto); só valida o token e conversa com o Estoque pelo RabbitMQ
- `backend/services/Email` — só envia e-mails: escuta a fila `email-queue` do RabbitMQ e manda pelo SMTP, sem saber de restaurante ou estoque (quem pede já manda os destinatários)
- `backend/services/Pagamentos` — cobrança das vendas num gateway de pagamento falso (só para testes, não mexe em dinheiro); recebe o pedido da Vendas pelo RabbitMQ e responde por lá
- `backend/shared/Core` — código comum a todas as APIs: classe base `Entity`, Value Objects, validação, tratamento de erros, validação do JWT e CorrelationId
- `frontend` — o app Next.js

Cada API segue Clean Architecture, com as dependências apontando para dentro:

- `Domain` — entidades e regras de negócio, sem depender de nenhum framework
- `Application` — casos de uso (Commands, Queries e Handlers do MediatR), validators do FluentValidation e as interfaces que a Infrastructure implementa. Não depende do EF Core
- `Infrastructure` — EF Core, repositórios e relógio (`IRelogio`); na Identidade, também o ASP.NET Core Identity e a geração do JWT
- `Api` — endpoints Minimal API, um módulo por entidade, middleware de erros e Swagger
- `Contracts` — DTOs de entrada e saída, compartilhados entre as camadas

Alguns detalhes:

- **Validação** roda num pipeline behavior do MediatR, antes de qualquer Handler. Erros viram respostas `ProblemDetails` (400, 401, 404) num middleware único.
- **Multi-tenant**: o `RestaurantId` vem de uma claim do JWT e é aplicado como Global Query Filter do EF Core em toda leitura. Testes de isolamento garantem que um restaurante não lê os dados de outro.
- **Eventos de domínio + Outbox**: o `Produto` registra um `EstoqueFicouBaixoEvent`; um interceptor do EF publica o evento e o handler grava o aviso na tabela `OutboxMensagens` na mesma transação; um `BackgroundService` publica a mensagem numa fila do RabbitMQ e um consumidor manda o e-mail (SMTP com MailKit); o que falha vai para uma dead-letter queue.
- **Banco**: um banco só, com um schema por API (`identidade`, `estoque`, `vendas` e `pagamentos`) e nenhuma FK de uma API para a outra. As tabelas de negócio são criadas por script SQL manual. Só as tabelas do Identity (`AspNet*`) usam EF Core Migration, no projeto da Identidade.
- **Token compartilhado**: a Identidade assina o JWT (HS256) e o Estoque valida com a mesma chave, sem chamar a Identidade a cada requisição.

O frontend fica em `frontend/src`, organizado por funcionalidade (`features/produtos`, `features/categorias`, `features/auth`, ...).

## Como rodar localmente

**Pré-requisitos**: .NET 10 SDK, Node.js, SQL Server local e a ferramenta do EF Core (`dotnet tool install --global dotnet-ef`).

### 1. Banco de dados

Rode os scripts de `backend/services/Estoque/Database/scripts/` no SQL Server, **nesta ordem**:

1. `01-criar-tabelas.sql` — cria o banco `RestauranteEstoqueDb` e as tabelas principais
2. `03-criar-tabela-fornecedores.sql`
3. `05-remover-tabela-users.sql` — apaga a tabela de usuários antiga (os usuários agora vivem nas tabelas do Identity)

Depois, crie as tabelas do ASP.NET Core Identity pela migration:

```bash
dotnet ef database update --project backend/services/Identidade/Infrastructure --startup-project backend/services/Identidade/Api
```

E termine com os scripts que dependem delas:

4. `07-criar-tabela-restaurants.sql`
5. `08-adicionar-restaurantid.sql` — liga produtos, categorias, fornecedores, movimentações e usuários a um restaurante
6. `09-adicionar-createdat.sql`
7. `11-traduzir-banco-pt-br.sql` — traduz tabelas e colunas para português (`Produtos`, `Categorias`, `Fornecedores`, `MovimentacoesEstoque`, `Restaurantes`), o mesmo vocabulário do código
8. `12-criar-tabela-outbox-emails.sql` — caixa de saída dos e-mails de aviso de estoque baixo
9. `13-restauranteid-obrigatorio.sql` — torna `RestauranteId` obrigatório (`NOT NULL`) em produtos, categorias, fornecedores e movimentações; para sem alterar nada se ainda houver linha sem restaurante (rode o `10-vincular-dados-orfaos-restaurante-bk.sql` antes, se o banco tiver dados antigos)
10. `14-separar-schemas-por-api.sql` — move as tabelas para os schemas `identidade` e `estoque` e tira as FKs entre as duas APIs
11. `15-criar-tabela-refresh-tokens.sql` — guarda os refresh tokens (só o hash) da API de Identidade
12. `16-criar-tabela-outbox-mensagens.sql` — troca a caixa de saída de e-mails por uma genérica, de mensagens para o RabbitMQ
13. `17-criar-schema-vendas.sql` — cria o schema `vendas` com as tabelas `Vendas` e `ItensVenda` da API de Vendas
14. `18-criar-tabela-outbox-vendas.sql` — caixa de saída da Vendas (o pedido de baixa de estoque)
15. `19-criar-tabela-mensagens-processadas.sql` — controle de mensagens já processadas do Estoque
16. `20-criar-tabela-mensagens-processadas-vendas.sql` — o mesmo controle, na Vendas
17. `21-criar-catalogo-produtos-vendas.sql` — cópia local dos produtos na Vendas (nome e preço), já com os produtos que existem hoje
18. `22-criar-indices-estoque.sql` — índices de `RestauranteId` e das FKs nas tabelas do Estoque
19. `23-criar-schema-pagamentos.sql` — cria o schema `pagamentos` com a tabela `Pagamentos` da API de Pagamentos
20. `24-pagamento-na-saga-da-venda.sql` — liga o pagamento à venda: token do cartão na venda, contador de tentativas no pagamento e a caixa de saída da Pagamentos
21. `25-motivo-cancelamento-venda.sql` — guarda na venda cancelada o motivo e o detalhe
22. `26-rastreio-outbox-mensagens.sql` — CorrelationId e traceparent nas caixas de saída, para seguir a venda de ponta a ponta
23. `27-criar-schema-email.sql` — cria o schema `email` com o controle de mensagens já enviadas da API de Email
24. `28-concorrencia-otimista.sql` — adiciona a coluna `VersaoLinha` (rowversion) em `estoque.Produtos` e `vendas.Vendas`, pra uma gravação não sobrescrever a outra
25. `29-plano-restaurante.sql` — adiciona `Plano`, `TesteTerminaEm` e `Ativo` em `identidade.Restaurantes` (os restaurantes que já existiam viram `Pro`)
26. `30-criar-convites-e-outbox-identidade.sql` — cria `identidade.Convites` e `identidade.OutboxMensagens` (o e-mail do convite sai pela fila, igual ao aviso de estoque baixo)
27. `31-confirmar-emails-existentes.sql` — marca como confirmado o e-mail das contas que já existiam antes da confirmação de e-mail (Etapa M18e)
28. `33-numero-venda.sql` — adiciona `Numero` em `vendas.Vendas` (Venda #1, #2... por restaurante, numerando as vendas que já existiam) e o índice único `(RestauranteId, Numero)`
29. `34-aviso-fim-teste.sql` — adiciona `UltimoAvisoFimTesteDias` em `identidade.Restaurantes` (o e-mail de "seu teste está acabando" não sai repetido)
30. `35-criar-cobrancas-plano.sql` — cria `pagamentos.CobrancasPlano` (o restaurante pagando o plano Básico/Pro pelo Stripe Checkout em modo teste)
31. `36-plano-pago-identidade.sql` — adiciona `PlanoValidoAte` em `identidade.Restaurantes` e cria `identidade.MensagensProcessadas` (o plano pago chega pela fila `plano-pago-queue`)
32. `37-data-assinatura-restaurante.sql` — adiciona `AssinadoEm` e `UltimoPagamentoEm` em `identidade.Restaurantes` (painel do SuperAdmin mostra os assinantes)

### 2. Configuração

Os `appsettings.json` das duas APIs (`backend/services/Identidade/Api` e `backend/services/Estoque/Api`) já vêm prontos para a máquina local (SQL Server em `localhost` com autenticação do Windows). As duas usam o mesmo *user-secrets*, então o que você guardar lá vale para as duas. Fora da sua máquina, nunca use os valores do arquivo: defina o segredo do JWT e a connection string por variável de ambiente (`Jwt__Secret` e `ConnectionStrings__DefaultConnection`) — o `Jwt` tem que ser igual nas duas APIs.

**E-mail de estoque baixo (opcional).** Quando um produto fica abaixo de 10 unidades, o Estoque pede um e-mail para os `Admin` daquele restaurante. Para ligar isso com uma conta Gmail, crie uma [senha de app](https://myaccount.google.com/apppasswords) (exige verificação em duas etapas) e guarde as credenciais no *user-secrets* da API de Email, nunca no `appsettings.json`:

```bash
dotnet user-secrets set "Email:Usuario" "seu.email@gmail.com" --project backend/services/Email/Api
```

```bash
dotnet user-secrets set "Email:Senha" "senha-de-app-de-16-letras" --project backend/services/Email/Api
```

Sem isso a API funciona normalmente; o log avisa que o SMTP não está configurado e a mensagem vai para a fila `email-queue.dlq`.

**RabbitMQ.** O aviso passa por uma fila do RabbitMQ. Com o Docker Desktop aberto, suba o container uma vez (ele volta sozinho nas próximas vezes):

```bash
docker run -d --name rabbitmq --restart unless-stopped -p 5672:5672 -p 15672:15672 rabbitmq:4-management
```

O painel fica em http://localhost:15672 (usuário `guest`, senha `guest`). Sem o RabbitMQ a API sobe do mesmo jeito: os avisos esperam no banco e saem quando ele ligar.

### 3. Backend

Na primeira vez, confie no certificado HTTPS de desenvolvimento do .NET:

```bash
dotnet dev-certs https --trust
```

Depois, suba as duas APIs com o perfil `https`, cada uma num terminal:

```bash
dotnet run --project backend/services/Identidade/Api --launch-profile https
```

```bash
dotnet run --project backend/services/Estoque/Api --launch-profile https
```

```bash
dotnet run --project backend/services/Vendas/Api --launch-profile https
```

A API de Vendas sobe em `https://localhost:7012` (perfil `http`: `http://localhost:5266`). Ela lê o nome e o preço de uma cópia local dos produtos, que o Estoque mantém em dia pelo RabbitMQ. Por isso dá para registrar uma venda mesmo com o Estoque desligado: ela fica `Pendente` e é confirmada quando o Estoque voltar.

```bash
dotnet run --project backend/services/Pagamentos/Api --launch-profile https
```

A API de Pagamentos sobe em `https://localhost:7014` (perfil `http`: `http://localhost:5268`). O gateway é falso e não mexe em dinheiro. A cobrança nasce da própria venda: o `POST /api/v1/vendas` recebe um `tokenCartao` de teste (`tok_aprovado`, `tok_saldo_insuficiente`, `tok_fraude` ou `tok_gateway_fora`) no lugar do cartão. A venda passa por `Pendente` → `AguardandoPagamento` (estoque baixado) → `Confirmada`; se faltar estoque ou o pagamento for recusado, vira `Cancelada` e o estoque baixado é devolvido. O `GET /api/v1/pagamentos/venda/{vendaId}` mostra a cobrança de uma venda. O resultado chega como no mundo real: o gateway falso chama o webhook `POST /api/v1/webhooks/gateway-falso` com o corpo assinado (HMAC-SHA256 no cabeçalho `X-Assinatura`), e a API recusa com 401 qualquer aviso sem a assinatura certa. O segredo fica em `GatewayFalso:SegredoWebhook`.

```bash
dotnet run --project backend/services/Email/Api --launch-profile https
```

A API de Email sobe em `https://localhost:7016` (perfil `http`: `http://localhost:5270`). Ela não tem Swagger nem tela: só escuta a fila `email-queue` e manda o e-mail. O `/health` mostra se o banco e o RabbitMQ respondem.

A Identidade sobe em `https://localhost:7010` e o Estoque em `https://localhost:7008`, cada uma com o Swagger em `/swagger`. Login e cadastro ficam na Identidade; o token que ela devolve serve também no Estoque. Sem o `--launch-profile https`, o `dotnet run` usa o perfil `http` (Identidade em `http://localhost:5264`, Estoque em `http://localhost:5262`).

### 4. Frontend

O front fala só com o API Gateway, que repassa cada chamada para a API certa. Suba ele também:

```bash
dotnet run --project backend/services/Gateway/Api --launch-profile https
```

O gateway sobe em `https://localhost:7018` (perfil `http`: `http://localhost:5272`) e repassa `/identidade/...` para a Identidade, `/estoque/...` para o Estoque e `/vendas/...` para a Vendas. Ele concentra o CORS, limita requisições por IP (429 quando passa do limite, mais apertado no login) e esconde os endpoints `/api/v1/interno`. O Swagger continua na porta de cada API.

Crie `frontend/.env.local` com o endereço do gateway:

```
NEXT_PUBLIC_API_URL=https://localhost:7018
```

Login, cadastro e o nome do restaurante vão para a Identidade; produtos, categorias, fornecedores e movimentações, para o Estoque; a tela de Vendas, para a Vendas. A Pagamentos não é chamada pelo front: o cartão de teste vai junto com a venda.

Depois:

```bash
npm install --prefix frontend
```

```bash
npm run dev --prefix frontend
```

O app sobe em `http://localhost:3000`.

### 5. Primeiro acesso

Crie uma conta pela tela de cadastro (`/registrar`). O cadastro pede o nome do restaurante, cria o restaurante e já faz login.

Quem cria a conta da empresa em `/registrar` já nasce `Admin` do restaurante novo, com 14 dias de teste grátis. Para trocar a role de outro usuário do mesmo restaurante, use a tela **Usuários** (só Admin).

## Testes

```bash
dotnet test backend/GestaoRestaurante.slnx
```

Cobrem regras de domínio, Handlers (com banco em memória) e o isolamento de dados entre restaurantes.
