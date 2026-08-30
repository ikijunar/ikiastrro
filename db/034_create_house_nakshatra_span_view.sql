-- 034_create_house_nakshatra_span_view.sql
-- House-centric House -> Rasi -> Nakshatra view: per chart, per house, the sign occupying it and
-- the 9 nakshatra-padas spanning that sign's 30 degrees. Whole-sign, so every house is exactly one
-- sign = 9 padas (2.25 nakshatras). Joins tbl_Chart_HouseLords.HouseSign to
-- tbl_SignAttributes.ZodiacEnumValue (the VedAstro enum form, e.g. "Capricornus"), NOT SignName.
-- Covers every chart type that has tbl_Chart_HouseLords rows (D1/D2/D6/D9/D10/D11/...).

IF OBJECT_ID('dbo.vw_Chart_HouseNakshatraSpan', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Chart_HouseNakshatraSpan;
GO

CREATE VIEW dbo.vw_Chart_HouseNakshatraSpan AS
SELECT
    hl.ChartResultId,
    hl.BirthDetailId,
    hl.ChartType,
    hl.HouseNumber,
    hl.HouseSign,
    sa.Id                      AS HouseSignId,
    hl.LordPlanet,
    n.Id                       AS NakshatraId,
    n.NakshatraName,
    p.PadaNumber,
    p.StartDegree              AS PadaStartDegree,
    p.EndDegree                AS PadaEndDegree,
    lord.PlanetName            AS NakshatraLordName,
    nav.SignName               AS NavamsaSignName
FROM dbo.tbl_Chart_HouseLords hl
JOIN dbo.tbl_SignAttributes  sa   ON sa.ZodiacEnumValue = hl.HouseSign
JOIN dbo.tbl_NakshatraPadas  p    ON p.StartDegree >= (sa.Id - 1) * 30.0
                                 AND p.StartDegree <  sa.Id * 30.0
JOIN dbo.tbl_Nakshatras      n    ON n.Id = p.NakshatraId
JOIN dbo.tbl_Planets         lord ON lord.Id = n.RulingPlanetId
JOIN dbo.tbl_SignAttributes  nav  ON nav.Id = p.NavamsaSignId;
GO
