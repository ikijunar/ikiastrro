-- =====================================================================
-- 42 - Use the PVR/BPHS traditional Rudramsa (D11) rule.
--
-- The existing implementation's sign map is already the PVR formula:
-- eleven equal parts, reverse seed counted from Aries, then zodiacal
-- advancement. This migration corrects the rule metadata that previously
-- labelled the same calculation as Sanjay Rath.
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

UPDATE dbo.tbl_Rule_VargaScheme
SET MethodCode = 'ParasaraTraditional',
    MethodSource = 'PVR Integrated Approach / BPHS Rudramsa; AstroMath.GetRudramsaSign',
    CalculationNarrative = N'PVR/BPHS RUDRAMSA parts=11: each rasi splits into 11 equal 2.7273 degree parts; seed is the same ordinal counted anti-zodiacally from Aries, then parts advance zodiacally.',
    SourceRefCode = 'SRC_PVR_INTEGRATED'
WHERE ChartTypeId = (SELECT Id FROM dbo.tbl_Dim_ChartType WHERE Code = 'D11');
GO

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '42_switch_d11_to_pvr_traditional.sql', 'D11 Rudramsa metadata and source switched from Sanjay Rath to PVR/BPHS traditional rule'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '42_switch_d11_to_pvr_traditional.sql');
GO

PRINT '42 applied: D11 Rudramsa is tagged PVR/BPHS traditional.';
GO
