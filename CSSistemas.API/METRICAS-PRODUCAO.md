# Métricas sugeridas para produção

Use estas métricas para monitorar **suporte** (capacidade) e **velocidade** do sistema.

---

## 1. Tempo de resposta da API

**O que:** Quanto tempo cada request leva (ms).  
**Por quê:** Ver se a API está rápida e se há endpoints lentos.

**Como obter:**
- **Application Insights (Azure):** já mede duração por request.
- **Middleware de timing:** registrar no log o tempo por rota (ex.: `Information: GET /api/appointments/by-business/... 142ms`).
- **Nginx/load balancer:** logs de `$request_time` ou equivalente.

**Meta sugerida:** P95 &lt; 500 ms para leituras; &lt; 1 s para escritas (criar agendamento, etc.).

---

## 2. Rate limit (429)

**O que:** Quantidade de respostas **429 Too Many Requests** por minuto/hora.  
**Por quê:** Ver se o limite (120/min por IP) está adequado ou se muitos usuários legítimos estão sendo barrados.

**Como obter:**
- Log no `OnRejected` do rate limiter: IP + timestamp.
- Contador em Application Insights ou em log agregado (ex.: “429 count by hour”).

**Meta:** Poucos 429 em cenário normal; pico de 429 pode indicar ataque ou necessidade de subir o limite.

---

## 3. Pool de conexões PostgreSQL

**O que:** Conexões em uso vs. máximo (200).  
**Por quê:** Ver se está perto do limite (possível gargalo ou necessidade de aumentar pool/réplicas).

**Como obter:**
- **PostgreSQL:** `SELECT count(*) FROM pg_stat_activity WHERE datname = 'CSSistemas';`
- Ou métricas do provedor (Azure DB, AWS RDS, etc.) de “active connections”.

**Meta:** Uso estável bem abaixo de 200 (ex.: &lt; 80% em pico).

---

## 4. Fila de e-mail

**O que:** Se há atraso ou falha no processamento da fila (envios em background).  
**Por quê:** Garantir que confirmações e cancelamentos chegam em tempo razoável.

**Como obter:**
- **Logs:** no `EmailQueueHostedService`, logar sucesso/erro por item (já existe `LogError` em falha).
- **Contadores:** opcionalmente, incrementar “emails_enqueued” e “emails_processed” / “emails_failed” (se integrar com Application Insights ou Prometheus).

**Meta:** Quase zero falhas; tempo entre enfileirar e enviar &lt; 1 minuto em condições normais.

---

## 5. Redis (quando em uso)

**O que:** Latência (ms) e disponibilidade do Redis.  
**Por quê:** Slots pendentes do WhatsApp e cache dependem dele em multi-instância.

**Como obter:**
- **Redis:** `INFO stats`, `LATENCY HISTORY command`.
- Provedor (Azure Cache, ElastiCache, etc.) costuma expor latência e uptime.

**Meta:** Latência estável (ex.: P99 &lt; 10 ms); 0% de indisponibilidade em janelas curtas.

---

## 6. Erros 5xx e exceções

**O que:** Quantidade de respostas 500/502/503 e exceções não tratadas.  
**Por quê:** Indicador de saúde e necessidade de correção.

**Como obter:**
- Pipeline de exceção já trata `CommException`; exceções inesperadas viram 500.
- Log de exceção no `UseExceptionHandler` (mensagem + stack em Development; em Production só log interno, resposta genérica).
- Application Insights: contagem por status code e por exceção.

**Meta:** Quase zero 5xx em tráfego normal.

---

## 7. Resumo por prioridade

| Prioridade | Métrica | Onde olhar |
|------------|---------|------------|
| Alta | Tempo de resposta (P95) | App Insights / middleware / proxy |
| Alta | 5xx e exceções | Logs / App Insights |
| Média | 429 (rate limit) | Log OnRejected / contadores |
| Média | Pool PostgreSQL | `pg_stat_activity` ou painel do provedor |
| Média | Fila de e-mail (falhas/atraso) | Logs do `EmailQueueHostedService` |
| Baixa (multi-instância) | Redis latência/disponibilidade | Redis INFO / provedor |

---

## Próximos passos opcionais

- **Middleware de timing:** registrar duração por request em log estruturado (correlation id + rota + ms).
- **Endpoint `/api/health/detailed`:** além de DB, incluir Redis (se configurado) e, se quiser, “fila de e-mail saudável” (ex.: worker rodando).
- **Application Insights:** habilitar no Azure para métricas e dashboards prontos.

Se quiser, posso sugerir o código do middleware de timing e do health detalhado no seu projeto.
