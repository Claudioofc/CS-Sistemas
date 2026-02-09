# CS Sistemas — Apresentação do Projeto

Sistema completo de agendamentos **multi-nicho** (clínicas, salões, consultorias, aulas etc.): painel para o profissional e agendamento público para o cliente final.

---

## 1. O que o sistema faz

- **Profissional** se cadastra, cria **negócios** (empresas/unidades), cadastra **serviços** (nome, duração, preço), **clientes** e **horários de funcionamento**.
- Gera um **link público de agendamento** (ex.: `/agendar/minha-clinica`). O **cliente** abre o link, escolhe serviço e horário e agenda **sem login**.
- O profissional vê **agendamentos** no painel, pode **confirmar, cancelar ou excluir**. Ao cancelar, pode informar motivo; o cliente recebe e-mail.
- O cliente pode **cancelar pelo link** no e-mail de confirmação (token único).
- **Notificações** (sino) no painel para novo agendamento ou cancelamento pelo cliente; opção de **som** (beep).
- **Planos/assinatura:** Free 3d, planos pagos (PIX e cartão via Mercado Pago), middleware que bloqueia acesso ao painel se sem acesso.
- **Mensagens do sistema** (templates por negócio) para uso em lembretes ou integrações.
- **Webhook WhatsApp** (opcional): recebe mensagem, IA sugere horários, cliente confirma por texto e o sistema cria o agendamento.
- **Admin:** painel separado para gestão; rotas protegidas por role.

---

## 2. Stack técnica

| Camada | Tecnologia |
|--------|------------|
| Backend | .NET 8, ASP.NET Core, Clean Architecture |
| Banco | PostgreSQL, EF Core, Npgsql |
| Frontend | React 18, TypeScript, Vite, TailwindCSS, React Router |
| Auth | JWT (login, registro, esqueci/redefinir senha) |
| E-mail | SMTP ou Resend |
| Pagamento | Mercado Pago (PIX + Checkout Pro) |
| Opcional | OpenAI, Redis (multi-instância) |

---

## 3. Arquitetura do backend

- **API:** Controllers, pipeline (rate limit, CORS, auth, exceções), health, Swagger.
- **Application:** DTOs, interfaces, validadores (FluentValidation), CommException, helpers.
- **Domain:** Entidades (User, Business, Service, Appointment, Client, Plan, Subscription, etc.) e enums.
- **Infrastructure:** EF Core, repositórios, AuthService, e-mail (SMTP/Resend), fila de e-mail, store WhatsApp (Redis/InMemory).

Validação só no backend; erros padronizados (CommException); alto volume: rate limit, pool, fila de e-mail, Redis opcional.

---

## 4. Frontend — rotas principais

| Rota | Descrição |
|------|------------|
| `/login` | Login |
| `/criar-conta` | Registro |
| `/esqueci-senha` | Solicitar link de redefinição |
| `/redefinir-senha` | Redefinir senha com token do e-mail |
| `/dashboard` | Resumo (cards, agenda do dia, próximos, ganhos do mês) |
| `/agendamentos` | Lista de agendamentos (filtro, paginação, status, cancelar/excluir) |
| `/servicos` | CRUD de serviços por negócio |
| `/clientes` | CRUD de clientes por negócio |
| `/configuracoes` | Perfil (nome, foto, documento), negócios (nome, tipo, link de agendamento, WhatsApp) |
| `/planos` | Assinatura (Free 3d, planos pagos, PIX/cartão) |
| `/agendar/:slug` | Agendamento público (sem login) |
| `/agendar/cancelar` | Cancelamento pelo link do e-mail (token) |
| `/admin` | Painel admin (protegido por role) |

Constantes em `constants.ts` (APP_NAME, ROUTES, NEGOCIO_SINGULAR/PLURAL) para textos e rotas multinicho. Build do front é copiado para `CSSistemas.API/wwwroot` para deploy único (API + SPA).

---

## 5. Principais entidades (Domain)

- **User** — e-mail, senha (hash), nome, foto, documento (CPF/CNPJ), admin.
- **Business** — nome, tipo, slug público, WhatsApp; pertence a um User.
- **BusinessHours** — horário de funcionamento por dia da semana.
- **Service** — nome, duração, preço; por Business; soft delete.
- **Client** — nome, telefone, e-mail, notas; por Business; soft delete.
- **Appointment** — negócio, serviço, cliente, data/hora (UTC), status, token de cancelamento, notas.
- **Notification** — tipo (novo agendamento, cancelamento pelo cliente), lida ou não.
- **Plan** / **Subscription** — planos de assinatura e vínculo do usuário.
- **SystemMessage** — templates de mensagem por negócio (chave, título, corpo).

---

## 6. Segurança e qualidade

JWT, rotas protegidas, validação FluentValidation, CommException + pipeline, rate limit 120/min por IP, CORS, HTTPS em produção.

---

## 7. Documentação

README.md (visão geral, como rodar), DEPLOY.md (checklist), ALTO-VOLUME.md (rate limit, pool, Redis, fila), METRICAS-PRODUCAO.md (métricas sugeridas), CONFIGURAR-CARTAO.md, CONFIGURAR-EMAIL.md.

---

## 8. Estado atual

- **Funcional:** cadastro, login, negócios, serviços, clientes, agendamento público e pelo painel, cancelamento (pelo link do e-mail e pelo profissional com motivo), notificações (sino + beep), planos (Free 3d + PIX/cartão), mensagens do sistema, webhook WhatsApp opcional, admin.
- **Código:** DRY (validação, erros, rate limit, pool, store WhatsApp, fila de e-mail); validações apenas no backend; pipeline e extensões organizados.
- **Produção:** preparado para alto volume (rate limit, pool, fila de e-mail, Redis opcional, cache de estáticos); métricas documentadas para monitorar suporte e velocidade.

O sistema está completo, organizado e pronto para uso e evolução em ambiente single ou multi-instância (com Redis).

