# CS Sistemas

Sistema completo de agendamentos para profissionais (SaaS): o profissional cadastra negócio, serviços e horários; o cliente agenda pela página pública (link único por negócio) e pode pagar por PIX, cartão ou Mercado Pago. Desenvolvido em .NET 8 e React (TypeScript + Vite).

## Funcionalidades

- Autenticação JWT (login, registro, recuperação e redefinição de senha por e-mail)
- CRUD de negócios, serviços, clientes e agendamentos
- Agendamento público: link por negócio (/agendar/:slug), escolha de serviço e horário, cancelamento por link
- Pagamentos: PIX, cartão e Mercado Pago; webhooks para confirmação
- Planos/assinatura (Mensal, 6 meses, 1 ano)
- Dashboard com resumo, ganhos e detalhes por período
- Notificações (ex.: novo agendamento) e mensagens do sistema (templates)
- Área admin restrita (usuários, negócios, clientes)
- API REST com Swagger
- Interface responsiva (React + CSS)
- Scripts de backup (PostgreSQL)

## Tecnologias

### Backend

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- FluentValidation, Swagger

### Frontend

- React 18
- TypeScript
- Vite
- React Router

## Pré-requisitos

- .NET 8 SDK
- Node.js (para o frontend)
- PostgreSQL

## Como Executar

### 1. Backend (API)

```bash
cd CSSistemas.API
dotnet restore
dotnet run
```

A API estará disponível em:

- **HTTPS:** conforme launchSettings.json (ex.: https://localhost:7xxx)
- **Swagger:** https://localhost:7xxx/swagger (ajuste a porta se necessário)

### 2. Frontend (React)

```bash
cd cssistemas-web
npm install
npm run dev
```

O frontend estará disponível em:

- **URL:** http://localhost:5173

### 3. Configuração e banco

- Crie o banco PostgreSQL e execute os scripts em **CSSistemas.API/Scripts** (001 a 018) na ordem.
- **Em desenvolvimento:** copie `CSSistemas.API/appsettings.Development.example.json` para `appsettings.Development.json` e preencha (connection string, JWT, admin, e-mail, pagamento). Não commite esse arquivo.
- **Em produção:** use variáveis de ambiente para connection string, JWT, admin e chaves de pagamento.

## Estrutura do Projeto

```
CSSistemas/
├── CSSistemas.API/           # Web API
├── CSSistemas.Application/   # DTOs, serviços, validações
├── CSSistemas.Domain/        # Entidades e enums
├── CSSistemas.Infrastructure/ # EF, repositórios, e-mail, etc.
├── cssistemas-web/           # Frontend React
├── CSSistemas.sln
├── Dockerfile
└── README.md
```

## Configuração do Banco

- **Servidor:** conforme connection string em appsettings ou variável de ambiente.
- **Banco:** nome definido na connection string (ex.: CSSistemas).
- **Criação:** scripts em **CSSistemas.API/Scripts** (001_create_users.sql até 018_...). Rodar na ordem.
- **Backup:** Scripts/backup/.

## Endpoints da API

### Autenticação (/api/auth)

- POST /api/auth/login — Login
- POST /api/auth/register — Registro
- POST /api/auth/forgot-password — Solicitar redefinição de senha
- POST /api/auth/reset-password — Redefinir senha
- GET /api/auth/me — Usuário logado
- POST /api/auth/profile-photo — Foto de perfil

### Negócios (/api/business)

- GET /api/business — Listar negócios do usuário
- GET /api/business/{id} — Buscar por ID
- POST /api/business — Criar negócio
- PUT /api/business/{id} — Atualizar
- DELETE /api/business/{id} — Excluir
- GET /api/business/{id}/hours — Horário de funcionamento
- PUT /api/business/{id}/hours — Atualizar horário

### Serviços (/api/services)

- GET /api/services/by-business/{businessId} — Listar por negócio
- GET /api/services/{id} — Buscar por ID
- POST /api/services — Criar (associado ao negócio)
- PUT /api/services/{id} — Atualizar
- DELETE /api/services/{id} — Excluir

### Agendamentos (/api/appointments)

- GET /api/appointments/by-business/{businessId} — Listar com filtro e paginação
- GET /api/appointments/{id} — Buscar por ID
- POST /api/appointments — Criar
- DELETE /api/appointments/{id} — Excluir/cancelar

### Clientes (/api/clients)

- GET /api/clients/by-business/{businessId} — Listar por negócio
- GET /api/clients/{id} — Buscar por ID
- POST /api/clients — Criar
- PUT /api/clients/{id} — Atualizar
- DELETE /api/clients/{id} — Excluir

### Agendamento público (sem autenticação) (/api/public/booking)

- GET /api/public/booking/{slug} — Dados do negócio pelo slug
- GET /api/public/booking/{slug}/services — Serviços disponíveis
- GET /api/public/booking/{slug}/slots — Horários disponíveis
- POST /api/public/booking/{slug}/appointments — Criar agendamento
- POST /api/public/booking/cancelar — Cancelar agendamento (token)

### Dashboard

- GET /api/dashboard/summary — Resumo (agendamentos, ganhos, etc.)
- GET /api/dashboard/earnings — Ganhos
- GET /api/dashboard/earnings-detail — Detalhe de ganhos por período

### Pagamento (/api/payment)

- GET /api/payment/card — Configuração cartão
- GET /api/payment/pix-enabled — Se PIX está ativo
- POST /api/payment/pix — Gerar cobrança PIX
- POST /api/payment/mercado-pago/checkout — Checkout Mercado Pago

### Planos e assinatura

- GET /api/plans — Listar planos
- GET /api/subscription/status — Status da assinatura do usuário

### Admin (/api/admin) — Requer role admin

- GET /api/admin/users — Usuários
- GET /api/admin/businesses — Negócios
- GET /api/admin/clients/by-business/{businessId} — Clientes por negócio

### Outros

- GET /api/notifications — Notificações do usuário
- GET /api/systemmessages/by-business/{businessId} — Mensagens/templates; POST, PUT, DELETE
- GET /api/health/db — Health check do banco
- POST /api/webhook/mercadopago — Webhook Mercado Pago
- POST /api/webhooks/whatsapp — Webhook WhatsApp

## Segurança

- Tokens JWT com expiração configurável (ex.: 60 minutos)
- Senhas com hash seguro
- Validação de entrada (FluentValidation)
- CORS configurado para desenvolvimento
- **Dados sensíveis protegidos:**
  - O repositório contém apenas **placeholders** em `appsettings.json` (sem senhas, JWT ou chaves reais).
  - Crie `appsettings.Development.json` a partir de `appsettings.Development.example.json` e preencha com suas credenciais locais. Esse arquivo **não é versionado** (está no `.gitignore`).
  - Em produção, use variáveis de ambiente ou `appsettings.Production.json` (também ignorado pelo Git) para connection string, JWT, admin, PIX, Mercado Pago e e-mail.
  - **Se você já commitou credenciais no passado:** altere imediatamente a senha do banco, o JWT Secret, a senha do admin e regenere chaves de API (Resend, Mercado Pago, etc.).
