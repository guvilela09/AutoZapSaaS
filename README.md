# AutoZap SaaS

Plataforma SaaS de automação de WhatsApp com integração multi-plataforma (Kiwify, Hotmart, Nuvemshop) utilizando a Evolution API.

## Arquitetura

```
AutoZapSaaS/
├── AutoZapSaaS.Domain/       # Entidades, Enums, Regras de Negócio
├── AutoZapSaaS.Application/  # Casos de Uso, DTOs, Validators, Interfaces
├── AutoZapSaaS.Infrastructure/ # EF Core, SQL Server, Evolution API, Asaas
├── AutoZapSaaS.API/          # Controllers REST, Autenticação JWT, Swagger
├── AutoZapSaaS.Web/          # Painel do lojista (Razor Pages)
└── AutoZapSaaS.Tests/        # Testes de isolamento, cobrança e integrações
```

O painel consome a API por HTTP, como qualquer outro cliente: a API continua
sendo o produto e pode ser publicada sozinha.

## Planos

| Plano | Preço | Números de WhatsApp | Mensagens/mês |
|-------|-------|---------------------|---------------|
| Free | Grátis | 1 | 100 |
| Pro | R$ 97/mês | 3 | 5.000 |
| Business | R$ 297/mês | 10 | 50.000 |

Os limites ficam em código (`PlanCatalog`), não em tabela: um banco sem seed
liberaria acesso ilimitado. Cobrança recorrente pelo Asaas (Pix, boleto e cartão).
Estourar o limite responde **402**, com o plano sugerido no corpo — não 400, que
culparia o pedido do lojista.

## Funcionalidades

### Ouvinte (Webhook Receiver)
- Recebe webhooks de **Kiwify**, **Hotmart** e **Nuvemshop**
- Mapeia automaticamente os campos de cada plataforma
- Registra logs de todos os eventos recebidos

### Cerebro (Database & Rules)
- SQL Server com Entity Framework Core
- Multi-tenancy: cada empresa tem seus dados isolados
- Templates de mensagem personalizaveis por evento
- Historico completo de clientes e mensagens

### Disparador (WhatsApp Sender)
- Integracao com Evolution API
- Envio automatico de mensagens ao receber webhooks
- Gerenciamento de instancias WhatsApp (conectar/desconectar/QR Code)
- Fila de mensagens pendentes com reprocessamento

## Tecnologias

| Camada | Tecnologia |
|--------|-----------|
| API | ASP.NET Core 9.0 |
| Auth | JWT Bearer Token |
| ORM | Entity Framework Core 9.0 |
| Database | SQL Server |
| Mapping | AutoMapper |
| Validacao | FluentValidation |
| Mensageria | MassTransit + RabbitMQ |
| WhatsApp | Evolution API |
| Container | Docker + Docker Compose |

## Pre-requisitos

- .NET 9.0 SDK
- SQL Server (ou Docker)
- Evolution API (ou Docker)

## Setup Rapido com Docker

```bash
# Subir todos os servicos (SQL Server, RabbitMQ, Evolution API, AutoZap API)
docker-compose up -d

# Acessar Swagger
# http://localhost:5000/swagger
```

## Setup Manual

1. **Configurar SQL Server** e atualizar a connection string em `appsettings.json`
2. **Executar migrations:**
```bash
cd AutoZapSaaS.Infrastructure
dotnet ef database update --startup-project ../AutoZapSaaS.API
```
3. **Configurar Evolution API** e atualizar `EvolutionApi` no `appsettings.json`
4. **Rodar a API:**
```bash
cd AutoZapSaaS.API
dotnet run
```
5. **Acessar Swagger:** `https://localhost:7185/swagger`

## API Endpoints

### Autenticacao (Publico)
| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/auth/register` | Registrar nova empresa |
| POST | `/api/auth/login` | Login (retorna JWT) |

### Instancias WhatsApp (Autenticado)
| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/instances` | Criar instancia |
| GET | `/api/instances` | Listar instancias |
| GET | `/api/instances/{id}` | Detalhes da instancia |
| PUT | `/api/instances/{id}` | Atualizar instancia |
| DELETE | `/api/instances/{id}` | Remover instancia |
| POST | `/api/instances/{id}/connect` | Conectar (gera QR Code) |
| GET | `/api/instances/{id}/qrcode` | Obter QR Code em base64 |
| GET | `/api/instances/{id}/status` | Status da conexao |
| POST | `/api/instances/{id}/disconnect` | Desconectar |

