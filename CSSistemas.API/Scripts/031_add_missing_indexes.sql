-- Índices em FKs e colunas críticas que estavam faltando (performance e full table scans)

-- Appointments: BusinessId (usado em GetByBusinessId, HasConflict, Dashboard)
CREATE INDEX IF NOT EXISTS "IX_Appointments_BusinessId"
    ON "Appointments" ("BusinessId") WHERE "IsDeleted" = false;

-- Appointments: ServiceId (JOIN com Services)
CREATE INDEX IF NOT EXISTS "IX_Appointments_ServiceId"
    ON "Appointments" ("ServiceId");

-- Appointments: ScheduledAt (range queries em todos os filtros de data)
CREATE INDEX IF NOT EXISTS "IX_Appointments_ScheduledAt"
    ON "Appointments" ("ScheduledAt" DESC) WHERE "IsDeleted" = false;

-- Appointments: CancelToken (busca pelo link de cancelamento)
CREATE INDEX IF NOT EXISTS "IX_Appointments_CancelToken"
    ON "Appointments" ("CancelToken");

-- Services: BusinessId (GetByBusinessId)
CREATE INDEX IF NOT EXISTS "IX_Services_BusinessId"
    ON "Services" ("BusinessId") WHERE "IsDeleted" = false;

-- Clients: BusinessId (GetByBusinessId)
CREATE INDEX IF NOT EXISTS "IX_Clients_BusinessId"
    ON "Clients" ("BusinessId") WHERE "IsDeleted" = false;

-- BusinessHours: BusinessId (GetByBusinessId)
CREATE INDEX IF NOT EXISTS "IX_BusinessHours_BusinessId"
    ON "BusinessHours" ("BusinessId") WHERE "IsDeleted" = false;

-- Subscriptions: UserId (GetActiveByUserId, middleware de assinatura)
CREATE INDEX IF NOT EXISTS "IX_Subscriptions_UserId"
    ON "Subscriptions" ("UserId") WHERE "IsDeleted" = false;

-- Subscriptions: EndsAt (aviso de vencimento, filtros de ativas)
CREATE INDEX IF NOT EXISTS "IX_Subscriptions_EndsAt"
    ON "Subscriptions" ("EndsAt") WHERE "IsDeleted" = false;

-- Businesses: UserId (GetByUserId — lista negócios do usuário)
CREATE INDEX IF NOT EXISTS "IX_Businesses_UserId"
    ON "Businesses" ("UserId") WHERE "IsDeleted" = false;

-- Índice composto para a query mais frequente: agendamentos de um negócio por data
CREATE INDEX IF NOT EXISTS "IX_Appointments_BusinessId_ScheduledAt"
    ON "Appointments" ("BusinessId", "ScheduledAt" DESC) WHERE "IsDeleted" = false;
