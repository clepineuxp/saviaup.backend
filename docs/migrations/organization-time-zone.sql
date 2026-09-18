START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260918141314_AddOrganizationTimeZone') THEN
    ALTER TABLE tenants ADD "TimeZoneId" character varying(100) NOT NULL DEFAULT 'America/Bogota';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260918141314_AddOrganizationTimeZone') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260918141314_AddOrganizationTimeZone', '10.0.11');
    END IF;
END $EF$;
COMMIT;

