/* ============================================================================
   check_tables.sql  --  ad-hoc, read-only DB inspection for ikiastrro
   ----------------------------------------------------------------------------
   NOT a migration. Never numbered, never recorded in dbo.SchemaMigrations.
   Run interactively in SSMS / Azure Data Studio against the `ikiastrro` DB.
   Nothing here writes. Safe to run in full or section-by-section.

   Organised in LEVELS -- run the level you care about:
     Level 0   Orientation .......... which ChartResult am I looking at?
     Level 0i  Chart INPUTS ........ every table whose values FEED chart generation
     Level 1   Rule / reference .... static, birth-independent lookup tables
     Level 2   Chart output ........ facts for one @ChartResultId
     Level 3   Conjunction deep-dive  Ramakrishnan 4-planet logic (@ChartResultId 427)
     Level 4   Sub-planet layer .... Dim + Rule tables for upagrahas
     Level 5   Row-count rollup .... one-line census of every table above

   Usage: set @ChartResultId below, then execute.
   ============================================================================ */

SET NOCOUNT ON;

DECLARE @ChartResultId INT = 427;   -- <<< change this to inspect a different chart
DECLARE @MultiGrahaConjunctionId INT = 17;   -- Level 3 member drill-down


/* ===========================================================================
   LEVEL 0  --  ORIENTATION
   =========================================================================== */

-- 0.1  The 25 most recent chart results (find the Id you want)
SELECT TOP (25)
       cr.Id            AS ChartResultId,
       cr.BirthDetailId,
       cr.ChartType,
       cr.CalculationKind,
       cr.VargaMethod,
       cr.Ayanamsha,
       cr.HouseSystem,
       cr.EngineVersion,
       cr.RuleSetId,
       cr.ComputedAt
FROM   dbo.tbl_ChartResults AS cr
ORDER  BY cr.Id DESC;

-- 0.2  The chosen chart + its birth record
SELECT cr.Id AS ChartResultId, cr.ChartType, cr.CalculationKind, cr.VargaMethod,
       cr.Ayanamsha, cr.HouseSystem, cr.EngineVersion, cr.RuleSetId, cr.ComputedAt,
       bd.*
FROM   dbo.tbl_ChartResults AS cr
JOIN   dbo.tbl_BirthDetails AS bd ON bd.Id = cr.BirthDetailId
WHERE  cr.Id = @ChartResultId;


/* ===========================================================================
   LEVEL 0i  --  CHART INPUTS  (every table whose values FEED chart generation)
   ---------------------------------------------------------------------------
   Chart generation reads from exactly two kinds of table:

     A. Per-person INPUT ...... tbl_BirthDetails  -- the ONLY user-entered data
                                (date, time, place, lat/lon, UTC offset, IANA tz)

     B. Engine CONFIG + static REFERENCE / RULE masters (shared by every chart):
        - tbl_Rule_Sets ................ which rule-set version is active
                                         (ayanamsha + house-system policy)
        - tbl_Dim_ChartType ............ the D1..D60 catalogue
        - tbl_Rule_VargaScheme ......... how each divisional chart is derived
        - tbl_Planets / tbl_SignAttributes
        - tbl_Nakshatras / tbl_NakshatraPadas / tbl_NakshatraSubLords
        - tbl_Rule_AspectOffset / tbl_Rule_CombustionOrb
        - tbl_Rule_NaturalRelationship / tbl_Rule_TemporaryFriendshipDistance
        - tbl_Dim_Source ............... provenance codes cited by the rule rows

   Everything in tbl_ChartResults / tbl_Chart_* / tbl_Fact_* is OUTPUT, not input.
   =========================================================================== */

-- 0i.1  THE input table -- every birth on file (newest first)
SELECT bd.Id AS BirthDetailId, bd.Name, bd.DateOfBirth, bd.TimeOfBirth,
       bd.PlaceCity, bd.PlaceCountry, bd.Latitude, bd.Longitude,
       bd.UtcOffset, bd.IanaTimeZoneId, bd.CreatedAt,
       (SELECT COUNT(*) FROM dbo.tbl_ChartResults cr WHERE cr.BirthDetailId = bd.Id) AS ChartsGenerated
