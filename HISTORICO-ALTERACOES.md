# Histórico de alterações – CS Sistemas

Documento com tudo que foi feito no projeto para não perder nada. Atualizado em 03/02/2026.

---

## 1. Autenticação e perfil

- Telas: Login, Cadastro, Esqueci senha, Redefinir senha, Perfil.
- Upload de foto de perfil.
- **CPF/CNPJ:** Frontend (`Configuracoes.tsx`) aceita dígitos e formatação (`.`, `/`, `-`). Backend valida quantidade de dígitos (11 CPF, 14 CNPJ) e grava só dígitos.

---

## 2. Negócios (estabelecimentos)

- CRUD de `Business` (nome, tipo, slug público, WhatsApp).
- **Horários de funcionamento:** API `GET/PUT /api/business/{id}/hours`, UI em Configurações.
- **Link de agendamento público:** Profissional define `publicSlug` (ex.: `minha-clinica`) em Configurações. Link exibido (ex.: `https://seusite.com/agendar/minha-clinica`) com botão **Copiar link**. Instrução na tela: como enviar pelo WhatsApp para testar.
- README com seção “Como seu cliente envia o link pelo WhatsApp”.

---

## 3. Serviços

- CRUD de `Service` (nome, duração, preço, ativo). Tela Serviços no painel.
- Aviso na tela informando que nome, duração e preço são usados pela IA no WhatsApp.

---

## 4. Clientes (pacientes)

- CRUD de `Client`. Página Clientes no painel (já existia, verificada como funcional).

---

## 5. Mensagens do sistema

- CRUD de `SystemMessage` (templates). Página Mensagens no painel.

---

## 6. Agendamentos

- **API:** `AppointmentsController` – listar, obter, criar, atualizar status, deletar (soft delete).
- **Página pública:** `AgendarPublico.tsx` – cliente final agenda pelo link público (sem login).
- **Painel:** `Agendamentos.tsx` – profissional vê, filtra, altera status e exclui agendamentos.

---

## 7. Disponibilidade e horários

- **Fuso Brasil:** `AvailabilityService` usa `DateOnly` e `TimeZoneInfo` (Brasil) para gerar slots em UTC corretamente (evita 11:30 vs 14:30).
- **Slots com disponibilidade:** API retorna `GetSlotsWithAvailabilityAsync` (cada slot com `available: true/false`). Frontend mostra **todos** os horários do dia; **ocupados em vermelho** e desabilitados, livres clicáveis.
- **Conflito:** `HasConflictAsync` no repositório de agendamentos; ocupados não aparecem como disponíveis.
- **Gravação em UTC:** No `PublicBookingController`, horário recebido sem "Z" é tratado como Brasília e convertido para UTC (`ToUtcFromRequest`).
- **Correção one-time:** Script `014_create_one_time_fixes_table.sql` (tabela `_OneTimeFixes`) e `OneTimeFixRunner.RunAppointmentTimezoneFixAsync` na subida da API – soma 3h em agendamentos gravados com horário Brasil como UTC. Roda só uma vez (registro em `_OneTimeFixes`).

---

## 8. WhatsApp e IA

- **Webhook:** `WebhooksController` recebe mensagens do WhatsApp.
- **OpenAIChatService:** Recebe lista de serviços (nome, duração, preço) para responder com dados reais. Prompt com regras para não inventar valores. Linguagem neutra (“negócio”, “estabelecimento”). Sugestão e confirmação de slot: IA sugere horários; se cliente confirmar, sistema cria agendamento. Store em memória `PendingWhatsAppSlot`.
- **Stub:** `WhatsAppSenderStub`; substituir por `IWhatsAppSender` real (Z-API, Twilio, etc.) quando for usar em produção.

---

## 9. Banco de dados

- **Scripts 010–013:** `010_create_clients.sql`, `011_create_system_messages.sql`, `012_create_business_hours.sql`, `013_alter_businesses_whatsapp.sql`.
- **Script 014:** `014_create_one_time_fixes_table.sql` – tabela para correções one-time.
- **Execução automática:** Scripts rodam na inicialização da API (`DatabaseScriptRunner`). Documentação em `CSSistemas.API/Scripts/README.md`.
- Pasta `Scripts/manual/` removida (correção de fuso feita automaticamente).

---

## 10. Configuração e deploy

