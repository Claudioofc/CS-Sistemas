# Subir o CS Sistemas no Railway (trial grátis)

Você já fez login no Railway. Siga estes passos para colocar o sistema no ar usando o crédito grátis.

---

## 1. Colocar o código no GitHub

Se ainda não estiver no GitHub:

1. Crie um repositório em [github.com](https://github.com) (ex.: `cssistemas`).
2. No seu PC, na pasta do projeto (onde está o `.sln`), abra o terminal e rode:

```bash
git init
git add .
git commit -m "Deploy Railway"
git branch -M main
git remote add origin https://github.com/SEU_USUARIO/SEU_REPO.git
git push -u origin main
```

(Substitua `SEU_USUARIO` e `SEU_REPO` pelo seu usuário e nome do repositório.)

---

## 2. Criar o projeto no Railway

1. Acesse [railway.app](https://railway.app) e faça login.
2. Clique em **“New Project”**.
3. Escolha **“Deploy from GitHub repo”** e autorize o Railway a acessar sua conta GitHub (se pedir).
4. Selecione o repositório do CS Sistemas e confirme.

---

## 3. Adicionar o PostgreSQL

1. No projeto que abriu, clique em **“+ New”**.
2. Escolha **“Database”** → **“PostgreSQL”**.
3. Aguarde o banco ser criado. Depois, clique no serviço do PostgreSQL.
4. Na aba **“Variables”** ou **“Connect”**, copie a **connection string** (algo como `postgresql://postgres:SENHA@host:porta/railway`). Você vai usar no próximo passo.

---

## 4. Configurar o serviço da API

1. No mesmo projeto, clique no serviço que é o seu **repositório** (o que veio do GitHub), não no PostgreSQL.
2. Abra **“Settings”**:
   - **Root Directory:** deixe em branco (raiz do repo).
   - **Builder:** escolha **“Dockerfile”** (o projeto tem um `Dockerfile` na raiz que monta o frontend + API).
   - **Dockerfile path:** `Dockerfile` (ou deixe em branco se a Railway detectar sozinha).
   - **Start Command:** deixe em branco (o Dockerfile já define o comando).
3. Em **“Variables”**, adicione as variáveis de ambiente (clique em **“+ New Variable”** ou **“Raw Editor”**):

| Nome | Valor (exemplo) |
|------|------------------|
| `ConnectionStrings__DefaultConnection` | A connection string que você copiou do PostgreSQL (substitua a que vier pelo valor real) |
| `Jwt__Secret` | Uma frase longa e aleatória com no mínimo 32 caracteres (ex.: `MinhaChaveSecretaProducao2025NuncaCompartilhar!`) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `BaseBookingUrl` | A URL que o Railway der para o seu app (ex.: `https://cssistemas.up.railway.app`) — você pode ajustar depois que gerar o domínio |

**Como pegar a connection string do PostgreSQL no Railway:**  
No serviço PostgreSQL → aba **“Variables”** ou **“Connect”** → use a variável que contém a URL (ex.: `DATABASE_URL`). No Railway ela costuma vir como `POSTGRES_URL` ou `DATABASE_URL`. O formato esperado pela API é:

```
Host=xxx.railway.app;Port=5432;Database=railway;Username=postgres;Password=XXXXX;SSL Mode=Require;
```

Se a Railway só mostrar `postgresql://postgres:senha@host:5432/railway`, converta assim:

- Host = o endereço depois de `@` e antes de `:5432`
- Port = 5432
- Database = railway (ou o nome que vier)
- Username = postgres
- Password = a senha que aparece na URL
- Connection string final:  
  `Host=SEU_HOST;Port=5432;Database=railway;Username=postgres;Password=SUA_SENHA;SSL Mode=Require;`

(Substitua SEU_HOST e SUA_SENHA.)

---

## 5. Ligar a API ao PostgreSQL

1. No serviço da **API** (repositório), em **“Settings”** ou **“Variables”**, procure por **“Reference”** ou **“Connect to PostgreSQL”**.
2. Se aparecer a opção de **referenciar o banco**, use-a; a Railway pode criar automaticamente algo como `DATABASE_URL`. Nesse caso, você pode precisar criar uma variável **`ConnectionStrings__DefaultConnection`** com o valor no formato do .NET (como na tabela acima).
3. Salve as variáveis. O Railway costuma fazer um novo deploy ao salvar.

---

## 6. Gerar domínio público

1. No serviço da **API**, vá em **“Settings”** → **“Networking”** ou **“Generate Domain”**.
2. Clique em **“Generate Domain”**. A Railway vai dar uma URL tipo `https://seu-projeto.up.railway.app`.
3. Copie essa URL e, nas variáveis, atualize **`BaseBookingUrl`** para ela (ex.: `https://seu-projeto.up.railway.app`).
4. Reinicie o serviço (ou faça um novo deploy) para aplicar.

---

## 7. Conferir o deploy

1. Abra no navegador a URL que a Railway gerou (ex.: `https://seu-projeto.up.railway.app`).
2. Você deve ver a tela de login do CS Sistemas.
3. Se aparecer erro 502/503, espere 1–2 minutos (primeira inicialização e criação das tabelas no PostgreSQL).
4. Os scripts SQL da pasta `Scripts` rodam na subida da API; o banco é criado/atualizado sozinho.

---

## 8. Admin e e-mail (opcional no início)

- **Admin:** o primeiro usuário admin é criado pelo seed. Configure no `appsettings` ou em variáveis: `Admin__Email` e `Admin__Password` (não use a senha de desenvolvimento em produção).
- **E-mail:** para redefinição de senha e notificações, configure depois as variáveis de SMTP ou Resend (conforme o `DEPLOY.md`).

---

## Resumo rápido

1. Código no GitHub.  
2. Railway → New Project → Deploy from GitHub → escolher o repo.  
3. New → Database → PostgreSQL.  
4. No serviço da API → Variables: `ConnectionStrings__DefaultConnection`, `Jwt__Secret`, `ASPNETCORE_ENVIRONMENT=Production`, `BaseBookingUrl`.  
5. Generate Domain no serviço da API.  
6. Acessar a URL e testar o login.

O trial da Railway dá cerca de US$ 5 de crédito (até 30 dias). Depois disso, para continuar você precisará do plano pago.
