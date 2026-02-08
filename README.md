# CS Sistemas

SaaS de agendamentos multi-nicho: painel para profissionais e agendamento público para clientes.

## Stack

- **Backend:** .NET 8, Clean Architecture, EF Core, PostgreSQL
- **Frontend:** React, TypeScript, Vite, TailwindCSS
- **Integrações:** WhatsApp (opcional), OpenAI para chat (opcional)

## Estrutura

| Pasta | Descrição |
|-------|-----------|
| `CSSistemas.API` | API ASP.NET Core (controllers, Program.cs, Scripts SQL) |
| `CSSistemas.Application` | Casos de uso, DTOs, validadores, interfaces |
| `CSSistemas.Domain` | Entidades e enums |
| `CSSistemas.Infrastructure` | EF Core, repositórios, serviços (auth, e-mail, IA, WhatsApp) |
| `cssistemas-web` | Frontend React (painel + página pública de agendamento) |

## Como rodar

1. **PostgreSQL:** Crie o banco e ajuste a connection string em `CSSistemas.API/appsettings.json` (ou `appsettings.Development.json`).
2. **API:** Na pasta da solução: `dotnet run --project CSSistemas.API`. Os scripts SQL em `CSSistemas.API/Scripts/` rodam automaticamente na subida.
3. **Frontend (dev):** Em `cssistemas-web`: `npm install` e `npm run dev`. Aponte a URL da API no `.env` (veja `.env.example`).

## Como seu cliente envia o link pelo WhatsApp (para testar)

Depois que o profissional se cadastra e configura o negócio:

1. Acesse **Configurações** no painel.
2. Em **Link de agendamento**, defina um endereço único (ex.: `minha-clinica`).
3. Clique em **Salvar** e depois em **Copiar link**.
4. Cole o link no WhatsApp (ou em qualquer app) e envie para o cliente final — ele abre o link e agenda sem precisar de login.

## Deploy

Use o checklist em **[DEPLOY.md](DEPLOY.md)** (banco, BaseBookingUrl, JWT, WhatsApp, OpenAI, etc.).

## Scripts do banco

Documentação dos scripts SQL em **[CSSistemas.API/Scripts/README.md](CSSistemas.API/Scripts/README.md)**.
