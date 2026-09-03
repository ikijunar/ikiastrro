-- =====================================================================
-- 22 — Multi-graha conjunction (Graha Saṃyoga) layer.
--
-- tbl_Chart_Conjunctions records conjunctions pairwise: 4 grahas in one
-- rāśi become C(4,2) = 6 rows, and the "group of 4" is only implicit. This
-- adds an explicit group layer so count-based yogas (Sanyāsa, stellium
-- intensity) and per-participant dignity have a first-class home:
--
--   tbl_Chart_MultiGrahaConjunction        — one row per (ChartResultId, SignId)
--                                            holding >= 2 grahas. PlanetCount +
--                                            MemberKey (ascending PlanetId CSV,
--                                            e.g. "1,3,4,6") for yoga-subset
--                                            matching; D1 LongitudeSpanDegrees
--                                            = max-min member longitude (null
--                                            for varga — a varga sign is a
--                                            discrete bucket, same reasoning as
--                                            tbl_Chart_Conjunctions.DegreeSeparation).
--   tbl_Chart_MultiGrahaConjunctionMember  — per-planet degree / longitude /
--                                            DignityStatus / retrograde /
--                                            combust, ONCE per planet (not
--                                            duplicated across the pair rows a
--                                            planet appears in). D1
--                                            OrbFromGroupCenterDegrees =
--                                            |memberLon - mean(memberLon)|.
--   tbl_Chart_Conjunctions.MultiGrahaConjunctionId — every pair links to its group.
--
-- Grouping is rāśi-based (classical conjunction = co-rāśi); the span / orb
-- columns let a rule derive an "effective" tight conjunction. Triple /
-- quadruple / larger subsets are NOT persisted — the Yoga engine enumerates
-- them from MemberKey. Pairs stay in tbl_Chart_Conjunctions.
--
-- Backfilled from tbl_Chart_KeyDetails (PointKind='Graha', real planet, not
-- the Ascendant). The engine / repository wiring that keeps these tables in
-- step with recompute-analytics lands in a later task; until then the
-- backfilled rows persist and a recompute will not re-derive them.
--
-- Idempotent: table/column adds guarded; the one-time backfill is gated on
-- the SchemaMigrations ledger entry.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/22_add_multigraha_conjunction.sql
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- --- Batch 1: group table ---------------------------------------------------
IF OBJECT_ID('dbo.tbl_Chart_MultiGrahaConjunction', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Chart_MultiGrahaConjunction (
        Id                    INT IDENTITY(1,1) NOT NULL
                                  CONSTRAINT PK_Chart_MultiGrahaConjunction PRIMARY KEY,
        ChartResultId         INT     NOT NULL
                                  CONSTRAINT FK_MultiGrahaConjunction_ChartResult
                                  FOREIGN KEY REFERENCES dbo.tbl_ChartResults (Id),
        SignId                TINYINT NOT NULL
                                  CONSTRAINT FK_MultiGrahaConjunction_Sign
                                  FOREIGN KEY REFERENCES dbo.tbl_SignAttributes (Id),
        HouseNumberFromLagna  TINYINT      NOT NULL,
        PlanetCount           TINYINT      NOT NULL,
        MemberKey             VARCHAR(40)  NOT NULL,  -- ascending PlanetId CSV, e.g. "1,3,4,6"
        LongitudeSpanDegrees  DECIMAL(7,4) NULL,      -- D1 only; max-min member NirayanaLongitude; null for varga
        CONSTRAINT UQ_MultiGrahaConjunction UNIQUE (ChartResultId, SignId),
        CONSTRAINT CK_MultiGrahaConjunction_House CHECK (HouseNumberFromLagna BETWEEN 1 AND 12),
        CONSTRAINT CK_MultiGrahaConjunction_Count CHECK (PlanetCount >= 2),
        CONSTRAINT CK_MultiGrahaConjunction_Span  CHECK (LongitudeSpanDegrees IS NULL
                                  OR (LongitudeSpanDegrees >= 0 AND LongitudeSpanDegrees < 30))
    );
    CREATE NONCLUSTERED INDEX IX_Chart_MultiGrahaConjunction_ChartResultId
        ON dbo.tbl_Chart_MultiGrahaConjunction (ChartResultId);
END
GO

-- --- Batch 2: member table ------------------------------------------------
IF OBJECT_ID('dbo.tbl_Chart_MultiGrahaConjunctionMember', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Chart_MultiGrahaConjunctionMember (
        Id                        INT IDENTITY(1,1) NOT NULL
                                      CONSTRAINT PK_Chart_MultiGrahaConjunctionMember PRIMARY KEY,
        MultiGrahaConjunctionId   INT     NOT NULL
                                      CONSTRAINT FK_MultiGrahaConjunctionMember_Group
                                      FOREIGN KEY REFERENCES dbo.tbl_Chart_MultiGrahaConjunction (Id)
                                      ON DELETE CASCADE,
        PlanetId                  TINYINT NOT NULL
                                      CONSTRAINT FK_MultiGrahaConjunctionMember_Planet
                                      FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        DegreesInSign             DECIMAL(7,4) NULL,   -- this chart's sign; D1 = real degree-in-sign
        NirayanaLongitude         FLOAT        NULL,   -- real sidereal longitude, all chart types
        VargaLongitude            DECIMAL(9,6) NULL,
        OrbFromGroupCenterDegrees DECIMAL(7,4) NULL,   -- D1 only; |memberLon - mean(memberLon)|; null for varga
        DignityStatus             VARCHAR(20)  NULL,   -- matches tbl_Chart_KeyDetails.DignityStatus
        IsRetrograde              BIT          NULL,
        IsCombust                 BIT          NULL,
        CONSTRAINT UQ_MultiGrahaConjunctionMember UNIQUE (MultiGrahaConjunctionId, PlanetId),
        CONSTRAINT CK_MGCMember_DegInSign CHECK (DegreesInSign IS NULL
                                  OR (DegreesInSign >= 0 AND DegreesInSign < 30)),
        CONSTRAINT CK_MGCMember_Nirayana  CHECK (NirayanaLongitude IS NULL
                                  OR (NirayanaLongitude >= 0 AND NirayanaLongitude < 360)),
        CONSTRAINT CK_MGCMember_Varga     CHECK (VargaLongitude IS NULL
                                  OR (VargaLongitude >= 0 AND VargaLongitude < 360)),
        CONSTRAINT CK_MGCMember_Orb       CHECK (OrbFromGroupCenterDegrees IS NULL
                                  OR (OrbFromGroupCenterDegrees >= 0 AND OrbFromGroupCenterDegrees < 30))
    );
    CREATE NONCLUSTERED INDEX IX_Chart_MultiGrahaConjunctionMember_GroupId
        ON dbo.tbl_Chart_MultiGrahaConjunctionMember (MultiGrahaConjunctionId);
END
GO

-- --- Batch 3: pair -> group link ----------------------------------------
IF COL_LENGTH('dbo.tbl_Chart_Conjunctions', 'MultiGrahaConjunctionId') IS NULL
    ALTER TABLE dbo.tbl_Chart_Conjunctions
        ADD MultiGrahaConjunctionId INT NULL
            CONSTRAINT FK_Chart_Conjunctions_MultiGrahaConjunction
            FOREIGN KEY REFERENCES dbo.tbl_Chart_MultiGrahaConjunction (Id);
GO

-- --- Batch 4: one-time backfill from tbl_Chart_KeyDetails --------------
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '22_add_multigraha_conjunction.sql')
BEGIN
    -- Groups: one per (ChartResultId, SignId) holding >= 2 real grahas.
    INSERT dbo.tbl_Chart_MultiGrahaConjunction
        (ChartResultId, SignId, HouseNumberFromLagna, PlanetCount, MemberKey, LongitudeSpanDegrees)
    SELECT
        kd.ChartResultId,
        kd.SignId,
        MIN(kd.HouseNumberFromLagna),                 -- whole-sign: identical across members
        CAST(COUNT(*) AS TINYINT),
        STRING_AGG(CAST(kd.PlanetId AS VARCHAR(2)), ',') WITHIN GROUP (ORDER BY kd.PlanetId),
        CASE WHEN cr.ChartTypeId = 1
             THEN CAST(ROUND(MAX(kd.NirayanaLongitudeDegrees) - MIN(kd.NirayanaLongitudeDegrees), 4) AS DECIMAL(7,4))
             ELSE NULL END
    FROM dbo.tbl_Chart_KeyDetails kd
    JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
    WHERE kd.PointKind = 'Graha' AND kd.PlanetId IS NOT NULL AND kd.Planet <> 'Ascendant'
    GROUP BY kd.ChartResultId, kd.SignId, cr.ChartTypeId
    HAVING COUNT(*) >= 2;

    -- Members: per-planet facts, once. D1 orb from the group's mean longitude.
    INSERT dbo.tbl_Chart_MultiGrahaConjunctionMember
        (MultiGrahaConjunctionId, PlanetId, DegreesInSign, NirayanaLongitude, VargaLongitude,
         OrbFromGroupCenterDegrees, DignityStatus, IsRetrograde, IsCombust)
    SELECT
        g.Id,
        kd.PlanetId,
        kd.DegreesInSignDecimal,
        kd.NirayanaLongitudeDegrees,
        kd.VargaLongitudeDegrees,
        CASE WHEN cr.ChartTypeId = 1
             THEN CAST(ROUND(ABS(kd.NirayanaLongitudeDegrees
                  - AVG(kd.NirayanaLongitudeDegrees) OVER (PARTITION BY g.Id)), 4) AS DECIMAL(7,4))
             ELSE NULL END,
        kd.DignityStatus,
        kd.IsRetrograde,
        kd.IsCombust
    FROM dbo.tbl_Chart_MultiGrahaConjunction g
    JOIN dbo.tbl_ChartResults cr ON cr.Id = g.ChartResultId
    JOIN dbo.tbl_Chart_KeyDetails kd
         ON kd.ChartResultId = g.ChartResultId AND kd.SignId = g.SignId
        AND kd.PointKind = 'Graha' AND kd.PlanetId IS NOT NULL AND kd.Planet <> 'Ascendant';

    -- Link every existing pair row to its group.
    UPDATE c
       SET c.MultiGrahaConjunctionId = g.Id
      FROM dbo.tbl_Chart_Conjunctions c
      JOIN dbo.tbl_Chart_MultiGrahaConjunction g
        ON g.ChartResultId = c.ChartResultId AND g.SignId = c.SignId
     WHERE c.MultiGrahaConjunctionId IS NULL;
END
GO

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '22_add_multigraha_conjunction.sql',
       'tbl_Chart_MultiGrahaConjunction + tbl_Chart_MultiGrahaConjunctionMember + tbl_Chart_Conjunctions.MultiGrahaConjunctionId; backfilled from tbl_Chart_KeyDetails'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '22_add_multigraha_conjunction.sql');
GO

DECLARE @g INT = (SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunction);
DECLARE @m INT = (SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunctionMember);
DECLARE @u INT = (SELECT COUNT(*) FROM dbo.tbl_Chart_Conjunctions WHERE MultiGrahaConjunctionId IS NULL);
PRINT '22 applied: ' + CAST(@g AS VARCHAR(10)) + ' groups, ' + CAST(@m AS VARCHAR(10))
    + ' members, ' + CAST(@u AS VARCHAR(10)) + ' pair rows still unlinked (expect 0).';
GO
