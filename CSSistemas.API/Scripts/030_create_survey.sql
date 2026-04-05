ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "SurveyDismissals" INT NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS "SurveyResponses" (
    "Id"        UUID          NOT NULL DEFAULT gen_random_uuid(),
    "UserId"    UUID          NOT NULL,
    "Score"     INT           NOT NULL,
    "Comment"   TEXT,
    "CreatedAt" TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_SurveyResponses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SR_Users" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_SurveyResponses_UserId" ON "SurveyResponses" ("UserId");
