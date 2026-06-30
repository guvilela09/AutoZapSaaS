# AutoZap SaaS

Plataforma SaaS de automação de WhatsApp com integração multi-plataforma (Kiwify, Hotmart, Nuvemshop) utilizando a Evolution API.

## Arquitetura

```
AutoZapSaaS/
├── AutoZapSaaS.Domain/       # Entidades, Enums, Regras de Negócio
├── AutoZapSaaS.Application/  # Casos de Uso, DTOs, Validators, Interfaces
├── AutoZapSaaS.Infrastructure/ # EF Core, SQL Server, Evolution API Client
└── AutoZapSaaS.API/          # Controllers REST, Autenticação JWT, Swagger
```

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

### Webhooks (Publico)
| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/webhooks/kiwify` | Receber webhook Kiwify |
| POST | `/api/webhooks/hotmart` | Receber webhook Hotmart |
| POST | `/api/webhooks/nuvemshop` | Receber webhook Nuvemshop |
| POST | `/api/webhooks/receive` | Webhook generico |

## Variaveis de Ambiente

| Variavel | Descricao | Padrao |
|----------|-----------|--------|
| `ConnectionStrings__DefaultConnection` | String de conexao SQL Server | - |
| `Jwt__Secret` | Chave secreta JWT (min 32 chars) | - |
| `EvolutionApi__BaseUrl` | URL da Evolution API | `http://localhost:8080` |
| `EvolutionApi__ApiKey` | API Key da Evolution | - |

## Fluxo de Webhook

1. Plataforma externa (Kiwify/Hotmart/Nuvemshop) envia webhook `POST /api/webhooks/{plataforma}`
2. Sistema extrai nome e telefone do payload
3. Cria ou atualiza cliente no banco de dados
4. Busca template de mensagem ativo para o tipo de evento
5. Envia mensagem WhatsApp via Evolution API
6. Registra log do envio

## Estrutura de Templates

Templates usam `{{nome}}` e `{{detalhe}}` como placeholders:

```
"Olá {{nome}}! Seu pagamento foi confirmado. {{detalhe}}"
```
