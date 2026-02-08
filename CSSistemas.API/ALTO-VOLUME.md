# Configuração para alto volume

A API está preparada para alto volume com as seguintes alterações:

## 1. Rate limiting (por IP)
- **120 requisições por minuto por IP** (global).
- Resposta 429 com `message` e `retryAfter` quando excedido.
- Header `Retry-After` na resposta.

## 2. Pool de conexões PostgreSQL
- **Maximum Pool Size=200** e **Minimum Pool Size=10** aplicados automaticamente se não estiverem na connection string.
- Para ajustar, use na connection string: `Maximum Pool Size=300;Minimum Pool Size=20`.

## 3. Redis (múltiplas instâncias)
- **Slots pendentes do WhatsApp** (confirmação por mensagem): sem Redis, ficam em memória (uma instância só).
- Para **várias instâncias** atrás de load balancer, configure Redis:
  - **appsettings** ou variável de ambiente: `Redis:Configuration` ou `ConnectionStrings:Redis` com a connection string (ex.: `localhost:6379` ou `redis-server:6379,password=xxx`).
  - Com Redis configurado, o store de slots pendentes usa Redis e funciona entre instâncias.

## 4. Fila de e-mail em background
- Envio de e-mail (redefinição de senha, confirmação/cancelamento de agendamento) é **enfileirado** e processado em background.
- O request retorna sem esperar o SMTP/Resend; um worker processa a fila (uma instância por processo).

## 5. Arquivos estáticos
- **HTML**: `Cache-Control: no-cache`.
- **JS/CSS/assets**: `Cache-Control: public,max-age=31536000,immutable` (1 ano) para melhor cache em CDN/navegador.

## Resumo para produção com várias instâncias
1. Configure **Redis** (`Redis:Configuration` ou `ConnectionStrings:Redis`).
2. Mantenha a connection string do PostgreSQL com pool (já aplicado por padrão).
3. Opcional: coloque o front (wwwroot) em CDN e aponte a API só para `/api` e health.
