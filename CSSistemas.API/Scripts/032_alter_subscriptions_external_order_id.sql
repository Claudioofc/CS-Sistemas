-- Adiciona ExternalOrderId para idempotência no webhook do Mercado Pago
-- Garante que o mesmo orderId não crie duas assinaturas (MP pode reenviar o mesmo webhook)

ALTER TABLE "Subscriptions"
    ADD COLUMN IF NOT EXISTS "ExternalOrderId" varchar(128) NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Subscriptions_ExternalOrderId"
    ON "Subscriptions" ("ExternalOrderId")
    WHERE "ExternalOrderId" IS NOT NULL;