- **appsettings.Production.json:** Criado com placeholders (ConnectionString, Jwt, Email, Payment, BaseBookingUrl, OpenAI, WhatsApp).
- **DEPLOY.md:** Checklist de deploy (banco, API, frontend, WhatsApp, OpenAI, pagamento, segurança). Seção “Pagamento com cartão” com instruções Mercado Pago.
- **.gitignore** na raiz: bin, obj, .vs, node_modules, secrets.json.
- **README.md** na raiz: descrição do projeto, stack, estrutura de pastas, como rodar, link para DEPLOY e Scripts/README.

---

## 11. Premium e planos

- **Rota /premium:** Passa a exibir a mesma página de Planos (não mais “Em breve”). Título “Premium” e texto “Assine um plano para desbloquear todos os recursos” quando acessado por /premium.
- **Componente Placeholder** removido de `App.tsx` (não usado).

---

## 12. Pagamento com cartão e PIX

- **Modal de forma de pagamento:** Ao clicar “Assine agora” no plano, abre modal com:
  - **Assine agora** (cartão) – redireciona para Checkout Pro do Mercado Pago.
  - **PIX** – fecha o modal e exibe bloco de PIX só para aquele plano.
- **PIX:** Não aparece mais fixo no rodapé; só quando o cliente escolhe PIX no modal. Bloco com valor do plano, QR code (chave PIX) e botão “Copiar chave”. Texto explicando: escanear QR no app do banco ou copiar chave e colar no app informando o valor.
- **Cartão:** `PaymentController` considera token válido só se não for placeholder (`ACCESS_TOKEN_AQUI`, `COLE_SEU`). Checkout Pro aceita todas as bandeiras (Visa, Mastercard, Elo, Hipercard, etc.).
- **CONFIGURAR-CARTAO.md:** Passo a passo completo: criar aplicação no Mercado Pago (4 passos), Credenciais de teste/produção, colar Access Token em `appsettings.Development.json`, reiniciar API.
- **appsettings.Development.json:** Seção `Payment` com `Card.Enabled`, `MercadoPago` (AccessToken, SuccessUrl, FailureUrl, PendingUrl). Placeholder `COLE_SEU_ACCESS_TOKEN_AQUI` substituído pelo token quando fornecido.
- **Produção:** URLs localhost trocadas pelo domínio real; token por variáveis de ambiente recomendado.

---

## 13. Ajustes de texto e UI

- Botão do cartão no modal: texto alterado de “Cartão (todas as bandeiras)” para **“Assine agora”** (loading continua “Abrindo...”).
- Texto do modal: “Qualquer cartão de crédito ou débito (Visa, Mastercard, Elo, Hipercard e outros). Pagamento seguro.”

---

## 14. Erros corrigidos ao longo do trabalho

- Typo/variável em `WebhooksController.cs` (pendingSlot).
- Sintaxe TypeScript em `Configuracoes.tsx` (mistura de `&&` e `??`).
- Placeholder `Placeholder` não utilizado em `App.tsx` (TS6133) – componente removido.

---

## 15. Estrutura de pastas (referência)

```
CS Sistemas/
├── cssistemas-web/          # Frontend React (Vite, Tailwind)
├── CSSistemas.API/          # API ASP.NET Core, Controllers, Scripts, CONFIGURAR-CARTAO.md
├── CSSistemas.Application/  # DTOs, Validators, Interfaces
├── CSSistemas.Domain/       # Entidades, Enums
├── CSSistemas.Infrastructure/ # EF Core, Repositories, Services (Auth, Availability, OpenAI, WhatsAppStub), Data (DbContext, ScriptRunner, OneTimeFixRunner, Seeds)
├── DEPLOY.md
├── HISTORICO-ALTERACOES.md  # Este arquivo
├── README.md
└── .gitignore
```

---

## 16. Como rodar (resumo)

1. **PostgreSQL:** Connection string em `appsettings.json` ou `appsettings.Development.json`.
2. **API:** `dotnet run --project CSSistemas.API` (scripts SQL e one-time fix rodam na subida).
3. **Frontend:** `cd cssistemas-web` → `npm install` → `npm run dev`. URL da API em `.env` (VITE_API_URL).
4. **Cartão:** Access Token do Mercado Pago em `Payment:MercadoPago:AccessToken` (ver CONFIGURAR-CARTAO.md).

---

*Este histórico foi gerado para preservar todas as alterações feitas no projeto. Atualize este arquivo quando fizer novas mudanças relevantes.*

