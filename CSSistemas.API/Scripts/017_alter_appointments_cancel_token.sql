-- Token para o cliente cancelar agendamento pelo link do e-mail (agendamento público).
ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "CancelToken" varchar(64) NULL;
