# Configurar envio de e-mail (Esqueci minha senha)

O link de redefinição de senha **só é enviado por e-mail** quando há um provedor configurado (Resend ou SMTP). Caso contrário, a API apenas registra o link no log (para uso em desenvolvimento).

---

## Opção mais prática: Resend (recomendado para o seu cliente)

**Uma chave de API** — sem senha de app, sem configurar Gmail/Yahoo.

1. Crie uma conta em **https://resend.com** (plano gratuito: 3.000 e-mails/mês).
2. Em **API Keys**, crie uma chave e copie (começa com `re_`).
3. No `appsettings`, na seção **Email**, use:
   - **Provider:** `"Resend"`
   - **ResendApiKey:** `"re_sua_chave_aqui"`
   - **PasswordResetBaseUrl:** URL do seu front (ex.: `https://localhost:5173`).
4. (Opcional) Em **Domains** no Resend, adicione e verifique seu domínio para enviar de `noreply@seudominio.com`. Sem isso, o Resend envia de `onboarding@resend.dev` (funciona para testes).
5. Reinicie a API.

O e-mail de redefinição chega para **qualquer e-mail cadastrado** (Gmail, Yahoo, Outlook, etc.). O cliente só precisa dessa chave; não mexe em senha de app.

---

## Uma configuração para todos os e-mails

O sistema **já está preparado** para enviar o link de redefinição para **qualquer e-mail cadastrado**, independente do provedor (Gmail, Yahoo, Outlook, Hotmail, etc.).

- **Você configura só uma vez:** uma única conta SMTP (remetente) — pode ser Gmail, Yahoo, Outlook ou outro; escolha a que preferir.
- **Essa mesma configuração** envia o e-mail para **todos** os usuários que pedirem “Esqueci minha senha”, seja o e-mail deles @gmail.com, @yahoo.com.br, @outlook.com ou qualquer outro.
- **Não é necessário** configurar nada por provedor: um único SMTP atende a todos.

**Resumo:** Configure **uma** conta que envia (remetente). Quem recebe é sempre o e-mail cadastrado (qualquer provedor).

---

## Como ativar (SMTP: Gmail, Yahoo, Outlook)

1. Escolha **uma conta de e-mail** para ser o **remetente** (ex.: Yahoo, Gmail ou Outlook).
2. Obtenha os dados SMTP dessa conta (veja exemplos por provedor mais abaixo).
3. Preencha a seção **Email** em `appsettings.Development.json` (ou User Secrets / variáveis em produção):
   - **Provider:** `"Smtp"`.
   - **SmtpHost**, **SmtpPort**, **SmtpUser**, **SmtpPassword**, **FromEmail**.
   - **PasswordResetBaseUrl:** URL do frontend (ex.: `https://localhost:5173`).
4. Reinicie a API.

Depois disso, quando alguém clicar em **Esqueci minha senha** e informar um **e-mail cadastrado** (qualquer um: @gmail.com, @yahoo.com.br, @outlook.com, etc.), o link de redefinição será enviado **para esse e-mail**.

---

## Onde configurar

- **Desenvolvimento:** `appsettings.Development.json` ou User Secrets.
- **Produção:** variáveis de ambiente ou `appsettings.Production.json` (evite senhas no arquivo).

## Seção no appsettings

Adicione ou preencha a seção `Email`:

**Com Resend (recomendado):**
```json
{
  "Email": {
    "Provider": "Resend",
    "PasswordResetBaseUrl": "https://localhost:5173",
    "ResendApiKey": "re_sua_chave_aqui",
    "FromEmail": "",
    "FromName": "CS Sistemas"
  }
}
```

**Com SMTP (Gmail/Yahoo/Outlook):**
```json
{
  "Email": {
    "Provider": "Smtp",
    "PasswordResetBaseUrl": "https://localhost:5173",
    "SmtpHost": "smtp.seudominio.com",
    "SmtpPort": 587,
    "SmtpUser": "seu-email@seudominio.com",
    "SmtpPassword": "SUA_SENHA_OU_APP_PASSWORD",
    "FromEmail": "seu-email@seudominio.com",
    "FromName": "CS Sistemas"
  }
}
```

