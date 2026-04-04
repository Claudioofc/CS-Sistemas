-- Atualiza preços dos planos para valores competitivos com o mercado (2026).
-- Mercado: Simples Agenda R$39,90/mês, EiBarber R$49/mês, Trinks ~R$65-89/mês.
-- CS Sistemas como entrante posiciona-se abaixo dos líderes.
UPDATE "Plans" SET "Price" = 49.90, "Features" = 'Cobrança mensal. Cancele quando quiser.' WHERE "Name" = 'Mensal';
UPDATE "Plans" SET "Price" = 269.40, "Features" = 'R$44,90/mês. Economia de 10% em relação ao mensal.' WHERE "Name" = '6 meses';
UPDATE "Plans" SET "Price" = 479.00, "Features" = 'R$39,90/mês. 2 meses grátis. Melhor custo-benefício.' WHERE "Name" = '1 ano';
