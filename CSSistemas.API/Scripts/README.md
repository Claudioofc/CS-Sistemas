# Scripts SQL do banco (PostgreSQL)

Scripts executados **automaticamente na inicialização da API**, em ordem alfabética (prefixo numérico). O executor está em `CSSistemas.Infrastructure.Data.DatabaseScriptRunner` e é chamado no startup da aplicação.

| Arquivo | Descrição |
|---------|-----------|
| `001_create_users.sql` | Tabela Users (usuários do sistema). |
| `002_create_businesses.sql` | Tabela Businesses (negócios do cliente SaaS). |
| `003_create_services.sql` | Tabela Services (serviços do negócio). |
| `004_create_appointments.sql` | Tabela Appointments (agendamentos). |
| `005_create_subscriptions.sql` | Tabela Subscriptions (trial/plano pago). |
| `006_create_plans.sql` | Tabela Plans (planos de assinatura). |
| `007_alter_soft_delete.sql` | Colunas IsDeleted e DeletedAt nas tabelas principais. |
| `008_alter_users_isadmin.sql` | Coluna IsAdmin na tabela Users. |
| `009_alter_users_document.sql` | Garante todas as colunas em Users (ProfilePhotoUrl, DocumentType, DocumentNumber, ResetToken, ResetTokenExpiresAt, IsAdmin, IsDeleted, DeletedAt). Evita 42703. |
| `010_create_clients.sql` | Tabela Clients (pacientes/clientes do negócio). |
| `011_create_system_messages.sql` | Tabela SystemMessages (templates de mensagens). |
| `012_create_business_hours.sql` | Tabela BusinessHours (horários de funcionamento por dia da semana). |
| `013_alter_businesses_whatsapp.sql` | Colunas WhatsAppPhone e PublicSlug na tabela Businesses. |
| `014_create_one_time_fixes_table.sql` | Tabela _OneTimeFixes (registro de correções one-time). |
| `015_alter_users_lockout.sql` | Colunas FailedLoginAttempts e LockoutEnd na tabela Users (bloqueio após 3 tentativas). |
| `016_create_notifications.sql` | Tabela Notifications (notificações para o usuário, ex.: novo agendamento). |
| `017_alter_appointments_cancel_token.sql` | Coluna CancelToken em Appointments (link de cancelamento no e-mail). |

Todos usam `CREATE TABLE IF NOT EXISTS` ou `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` para serem idempotentes.

**Correção automática (uma vez):** Na subida da API, o `OneTimeFixRunner` corrige agendamentos gravados com horário Brasil como UTC (soma 3h). Roda apenas uma vez; o registro fica em `_OneTimeFixes`.

**Importante:** Garanta que a connection string (`appsettings.json` ou variável de ambiente `ConnectionStrings__DefaultConnection`) esteja correta para o ambiente. Os scripts rodam na primeira requisição/startup que acessa o banco.
