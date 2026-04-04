-- Remove flag de admin do e-mail antigo e garante que o novo seja admin
UPDATE "Users" SET "IsAdmin" = false WHERE "Email" = 'claudioelias98@yahoo.com.br' AND "IsDeleted" = false;
