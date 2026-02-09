# Checklist de Deploy – CS Sistemas

Use este checklist ao colocar o sistema em produção.

## 1. Banco de Dados (PostgreSQL)

- [ ] **Connection string** configurada em produção (`appsettings.Production.json` ou variável `ConnectionStrings__DefaultConnection`).
- [ ] Os scripts SQL em `CSSistemas.API/Scripts/` são executados **automaticamente** na inicialização da API (ordem 001 a 013). Não é necessário rodar manualmente, desde que a API consiga conectar ao banco.
- [ ] Em ambientes restritos, verifique se o usuário do banco tem permissão para `CREATE TABLE` e `ALTER TABLE`.

## 2. Configuração da API (Produção)

- [ ] **BaseBookingUrl**: URL pública do frontend onde os clientes agendam (ex.: `https://seusite.com`). Usada no link de agendamento e em e-mails.
- [ ] **Jwt:Secret**: Definir um secret forte (mín. 32 caracteres). Em produção, use variável de ambiente (ex.: `Jwt__Secret`).
- [ ] **Admin**: E-mail e senha do primeiro admin. Configure por variáveis de ambiente (`Admin__Email`, `Admin__Password`) e não commite senhas no repositório.
- [ ] **Email**: SMTP para redefinição de senha e notificações. Ajustar `PasswordResetBaseUrl` para o domínio de produção.

## 3. Frontend

- [ ] Variável de ambiente (ou build) com a URL da API em produção.
- [ ] Build de produção (`npm run build` no `cssistemas-web`) e hospedagem dos arquivos estáticos (ou deploy do output em `dist`).

## 4. Integração WhatsApp (Opcional)

- [ ] **WhatsApp:Enabled** = `true`.
- [ ] **WhatsApp:ApiUrl** e **WhatsApp:ApiToken** (ex.: Z-API ou outro provedor).
- [ ] No painel, o profissional deve vincular o número de WhatsApp ao negócio (Configurações → negócio → campo WhatsApp).

## 5. IA (OpenAI) – Opcional

- [ ] **OpenAI:Enabled** = `true`.
- [ ] **OpenAI:ApiKey** definido (variável de ambiente `OpenAI__ApiKey` recomendado).
- [ ] Serviços cadastrados no negócio (nome, duração, preço) para a IA responder com dados reais no WhatsApp.

## 6. Pagamento com cartão e PIX

O cliente paga com **qualquer cartão** (crédito ou débito, Visa, Mastercard, Elo, Hipercard, etc.). O processamento é feito por um gateway; hoje o sistema usa **Mercado Pago** como processador (não exige que o cliente tenha conta Mercado Pago).

### Como ativar o cadastro e validação de cartão

1. **Criar conta no Mercado Pago** (ou usar existente): [https://www.mercadopago.com.br](https://www.mercadopago.com.br) → desenvolvedores.
2. **Obter o Access Token:**
   - No painel do Mercado Pago: **Suas integrações** → sua aplicação → **Credenciais**.
   - Use **Credenciais de produção** para receber pagamentos reais (ou **Credenciais de teste** para testar).
   - Copie o **Access Token** (não exponha no frontend; só na API).
3. **Configurar na API** em `appsettings.json` (ou variáveis de ambiente em produção):

```json
"Payment": {
  "Card": { "Enabled": true },
  "MercadoPago": {
    "AccessToken": "APP_USR-xxxx...",
    "SuccessUrl": "https://seusite.com/planos?status=success",
    "FailureUrl": "https://seusite.com/planos?status=failure",
    "PendingUrl": "https://seusite.com/planos"
  }
}
```

4. **Reiniciar a API.** O botão "Cartão (todas as bandeiras)" na tela de planos passa a abrir o checkout; o cliente informa **qualquer cartão** (crédito/débito) na página segura do Mercado Pago.

- [ ] **Payment:MercadoPago**: AccessToken e URLs de retorno configurados.
- [ ] **Payment:Pix**: Chave PIX configurada se for usar.

## 7. Segurança

- [ ] Não commitar `appsettings.Production.json` com segredos reais. Usar variáveis de ambiente ou um vault em produção.
- [ ] **AllowedHosts** restrito ao domínio da API, se desejado.
- [ ] HTTPS em produção para API e frontend.

---

**Resumo:** Ajuste `appsettings.Production.json` (ou variáveis de ambiente), garanta a connection string do PostgreSQL, defina `BaseBookingUrl` para a URL real do sistema e, se for usar WhatsApp/IA, configure as chaves e habilite os módulos.
