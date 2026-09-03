-- =====================================================================
-- 25 - Repoint tbl_Rule_NaturalRelationship.SourceRefCode from SRC_BPHS to
-- SRC_PVR_INTEGRATED.
--
-- The 42-row Naisargika friendship data is identical across BPHS and PVR's
-- "Integrated Approach", but rammyps wants PVR to be the cited key reference
-- for the relationship layer (a BPHS toggle can be added later as a second
-- rule-set if ever needed). Data rows are NOT changed - only the citation.
--
-- tbl_Rule_TemporaryFriendshipDistance is left on SRC_BPHS (not in scope).
-- Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/25_repoint_natural_relationship_source.sql
-- =====================================================================
USE [ikiastrro];
GO

UPDATE dbo.tbl_Rule_NaturalRelationship
   SET SourceRefCode = 'SRC_PVR_INTEGRATED'
 WHERE SourceRefCode IS NULL OR SourceRefCode <> 'SRC_PVR_INTEGRATED';
GO

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '25_repoint_natural_relationship_source.sql',
       'tbl_Rule_NaturalRelationship.SourceRefCode SRC_BPHS -> SRC_PVR_INTEGRATED (citation only; 42 data rows unchanged)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '25_repoint_natural_relationship_source.sql');
GO

DECLARE @pvr INT  = (SELECT COUNT(*) FROM dbo.tbl_Rule_NaturalRelationship WHERE SourceRefCode = 'SRC_PVR_INTEGRATED');
DECLARE @other INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_NaturalRelationship WHERE SourceRefCode <> 'SRC_PVR_INTEGRATED' OR SourceRefCode IS NULL);
PRINT '25 applied: ' + CAST(@pvr AS VARCHAR(10)) + ' rows now SRC_PVR_INTEGRATED (expect 42), '
    + CAST(@other AS VARCHAR(10)) + ' rows on another code (expect 0).';
GO
