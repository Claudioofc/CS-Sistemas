# CS Sistemas

SaaS de agendamentos: painel para profissionais e agendamento público para clientes.

**Stack:** .NET 8, React, TypeScript, Vite, PostgreSQL.

## Como rodar

1. PostgreSQL: crie o banco. Connection string, JWT, admin e chaves: use variáveis de ambiente em produção; em desenvolvimento, copie `CSSistemas.API/appsettings.Development.example.json` para `appsettings.Development.json` e preencha (não commite esse arquivo).
2. API: `dotnet run --project CSSistemas.API`
3. Frontend: em `cssistemas-web` → `npm install` e `npm run dev`