FROM   dbo.tbl_BirthDetails AS bd
ORDER  BY bd.Id DESC;

-- 0i.2  The birth record feeding @ChartResultId
SELECT bd.*
FROM   dbo.tbl_BirthDetails AS bd
JOIN   dbo.tbl_ChartResults AS cr ON cr.BirthDetailId = bd.Id
WHERE  cr.Id = @ChartResultId;

-- 0i.3  Engine config -- rule-set versions (the active row drives new charts)
SELECT * FROM dbo.tbl_Rule_Sets ORDER BY Id;

-- 0i.4  Chart-type catalogue + divisional-chart derivation rules
SELECT * FROM dbo.tbl_Dim_ChartType   ORDER BY Id;
SELECT * FROM dbo.tbl_Rule_VargaScheme ORDER BY 1;

-- 0i.5  Static astronomical reference masters
SELECT * FROM dbo.tbl_Planets         ORDER BY Id;
SELECT * FROM dbo.tbl_SignAttributes  ORDER BY Id;
SELECT * FROM dbo.tbl_Nakshatras      ORDER BY 1;
SELECT * FROM dbo.tbl_NakshatraPadas  ORDER BY 1;
SELECT * FROM dbo.tbl_NakshatraSubLords ORDER BY 1;

-- 0i.6  Computation-rule masters read during generation
SELECT * FROM dbo.tbl_Rule_AspectOffset;
SELECT * FROM dbo.tbl_Rule_CombustionOrb;
SELECT * FROM dbo.tbl_Rule_NaturalRelationship;
SELECT * FROM dbo.tbl_Rule_TemporaryFriendshipDistance;
SELECT * FROM dbo.tbl_Dim_Source ORDER BY 1;


/* ===========================================================================
   LEVEL 1  --  RULE / REFERENCE LAYER  (static; not tied to any birth)
   =========================================================================== */

--- Planet Dignity & Planet Relationships (Natural & Compound) ---
SELECT * FROM [dbo].[tbl_Rule_GrahaDignity];
SELECT * FROM [dbo].[tbl_Rule_NaturalRelationship];
SELECT * FROM [dbo].[tbl_Rule_CompoundRelationship];
SELECT * FROM [dbo].[tbl_Rule_GrahaAttribute];
SELECT * FROM [dbo].[tbl_Rule_DigBala];

-- 1.1  Dignity legend view (human-readable rollup of tbl_Rule_GrahaDignity)
SELECT * FROM [dbo].[vw_Dignity_Legend];


/* ===========================================================================
   LEVEL 2  --  CHART OUTPUT  for @ChartResultId
   =========================================================================== */

-- Raw, as requested -------------------------------------------------
SELECT * FROM [dbo].[tbl_Chart_Aspects]      WHERE ChartResultId = @ChartResultId;
SELECT * FROM [dbo].[tbl_ChartResults]       WHERE Id            = @ChartResultId;
SELECT * FROM [dbo].[tbl_Chart_KeyDetails]   WHERE ChartResultId = @ChartResultId;

-- 2.1  Aspects, planet names resolved
SELECT a.*,
       p1.PlanetName AS AspectingPlanetResolved,
       p2.PlanetName AS AspectedPlanetResolved
FROM   dbo.tbl_Chart_Aspects AS a
LEFT   JOIN dbo.tbl_Planets  AS p1 ON p1.Id = a.AspectingPlanetId
LEFT   JOIN dbo.tbl_Planets  AS p2 ON p2.Id = a.AspectedPlanetId
WHERE  a.ChartResultId = @ChartResultId
ORDER  BY a.Id;


/* ===========================================================================
   LEVEL 3  --  CONJUNCTION DEEP-DIVE
   Ramakrishnan reference chart -- 4-planet conjunction logic
   (defaults to @ChartResultId 427 / @MultiGrahaConjunctionId 17)
   =========================================================================== */

-- Raw, as requested -------------------------------------------------
SELECT * FROM [dbo].[tbl_Chart_Conjunctions]
WHERE  ChartResultId = @ChartResultId;

SELECT * FROM [dbo].[tbl_Chart_MultiGrahaConjunction]
WHERE  ChartResultId = @ChartResultId;