- **PasswordResetBaseUrl:** URL do frontend (onde o usuário clica no link). Ex.: `https://localhost:5173` em dev ou `https://app.seudominio.com` em produção.
- **SmtpHost:** Servidor SMTP. Se estiver vazio, o e-mail **não é enviado** (só aparece no log da API).
- **SmtpPort:** 587 (TLS) ou 465 (SSL).
- **SmtpUser / SmtpPassword:** Login do servidor. Em Gmail/Yahoo use **senha de app**, não a senha normal.
- **FromEmail:** E-mail remetente (quem aparece como “De”).

## Exemplos por provedor (conta remetente)

O e-mail de redefinição vai **sempre para o e-mail cadastrado** (qualquer provedor). Abaixo, a conta que **envia** (remetente) — pode ser qualquer uma.

### Gmail

1. Ative “Acesso a app menos seguro” ou use **Senha de app** (recomendado): Google Conta → Segurança → Verificação em 2 etapas → Senhas de app.
2. Use:
   - **SmtpHost:** `smtp.gmail.com`
   - **SmtpPort:** 587
   - **SmtpUser:** seu@gmail.com
   - **SmtpPassword:** a senha de app de 16 caracteres

### Yahoo (incluindo @yahoo.com.br)

1. Crie uma **Senha de app**: Yahoo → Conta → Segurança → Gerar senha de app.
2. Use:
   - **SmtpHost:** `smtp.mail.yahoo.com`
   - **SmtpPort:** 587 (ou 465 para SSL)
   - **SmtpUser:** seu@yahoo.com.br
   - **SmtpPassword:** a senha de app gerada
   - **FromEmail:** seu@yahoo.com.br

### Outlook / Microsoft 365

- **SmtpHost:** `smtp.office365.com`
- **SmtpPort:** 587
- **SmtpUser:** seu@outlook.com (ou domínio corporativo)
- **SmtpPassword:** senha da conta ou senha de app

### Outro SMTP

Use host, porta, usuário e senha fornecidos pelo seu provedor de e-mail ou serviço (SendGrid, Mailgun, etc.). Muitos usam porta 587 (TLS) ou 465 (SSL).

## Erro "5.7.0 Authentication Required" ou "secure connection"

- **Gmail:** use **senha de app** (16 caracteres), não a senha normal da conta. Google Conta → Segurança → Verificação em 2 etapas → Senhas de app → Gerar.
- **Yahoo:** use **senha de app** gerada em Segurança da conta.
- Confira se **SmtpUser** e **SmtpPassword** estão corretos no `appsettings` (sem espaços, aspas fechando certo).
- Reinicie a API após alterar a configuração.

O código da API usa TLS 1.2+ e envia credenciais; se o erro continuar, o provedor está rejeitando a senha (use sempre senha de app no Gmail/Yahoo).

## Como verificar

1. Preencha **SmtpHost** (e usuário/senha) em `appsettings.Development.json` (ou User Secrets).
2. Reinicie a API.
3. Na tela de login, clique em “Esqueci minha senha”, informe um e-mail **cadastrado** e envie.
4. Confira a caixa de entrada e o spam. O assunto do e-mail é: **Redefinição de senha - CS Sistemas**.

Se ainda não chegar:

- Veja o **log da API**: se SMTP estiver vazio, aparece algo como:  
  `E-mail não configurado. Link de redefinição para {email}: {link}`.  
  Nesse caso, o link está só no log (copie e abra no navegador para testar).
- Se SMTP estiver preenchido e der erro, a mensagem de exceção no log indica o problema (credenciais, firewall, porta, etc.).

## Segurança

- **Não** commite senhas no repositório. Use User Secrets em dev e variáveis de ambiente em produção.
- Em produção, prefira variáveis:  
  `Email__SmtpHost`, `Email__SmtpUser`, `Email__SmtpPassword`, etc.
