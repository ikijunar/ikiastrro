-- =====================================================================
-- 03 — tbl_Rule_Sets becomes a real version dimension (review item 1).
-- Id stays TINYINT (widening would cascade to 5+ FK columns). Immutability
-- of a published set is a convention, not yet enforced.
-- =====================================================================
USE [ikiastrro];
GO
IF COL_LENGTH('dbo.tbl_Rule_Sets', 'VersionNumber') IS NULL
ALTER TABLE dbo.tbl_Rule_Sets ADD
    VersionNumber       INT           NOT NULL CONSTRAINT DF_RuleSets_Version     DEFAULT (1),
    EffectiveFromUtc    DATETIME2(0)  NOT NULL CONSTRAINT DF_RuleSets_EffFrom     DEFAULT ('2000-01-01T00:00:00'),
    EffectiveToUtc      DATETIME2(0)  NULL,
    CreatedAtUtc        DATETIME2(0)  NOT NULL CONSTRAINT DF_RuleSets_CreatedAt   DEFAULT sysutcdatetime(),
    SupersedesRuleSetId TINYINT       NULL CONSTRAINT FK_RuleSets_Supersedes FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
    SourceReference     VARCHAR(500)  NULL,
    IsPublished         BIT           NOT NULL CONSTRAINT DF_RuleSets_IsPublished DEFAULT (1);
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RuleSets_Name_Version')
CREATE UNIQUE INDEX UX_RuleSets_Name_Version ON dbo.tbl_Rule_Sets (RuleSetName, VersionNumber);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RuleSets_OneActive')
CREATE UNIQUE INDEX UX_RuleSets_OneActive ON dbo.tbl_Rule_Sets (IsActive) WHERE IsActive = 1;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '03_extend_rule_sets_version.sql', 'rule-set version columns + one-active index'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '03_extend_rule_sets_version.sql');
GO
PRINT '03 applied: tbl_Rule_Sets versioned.';
GO