### Clientes (Autenticado)
| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/customers` | Criar cliente |
| GET | `/api/customers` | Listar clientes |
| GET | `/api/customers/{id}` | Detalhes do cliente |
| GET | `/api/customers/phone/{phone}` | Buscar por telefone |
| PUT | `/api/customers/{id}` | Atualizar cliente |
| DELETE | `/api/customers/{id}` | Remover cliente |

### Mensagens (Autenticado)
| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/whatsapp/send` | Enviar mensagem |
| POST | `/api/whatsapp/send-template` | Enviar usando template |
| GET | `/api/whatsapp/messages` | Historico de mensagens |
| POST | `/api/whatsapp/process-pending` | Reprocessar fila |

### Templates (Autenticado)
| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/templates` | Criar template |
| GET | `/api/templates` | Listar templates |
| GET | `/api/templates/{id}` | Detalhes do template |
| PUT | `/api/templates/{id}` | Atualizar template |
| DELETE | `/api/templates/{id}` | Remover template |

### Integracoes de Webhook (Autenticado)
| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/webhook-integrations` | Criar integracao (retorna a URL de webhook) |
| GET | `/api/webhook-integrations` | Listar integracoes |
| POST | `/api/webhook-integrations/{id}/rotate-token` | Rotacionar o token da URL |
| PUT | `/api/webhook-integrations/{id}/secret` | Atualizar o secret de assinatura |
| DELETE | `/api/webhook-integrations/{id}` | Remover integracao |

### Webhooks (Publico, autenticado por token + assinatura)
| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/webhooks/kiwify/{token}` | Receber webhook Kiwify |
| POST | `/api/webhooks/hotmart/{token}` | Receber webhook Hotmart |
| POST | `/api/webhooks/nuvemshop/{token}` | Receber webhook Nuvemshop |

O `{token}` identifica o tenant e e gerado ao criar a integracao. Alem dele, a
requisicao precisa trazer a assinatura da plataforma, validada contra o secret do tenant:

| Plataforma | Onde vem a assinatura | Algoritmo |
|------------|----------------------|-----------|
| Kiwify | query string `?signature=` | HMAC-SHA1 do corpo, hex |
| Nuvemshop | header `x-linkedstore-hmac-sha256` | HMAC-SHA256 do corpo, hex |
| Hotmart | header `X-HOTMART-HOTTOK` | token estatico |

Token invalido ou assinatura invalida respondem `401` sem distincao entre os dois casos.

## Variaveis de Ambiente

Nenhum segredo fica versionado. A aplicacao **nao sobe** sem `Jwt__Secret` (min 32 chars)
e `ConnectionStrings__DefaultConnection` — falha no startup com mensagem explicita.

| Variavel | Descricao | Padrao |
|----------|-----------|--------|
| `ConnectionStrings__DefaultConnection` | String de conexao SQL Server | obrigatorio |
| `Jwt__Secret` | Chave secreta JWT (min 32 chars) | obrigatorio |
| `App__PublicBaseUrl` | URL publica, usada para montar as URLs de webhook | - |
| `EvolutionApi__BaseUrl` | URL da Evolution API | `http://localhost:8080` |
| `EvolutionApi__ApiKey` | API Key da Evolution | - |

Para Docker, copie `.env.example` para `.env` e preencha. Para desenvolvimento local:

```bash
cd AutoZapSaaS.API
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;"
```

## Isolamento Multi-Tenant

Toda entidade que implementa `ITenantEntity` recebe um **Global Query Filter** no
`ApplicationDbContext`, amarrado ao tenant da requisicao (claim `tenant_id` do JWT).
Consultas nao conseguem atravessar a fronteira do tenant nem quando recebem o `Id` de
um recurso alheio. Atravessar exige um `IgnoreQueryFilters()` explicito — usado apenas
no login, no cadastro e na resolucao de token de webhook.

Os testes em `AutoZapSaaS.Tests/TenantIsolationTests.cs` exercitam esse ataque.

## Fluxo de Webhook

1. O tenant cria uma integracao e recebe a URL `POST /api/webhooks/{plataforma}/{token}`
2. Cadastra essa URL e o secret na plataforma de vendas
3. A plataforma envia o webhook assinado
4. Sistema resolve o tenant pelo token e **valida a assinatura HMAC sobre o corpo cru**
5. So entao a requisicao ganha um tenant e o processamento comeca
6. Extrai nome e telefone, cria/atualiza cliente, busca template ativo
7. Envia mensagem WhatsApp via Evolution API e registra o log

## Estrutura de Templates

Templates usam `{{nome}}` e `{{detalhe}}` como placeholders:

```
"Olá {{nome}}! Seu pagamento foi confirmado. {{detalhe}}"
```
