START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260918141334_AddExpenseBusinessDate') THEN
    ALTER TABLE expenses ADD "BusinessDate" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260918141334_AddExpenseBusinessDate') THEN
    CREATE INDEX "IX_expenses_TenantId_BusinessDate" ON expenses ("TenantId", "BusinessDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260918141334_AddExpenseBusinessDate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260918141334_AddExpenseBusinessDate', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260918141833_AddTemporalReportIndexes') THEN
    CREATE INDEX "IX_orders_TenantId_CreatedAt" ON orders ("TenantId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260918141833_AddTemporalReportIndexes') THEN
    CREATE INDEX "IX_orders_TenantId_PaidAt" ON orders ("TenantId", "PaidAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260918141833_AddTemporalReportIndexes') THEN
    CREATE INDEX "IX_order_receipts_TenantId_CreatedAt" ON order_receipts ("TenantId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260918141833_AddTemporalReportIndexes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260918141833_AddTemporalReportIndexes', '10.0.11');
    END IF;
END $EF$;
COMMIT;

