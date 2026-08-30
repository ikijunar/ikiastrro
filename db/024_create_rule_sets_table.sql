-- 024_create_rule_sets_table.sql
-- The version dimension for every tbl_Rule_* table. One row per named classical scheme.
-- Seeded with exactly one row transcribing this project's CURRENT hardcoded behavior
-- (ClassicalRelationships.cs / ClassicalCombustion.cs) -- not a new interpretation, a mirror
-- of what's already live, so Phase 2 wiring is a no-op change in computed results.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Rule_Sets')
BEGIN
    CREATE TABLE tbl_Rule_Sets
    (
        Id           TINYINT      NOT NULL PRIMARY KEY,
        RuleSetName  VARCHAR(40)  NOT NULL UNIQUE,
        Description  VARCHAR(200) NULL,
        IsActive     BIT          NOT NULL DEFAULT 0
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_Rule_Sets)
BEGIN
    INSERT INTO tbl_Rule_Sets (Id, RuleSetName, Description, IsActive)
    VALUES (1, 'Parashari-Classical',
            'This project''s existing hardcoded rules (ClassicalRelationships.cs / ClassicalCombustion.cs) as of 2026-08-30 -- Rahu/Ketu use Jupiter-style 5th/7th/9th aspects, BPHS/Phaladeepika combustion orbs.',
            1);
END
GO
