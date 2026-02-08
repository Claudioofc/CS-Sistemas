# Passo a passo: pagamento com cartão (Mercado Pago)

Quando o seu cliente clicar em **"Cartão (todas as bandeiras)"**, ele será levado à página do Mercado Pago para cadastrar e validar o cartão. Siga os passos abaixo.

---

## Parte 1 – Criar a aplicação no Mercado Pago (uma vez)

1. Acesse **[Mercado Pago Desenvolvedores](https://www.mercadopago.com.br/developers/panel)** e entre na sua conta.

2. **Passo 1 de 4 – Criar aplicação**  
   - Clique em **Criar aplicação** (ou **Suas integrações** → **Criar aplicação**).  
   - Em **Nome da aplicação**, digite por exemplo: **CS Sistemas**.  
   - Clique em **Continuar**.

3. **Passo 2 de 4 – Tipo de pagamento**  
   - Selecione **Pagamentos online**.  
   - Em **Como você criou a loja?**, selecione **Com um desenvolvimento próprio**.  
   - Em **URL da loja (opcional)** pode colocar `https://localhost:5173` ou deixar em branco.  
   - Clique em **Continuar**.

4. **Passo 3 de 4 – Como receber pagamentos**  
   - Na aba **Checkouts**, selecione **Checkout Pro** (“Mais usado” / “Integração fácil”).  
   - Clique em **Continuar**.

5. **Passo 4 de 4**  
   - Conclua o que o assistente pedir e finalize a criação da aplicação.

---

## Parte 2 – Pegar o Access Token

6. Com a aplicação criada, entre nela: **Suas integrações** → clique na aplicação **CS Sistemas** (ou no nome que você deu).

7. No painel da aplicação, à direita, localize o card **Credenciais**.

8. Escolha o tipo de credencial:  
   - **Teste**: para testar sem cobrar (recomendado primeiro).  
   - **Produtivas**: para receber pagamentos reais.

9. Se aparecer **Ativar credenciais**, clique em **Ativar credenciais**.

10. Depois de ativadas, serão exibidos:  
    - **Chave pública** (Public Key)  
    - **Access Token**  
    Copie o **Access Token** inteiro (começa com `TEST-...` em teste ou `APP_USR-...` em produção). Não use no frontend; só na API.

---

## Parte 3 – Configurar na API

11. Abra o arquivo **`appsettings.Development.json`** na pasta **`CSSistemas.API`**.

12. Localize a linha:  
    `"AccessToken": "COLE_SEU_ACCESS_TOKEN_AQUI"`

13. Substitua **`COLE_SEU_ACCESS_TOKEN_AQUI`** pelo Access Token que você copiou no passo 10. Mantenha as aspas.  
    Exemplo:  
    `"AccessToken": "TEST-1234567890123456-012345-..."`

14. Salve o arquivo (Ctrl+S).

15. **Reinicie a API** (pare e suba de novo o projeto `CSSistemas.API`).

---

## Parte 4 – Testar

16. No sistema, acesse **Premium** ou **Planos**, escolha um plano e clique em **Assine agora**.

17. No modal, clique em **Cartão (todas as bandeiras)**.  
    - Se estiver tudo certo, você será redirecionado para a página do Mercado Pago para informar o cartão.  
    - Com credenciais de **teste**, use os [cartões de teste do Mercado Pago](https://www.mercadopago.com.br/developers/pt/docs/checkout-api/additional-content/test-cards) (ex.: 5031 4332 1540 6351).

---

## Produção

Quando for para produção:

- Use as **Credenciais de produção** (aba **Produtivas**) e copie o novo **Access Token**.
- Configure na API por **variáveis de ambiente** (recomendado) ou em **`appsettings.Production.json`**:
  - `Payment__MercadoPago__AccessToken` = seu token de produção
  - `Payment__MercadoPago__SuccessUrl` = https://seusite.com/planos?status=success
  - `Payment__MercadoPago__FailureUrl` = https://seusite.com/planos?status=failure
  - `Payment__MercadoPago__PendingUrl` = https://seusite.com/planos

Troque **seusite.com** pelo seu domínio real.
