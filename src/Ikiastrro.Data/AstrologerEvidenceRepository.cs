using Dapper;

namespace Ikiastrro.Data;

public sealed record EvidenceTableData(string Code, string Title,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);

public sealed record AstrologerEvidenceData(string PersonName, string ChartType,
    IReadOnlyList<string> ChartTypes, IReadOnlyList<EvidenceTableData> Sections);

/// <summary>Read-only, bounded-query data source for the table-only evidence page.</summary>
public sealed class AstrologerEvidenceRepository(SqlConnectionFactory factory)
{
    public AstrologerEvidenceData? Load(int birthDetailId, string chartType = "D1")
    {
        using var connection = factory.CreateOpenConnection();
        var name = connection.QuerySingleOrDefault<string>(
            "SELECT Name FROM dbo.tbl_BirthDetails WHERE Id=@birthDetailId", new { birthDetailId });
        if (name is null) return null;
        var chartTypes = connection.Query<string>("""
            SELECT ChartType FROM dbo.tbl_ChartResults
            WHERE BirthDetailId=@birthDetailId AND CalculationKind='PositionChart'
            ORDER BY CASE WHEN ChartType='D1' THEN 0 ELSE 1 END, ChartType
            """, new { birthDetailId }).ToList();
        if (!chartTypes.Contains(chartType, StringComparer.OrdinalIgnoreCase)) chartType = "D1";

        var sections = new List<EvidenceTableData>
        {
            Query(connection, "context", "1. Birth and calculation context", """
                SELECT b.Name,b.Sex,b.DateOfBirth,b.TimeOfBirth,b.PlaceCity,b.PlaceCountry,
                       b.Latitude,b.Longitude,b.UtcOffset,b.IanaTimeZoneId,
                       c.ChartType,c.Ayanamsha,c.AyanamshaDegrees,c.HouseSystem,c.RuleSetId,c.ComputedAt
                FROM dbo.tbl_BirthDetails b JOIN dbo.tbl_ChartResults c ON c.BirthDetailId=b.Id
                WHERE b.Id=@birthDetailId AND c.ChartType=@chartType
                """, new { birthDetailId, chartType }),
            Query(connection, "moon", "2. Moon, tithi and lunar context", """
                SELECT SunLongitudeDegrees,MoonLongitudeDegrees,ElongationDegrees,TithiNumber,
                       PakshaCode,IsWaxingMoon,IsFullMoon,IsNightBirth,
                       LunarPhasePolicyCode,SunriseMethodCode
                FROM dbo.vw_ChartMoonContext WHERE BirthDetailId=@birthDetailId
                """, new { birthDetailId }),
            Query(connection, "positions", $"3. {chartType} planetary positions", """
                SELECT Planet,PointKind,Sign,COALESCE(VargaLongitudeDegrees,NirayanaLongitudeDegrees) AS LongitudeDegrees,
                       DegreesInSignDisplay,HouseNumberFromLagna,HouseNumberFromMoon,HouseNumberFromSun,
                       Nakshatra,NakshatraPada,NakshatraLordPlanet,NakshatraSubLordPlanet,
                       IsRetrograde,IsCombust,SignLordPlanet,CharaKaraka
                FROM dbo.vw_ChartPlanetEvidence
                WHERE BirthDetailId=@birthDetailId AND ChartType=@chartType
                ORDER BY CASE PointKind WHEN 'Graha' THEN 0 WHEN 'SpecialLagna' THEN 1
                         WHEN 'Upagraha' THEN 2 ELSE 3 END, Planet
                """, new { birthDetailId, chartType }),
            Query(connection, "rasi", "4. Rasi characteristics", """
                SELECT SignName,SignNameSanskrit,type_house_element AS Element,type_house_keyattri AS Modality,
                       Gender,Direction,RisingType,OddEven,BodyType,Guna,Day_Night AS DayNight,
                       Varna_Class AS Varna,SymbolAnimalType,SymbolDescription,KalapurushaBodyPart,
                       SignIndication,Fertility,SignColour,Ritu
                FROM dbo.tbl_SignAttributes ORDER BY Id
                """),
            Query(connection, "graha", "5. Graha characteristics", """
                SELECT p.PlanetName,p.PlanetNameSanskrit,p.NaturalNature,p.ConditionalRule,
                       a.DisplayName AS Attribute,r.ValueCode,r.ValueText,r.Priority,r.SourceRefCode
                FROM dbo.tbl_Planets p
                LEFT JOIN dbo.tbl_Rule_GrahaAttribute r ON r.GrahaId=p.Id AND r.IsActive=1
                LEFT JOIN dbo.tbl_Dim_GrahaAttribute a ON a.AttributeCode=r.AttributeCode
                ORDER BY p.Id,a.SortOrder,r.Priority
                """),
            Query(connection, "dignity", $"6. {chartType} dignity and avastha", """
                SELECT Planet,Sign,HouseNumberFromLagna,DignityStatus,IsRetrograde,IsCombust,
                       DistanceFromSunDegrees,AgeState,AgeEffectFraction,WakefulnessState,RuleSetId
                FROM dbo.vw_ChartPlanetEvidence
                WHERE BirthDetailId=@birthDetailId AND ChartType=@chartType AND PointKind='Graha'
                ORDER BY Planet
                """, new { birthDetailId, chartType }),
            Query(connection, "shadbala", "7a. Planetary Shadbala", """
                SELECT Planet,SthanaBalaVirupas,DigBalaVirupas,KalaBalaVirupas,CheshtaBalaVirupas,
                       NaisargikaBalaVirupas,DrikBalaVirupas,YuddhaBalaVirupas,
                       ShadbalaVirupas,ShadbalaRupas,MinimumRequiredRupas,PercentOfMinimum,
                       IshtaBala,KashtaBala,StrengthProfileCode,FormulaSourceRefCode
                FROM dbo.vw_ChartShadbala WHERE BirthDetailId=@birthDetailId ORDER BY Planet
                """, new { birthDetailId }),
            Query(connection, "shadbala-components", "7b. Shadbala components", """
                SELECT p.PlanetName AS Planet,s.BalaCode,s.SubComponentCode,s.ValueVirupas,
                       s.UnitCode,s.FormulaSourceRefCode,s.CalculationNarrative
                FROM dbo.tbl_Fact_PlanetaryStrengthComponent s
                JOIN dbo.tbl_ChartResults c ON c.Id=s.ChartResultId
                JOIN dbo.tbl_Planets p ON p.Id=s.PlanetId
                WHERE c.BirthDetailId=@birthDetailId ORDER BY p.Id,s.BalaCode,s.SubComponentCode
                """, new { birthDetailId }),
            Query(connection, "bhava", "8a. Bhava Bala", """
                SELECT HouseNumber,HouseSign,LordPlanet,LordPlacedInHouseFromLagna,LordPlacedInSign,
                       LordDignityStatus,BhavaBalaVirupas,BhavaBalaRupas,
                       StrengthProfileCode,FormulaSourceRefCode
                FROM dbo.vw_ChartBhavaBala WHERE BirthDetailId=@birthDetailId ORDER BY HouseNumber
                """, new { birthDetailId }),
            Query(connection, "bhava-components", "8b. Bhava Bala components", """
                SELECT s.HouseNumber,s.ComponentCode,s.ValueVirupas,s.FormulaSourceRefCode,s.CalculationNarrative
                FROM dbo.tbl_Fact_BhavaStrengthComponent s JOIN dbo.tbl_ChartResults c ON c.Id=s.ChartResultId
                WHERE c.BirthDetailId=@birthDetailId ORDER BY s.HouseNumber,s.ComponentCode
                """, new { birthDetailId }),
            Query(connection, "house-lords", $"9a. {chartType} house lords", """
                SELECT h.HouseNumber,h.HouseSign,h.LordPlanet,h.LordPlacedInHouseFromLagna,
                       h.LordPlacedInHouseFromMoon,h.LordPlacedInHouseFromSun,
                       h.LordPlacedInSign,h.LordDignityStatus
                FROM dbo.tbl_Chart_HouseLords h JOIN dbo.tbl_ChartResults c ON c.Id=h.ChartResultId
                WHERE c.BirthDetailId=@birthDetailId AND c.ChartType=@chartType ORDER BY h.HouseNumber
                """, new { birthDetailId, chartType }),
            Query(connection, "conjunctions", $"9b. {chartType} conjunctions", """
                SELECT x.Planet1,x.Planet2,x.Sign,x.HouseNumberFromLagna,x.DegreeSeparation
                FROM dbo.tbl_Chart_Conjunctions x JOIN dbo.tbl_ChartResults c ON c.Id=x.ChartResultId
                WHERE c.BirthDetailId=@birthDetailId AND c.ChartType=@chartType
                ORDER BY x.HouseNumberFromLagna,x.Planet1,x.Planet2
                """, new { birthDetailId, chartType }),
            Query(connection, "aspects", $"9c. {chartType} aspects", """
                SELECT a.AspectingPlanet,a.AspectedTarget,a.AspectedTargetType,a.AspectType
                FROM dbo.tbl_Chart_Aspects a JOIN dbo.tbl_ChartResults c ON c.Id=a.ChartResultId
                WHERE c.BirthDetailId=@birthDetailId AND c.ChartType=@chartType
                ORDER BY a.AspectingPlanet,a.AspectedTarget
                """, new { birthDetailId, chartType }),
            Query(connection, "vargottama", "10. Vargottama confirmation", """
                SELECT v.PlanetCode,v.D1Sign,v.D9Sign,v.IsVargottama,v.RuleSetId,v.SourceRefCode
                FROM dbo.tbl_Fact_Vargottama v JOIN dbo.tbl_ChartResults c ON c.Id=v.ChartResultId
                WHERE c.BirthDetailId=@birthDetailId ORDER BY v.PlanetCode
                """, new { birthDetailId }),
            Query(connection, "yoga-summary", "11a. Yoga coverage summary", """
                SELECT COUNT(*) AS Total,
                       SUM(CASE WHEN Present=1 THEN 1 ELSE 0 END) AS Present,
                       SUM(CASE WHEN Present=0 THEN 1 ELSE 0 END) AS Absent,
                       SUM(CASE WHEN EvaluationStatus='NOT_EVALUATED' THEN 1 ELSE 0 END) AS NotEvaluated,
                       COUNT(DISTINCT CASE WHEN SourceRefCode='SRC_RAMAN_300_COMBINATIONS' THEN SourceVariantCode END) AS RamanVariants,
                       COUNT(DISTINCT CASE WHEN SourceRefCode='SRC_PVR_INTEGRATED' THEN SourceVariantCode END) AS PvrVariants
                FROM dbo.vw_ChartYogaEvaluations WHERE BirthDetailId=@birthDetailId
                """, new { birthDetailId }),
            Query(connection, "yogas", "11b. Yoga source variants", """
                SELECT SourceVariantCode,YogaCode,SourceRefCode,
                       CASE WHEN Present=1 THEN 'PRESENT' WHEN Present=0 THEN 'ABSENT' ELSE 'NOT_EVALUATED' END AS Result,
                       EvaluationStatus,MissingRequirementCodesJson,SourceLocator,Notes,RuleSetId,ComputedAtUtc
                FROM dbo.vw_ChartYogaEvaluations WHERE BirthDetailId=@birthDetailId
                ORDER BY SourceRefCode,SourceVariantCode
                """, new { birthDetailId }),
            Query(connection, "yoga-charts", "11c. Yoga chart requirements", """
                SELECT SourceVariantCode,ChartCode,RequirementRole,EvaluationScope,MissingBehavior,Notes
                FROM dbo.vw_YogaChartApplicability WHERE RuleSetId=(SELECT MAX(Id) FROM dbo.tbl_Rule_Sets WHERE IsActive=1)
                ORDER BY SourceVariantCode,ChartCode
                """),
            Query(connection, "yoga-context", "11d. Yoga context requirements", """
                SELECT SourceVariantCode,RequirementCode,DerivationMethodCode,MissingBehavior,Notes
                FROM dbo.vw_YogaContextRequirements WHERE IsActive=1 ORDER BY SourceVariantCode,RequirementCode
                """),
            Query(connection, "dasha", "12. Vimshottari Dasha timeline", """
                SELECT p.LevelNumber,p.Lord,p.StartDate,p.EndDate,p.StartDayOffset,p.EndDayOffset
                FROM dbo.tbl_Chart_DashaPeriods p JOIN dbo.tbl_ChartResults c ON c.Id=p.ChartResultId
                WHERE c.BirthDetailId=@birthDetailId AND p.LevelNumber<=2
                ORDER BY p.StartDate,p.LevelNumber,p.SequenceInParent
                """, new { birthDetailId })
        };
        return new(name, chartType, chartTypes, sections);
    }

    private static EvidenceTableData Query(System.Data.IDbConnection connection, string code,
        string title, string sql, object? parameters = null)
    {
        var rows = connection.Query(sql, parameters).Select(row =>
            (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(
                ((IDictionary<string, object>)row).ToDictionary(x => x.Key, x => (object?)x.Value)))
            .ToList();
        var columns = rows.FirstOrDefault()?.Keys.ToList() ?? [];
        return new(code, title, columns, rows);
    }
}