SELECT * FROM [dbo].[tbl_Chart_MultiGrahaConjunctionMember]
WHERE  [MultiGrahaConjunctionId] = @MultiGrahaConjunctionId;

-- 3.1  Pairwise conjunctions, names resolved, ordered by group
SELECT c.Id,
       c.MultiGrahaConjunctionId,
       p1.PlanetName AS Planet1,
       p2.PlanetName AS Planet2,
       s.SignName,
       c.HouseNumberFromLagna,
       c.DegreeSeparation
FROM   dbo.tbl_Chart_Conjunctions AS c
LEFT   JOIN dbo.tbl_Planets        AS p1 ON p1.Id = c.Planet1Id
LEFT   JOIN dbo.tbl_Planets        AS p2 ON p2.Id = c.Planet2Id
LEFT   JOIN dbo.tbl_SignAttributes AS s  ON s.Id  = c.SignId
WHERE  c.ChartResultId = @ChartResultId
ORDER  BY c.MultiGrahaConjunctionId, c.Planet1Id, c.Planet2Id;

-- 3.2  Multi-graha groups for the chart, with member roster expanded
SELECT g.Id            AS MultiGrahaConjunctionId,
       s.SignName,
       g.HouseNumberFromLagna,
       g.PlanetCount,
       g.MemberKey,
       g.LongitudeSpanDegrees,
       m.PlanetId,
       p.PlanetName,
       m.DegreesInSign,
       m.NirayanaLongitude,
       m.VargaLongitude,
       m.OrbFromGroupCenterDegrees,
       m.DignityStatus,
       m.IsRetrograde,
       m.IsCombust
FROM   dbo.tbl_Chart_MultiGrahaConjunction        AS g
LEFT   JOIN dbo.tbl_SignAttributes               AS s ON s.Id = g.SignId
LEFT   JOIN dbo.tbl_Chart_MultiGrahaConjunctionMember AS m ON m.MultiGrahaConjunctionId = g.Id
LEFT   JOIN dbo.tbl_Planets                       AS p ON p.Id = m.PlanetId
WHERE  g.ChartResultId = @ChartResultId
ORDER  BY g.Id, m.PlanetId;

-- 3.3  Sanity: does PlanetCount match the number of member rows / MemberKey entries?
SELECT g.Id AS MultiGrahaConjunctionId,
       g.PlanetCount,
       COUNT(m.Id)                                             AS MemberRows,
       LEN(g.MemberKey) - LEN(REPLACE(g.MemberKey, ',', '')) + 1 AS MemberKeyCount
FROM   dbo.tbl_Chart_MultiGrahaConjunction            AS g
LEFT   JOIN dbo.tbl_Chart_MultiGrahaConjunctionMember AS m ON m.MultiGrahaConjunctionId = g.Id
WHERE  g.ChartResultId = @ChartResultId
GROUP  BY g.Id, g.PlanetCount, g.MemberKey;


/* ===========================================================================
   LEVEL 4  --  SUB-PLANET (UPAGRAHA) LAYER
   =========================================================================== */

SELECT * FROM [dbo].[tbl_Dim_SubPlanets];
SELECT * FROM [dbo].[tbl_Rule_SubPlanetSunLongitude];
SELECT * FROM [dbo].[tbl_Rule_SubPlanetTime];


/* ===========================================================================
   LEVEL 5  --  ROW-COUNT ROLLUP  (census of every table touched above)
   =========================================================================== */

