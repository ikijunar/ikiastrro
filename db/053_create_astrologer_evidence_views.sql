USE [ikiastrro];
GO

CREATE OR ALTER VIEW dbo.vw_ChartMoonContext
AS
SELECT c.BirthDetailId, c.Id AS ChartResultId, b.Name, b.Sex, c.RuleSetId,
       sun.NirayanaLongitudeDegrees AS SunLongitudeDegrees,
       moon.NirayanaLongitudeDegrees AS MoonLongitudeDegrees,
       phase.ElongationDegrees,
       CASE WHEN phase.ElongationDegrees IS NULL THEN NULL
            ELSE CONVERT(TINYINT, FLOOR(phase.ElongationDegrees / 12.0) + 1) END AS TithiNumber,
       CASE WHEN phase.ElongationDegrees IS NULL THEN NULL
            WHEN phase.ElongationDegrees < 180 THEN 'SHUKLA' ELSE 'KRISHNA' END AS PakshaCode,
       y.IsWaxingMoon, y.IsFullMoon, y.IsNightBirth,
       y.LunarPhasePolicyCode, y.SunriseMethodCode
FROM dbo.tbl_ChartResults c
JOIN dbo.tbl_BirthDetails b ON b.Id = c.BirthDetailId
LEFT JOIN dbo.tbl_Chart_KeyDetails sun ON sun.ChartResultId = c.Id AND sun.Planet = 'Sun'
LEFT JOIN dbo.tbl_Chart_KeyDetails moon ON moon.ChartResultId = c.Id AND moon.Planet = 'Moon'
OUTER APPLY (SELECT CASE WHEN sun.NirayanaLongitudeDegrees IS NULL OR moon.NirayanaLongitudeDegrees IS NULL THEN NULL
                    ELSE CONVERT(FLOAT,
                        ((CONVERT(DECIMAL(12,6), moon.NirayanaLongitudeDegrees - sun.NirayanaLongitudeDegrees)
                          % CONVERT(DECIMAL(12,6), 360)) + CONVERT(DECIMAL(12,6), 360))
                          % CONVERT(DECIMAL(12,6), 360)) END) phase(ElongationDegrees)
OUTER APPLY (SELECT TOP (1) q.IsWaxingMoon, q.IsFullMoon, q.IsNightBirth,
                    q.LunarPhasePolicyCode, q.SunriseMethodCode
             FROM dbo.tbl_Fact_YogaInputEvaluations q
             WHERE q.ChartResultId = c.Id ORDER BY q.Id) y
WHERE c.ChartType = 'D1';
GO

CREATE OR ALTER VIEW dbo.vw_ChartPlanetEvidence
AS
SELECT c.BirthDetailId, c.Id AS ChartResultId, c.ChartType, c.RuleSetId,
       k.Planet, k.PointKind, k.Sign, k.NirayanaLongitudeDegrees,
       k.VargaLongitudeDegrees, k.DegreesInSignDisplay,
       k.HouseNumberFromLagna, k.HouseNumberFromMoon, k.HouseNumberFromSun,
       k.Nakshatra, k.NakshatraPada, k.NakshatraLordPlanet,
       k.NakshatraSubLordPlanet, k.IsRetrograde, k.IsCombust,
       k.DistanceFromSunDegrees, k.SignLordPlanet, k.DignityStatus,
       k.CharaKaraka, ageState.StateName AS AgeState,
       ps.AgeEffectFraction, wakeState.StateName AS WakefulnessState
FROM dbo.tbl_ChartResults c
JOIN dbo.tbl_Chart_KeyDetails k ON k.ChartResultId = c.Id
LEFT JOIN dbo.tbl_Fact_PlanetaryState ps ON ps.ChartResultId = c.Id AND ps.Planet = k.Planet
LEFT JOIN dbo.tbl_Dim_PlanetaryState ageState ON ageState.Id = ps.AgeStateId
LEFT JOIN dbo.tbl_Dim_PlanetaryState wakeState ON wakeState.Id = ps.WakefulnessStateId
WHERE c.CalculationKind = 'PositionChart';
GO

CREATE OR ALTER VIEW dbo.vw_ChartShadbala
AS
SELECT c.BirthDetailId, s.ChartResultId, c.RuleSetId, p.PlanetName AS Planet,
       s.SthanaBalaVirupas, s.DigBalaVirupas, s.KalaBalaVirupas,
       s.CheshtaBalaVirupas, s.NaisargikaBalaVirupas, s.DrikBalaVirupas,
       s.YuddhaBalaVirupas, s.ShadbalaVirupas, s.ShadbalaRupas,
       s.MinimumRequiredRupas,
       CASE WHEN s.MinimumRequiredRupas IS NULL OR s.MinimumRequiredRupas = 0 THEN NULL
            ELSE CONVERT(DECIMAL(9,2), s.ShadbalaRupas * 100.0 / s.MinimumRequiredRupas) END AS PercentOfMinimum,
       s.IshtaBala, s.KashtaBala, s.StrengthProfileCode,
       s.FormulaSourceRefCode, s.CalculationNarrative, s.ComputedAtUtc
FROM dbo.tbl_Fact_PlanetaryStrength s
JOIN dbo.tbl_ChartResults c ON c.Id = s.ChartResultId
JOIN dbo.tbl_Planets p ON p.Id = s.PlanetId;
GO

CREATE OR ALTER VIEW dbo.vw_ChartBhavaBala
AS
SELECT c.BirthDetailId, b.ChartResultId, b.RuleSetId, b.HouseNumber,
       h.HouseSign, h.LordPlanet, h.LordPlacedInHouseFromLagna,
       h.LordPlacedInSign, h.LordDignityStatus,
       b.BhavaBalaVirupas, b.BhavaBalaRupas, b.StrengthProfileCode,
       b.FormulaSourceRefCode, b.CalculationNarrative, b.ComputedAtUtc
FROM dbo.tbl_Fact_BhavaStrength b
JOIN dbo.tbl_ChartResults c ON c.Id = b.ChartResultId
LEFT JOIN dbo.tbl_Chart_HouseLords h ON h.ChartResultId = b.ChartResultId
                                      AND h.HouseNumber = b.HouseNumber;
GO

CREATE OR ALTER VIEW dbo.vw_ChartYogaEvaluations
AS
SELECT c.BirthDetailId, b.Name, y.ChartResultId, y.RuleSetId,
       y.SourceRefCode, y.SourceVariantCode, y.YogaCode, y.SourceLocator,
       y.Present, y.EvaluationStatus, y.MissingRequirementCodesJson,
       y.SubjectSex, y.IsNightBirth, y.ElongationDegrees,
       y.IsWaxingMoon, y.IsFullMoon, y.LunarPhasePolicyCode,
       y.SunriseMethodCode, y.Notes, y.ComputedAtUtc
FROM dbo.tbl_Fact_YogaInputEvaluations y
JOIN dbo.tbl_ChartResults c ON c.Id = y.ChartResultId
JOIN dbo.tbl_BirthDetails b ON b.Id = c.BirthDetailId;
GO

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '053_create_astrologer_evidence_views.sql',
       'Read-only Moon, planet, strength, Bhava and yoga views for the astrologer evidence page.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations
                  WHERE ScriptName = '053_create_astrologer_evidence_views.sql');
GO