-- inputs -------------------------------------------------------------
SELECT 'tbl_BirthDetails  (INPUT)'              AS TableName, COUNT(*) AS [Rows] FROM dbo.tbl_BirthDetails
UNION ALL SELECT 'tbl_Rule_Sets  (config)',                   COUNT(*) FROM dbo.tbl_Rule_Sets
UNION ALL SELECT 'tbl_Dim_ChartType  (config)',               COUNT(*) FROM dbo.tbl_Dim_ChartType
UNION ALL SELECT 'tbl_Rule_VargaScheme  (config)',            COUNT(*) FROM dbo.tbl_Rule_VargaScheme
UNION ALL SELECT 'tbl_Planets  (ref)',                        COUNT(*) FROM dbo.tbl_Planets
UNION ALL SELECT 'tbl_SignAttributes  (ref)',                 COUNT(*) FROM dbo.tbl_SignAttributes
UNION ALL SELECT 'tbl_Nakshatras  (ref)',                     COUNT(*) FROM dbo.tbl_Nakshatras
UNION ALL SELECT 'tbl_NakshatraPadas  (ref)',                 COUNT(*) FROM dbo.tbl_NakshatraPadas
UNION ALL SELECT 'tbl_NakshatraSubLords  (ref)',              COUNT(*) FROM dbo.tbl_NakshatraSubLords
UNION ALL SELECT 'tbl_Rule_AspectOffset  (rule)',             COUNT(*) FROM dbo.tbl_Rule_AspectOffset
UNION ALL SELECT 'tbl_Rule_CombustionOrb  (rule)',            COUNT(*) FROM dbo.tbl_Rule_CombustionOrb
UNION ALL SELECT 'tbl_Rule_TemporaryFriendshipDistance (rule)', COUNT(*) FROM dbo.tbl_Rule_TemporaryFriendshipDistance
UNION ALL SELECT 'tbl_Dim_Source  (ref)',                     COUNT(*) FROM dbo.tbl_Dim_Source
-- output -------------------------------------------------------------
UNION ALL SELECT 'tbl_ChartResults',                         COUNT(*) FROM dbo.tbl_ChartResults
UNION ALL SELECT 'tbl_Chart_Aspects',                         COUNT(*) FROM dbo.tbl_Chart_Aspects
UNION ALL SELECT 'tbl_Chart_KeyDetails',                      COUNT(*) FROM dbo.tbl_Chart_KeyDetails
UNION ALL SELECT 'tbl_Chart_Conjunctions',                    COUNT(*) FROM dbo.tbl_Chart_Conjunctions
UNION ALL SELECT 'tbl_Chart_MultiGrahaConjunction',           COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunction
UNION ALL SELECT 'tbl_Chart_MultiGrahaConjunctionMember',     COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunctionMember
UNION ALL SELECT 'tbl_Rule_GrahaDignity',                     COUNT(*) FROM dbo.tbl_Rule_GrahaDignity
UNION ALL SELECT 'tbl_Rule_NaturalRelationship',              COUNT(*) FROM dbo.tbl_Rule_NaturalRelationship
UNION ALL SELECT 'tbl_Rule_CompoundRelationship',             COUNT(*) FROM dbo.tbl_Rule_CompoundRelationship
UNION ALL SELECT 'tbl_Rule_GrahaAttribute',                   COUNT(*) FROM dbo.tbl_Rule_GrahaAttribute
UNION ALL SELECT 'tbl_Rule_DigBala',                          COUNT(*) FROM dbo.tbl_Rule_DigBala
UNION ALL SELECT 'tbl_Dim_SubPlanets',                        COUNT(*) FROM dbo.tbl_Dim_SubPlanets
UNION ALL SELECT 'tbl_Rule_SubPlanetSunLongitude',            COUNT(*) FROM dbo.tbl_Rule_SubPlanetSunLongitude
UNION ALL SELECT 'tbl_Rule_SubPlanetTime',                    COUNT(*) FROM dbo.tbl_Rule_SubPlanetTime;
-- (printed in written order: inputs/config/ref/rule first, then output tables)

-- 5.1  Per-chart fact counts for @ChartResultId
SELECT 'tbl_Chart_Aspects'                   AS TableName, COUNT(*) AS RowsForChart FROM dbo.tbl_Chart_Aspects                  WHERE ChartResultId = @ChartResultId
UNION ALL SELECT 'tbl_Chart_KeyDetails',                   COUNT(*)                 FROM dbo.tbl_Chart_KeyDetails               WHERE ChartResultId = @ChartResultId
UNION ALL SELECT 'tbl_Chart_Conjunctions',                 COUNT(*)                 FROM dbo.tbl_Chart_Conjunctions             WHERE ChartResultId = @ChartResultId
UNION ALL SELECT 'tbl_Chart_MultiGrahaConjunction',        COUNT(*)                 FROM dbo.tbl_Chart_MultiGrahaConjunction    WHERE ChartResultId = @ChartResultId
ORDER BY TableName;
