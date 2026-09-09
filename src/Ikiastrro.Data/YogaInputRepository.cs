using Dapper;
using System.Text.Json;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Data;

public sealed class YogaInputRepository(SqlConnectionFactory factory)
{
    public void Replace(int chartResultId, int ruleSetId, SiderealPositions positions,
        IReadOnlyList<ChartAnalysisInput> charts, SunTimes sunTimes,
        IReadOnlyList<Ikiastrro.Core.Engines.Strength.PlanetaryStrengthResult> strengths)
    {
        using var connection = factory.CreateOpenConnection();
        var birth = connection.QuerySingle<BirthDetails>("""
            SELECT b.Sex FROM dbo.tbl_BirthDetails b
            JOIN dbo.tbl_ChartResults c ON c.BirthDetailId=b.Id WHERE c.Id=@chartResultId
            """, new { chartResultId });
        var bundle = new ChartBundle(birth, positions, sunTimes, charts,
            new Dictionary<string, string>(), []) { Strengths = strengths };
        var rows = new ProductionYogaEngine().DetectDetailed(bundle);
        using var transaction = System.Transactions.Transaction.Current is null ? connection.BeginTransaction() : null;
        connection.Execute("DELETE dbo.tbl_Fact_YogaInputEvaluations WHERE ChartResultId=@chartResultId",
            new { chartResultId }, transaction);
        foreach (var row in rows)
            connection.Execute("""
                INSERT dbo.tbl_Fact_YogaInputEvaluations
                (ChartResultId,RuleSetId,SourceRefCode,SourceVariantCode,YogaCode,SourceLocator,
                 Present,EvaluationStatus,MissingRequirementCodesJson,SubjectSex,IsNightBirth,
                 ElongationDegrees,IsWaxingMoon,IsFullMoon,LunarPhasePolicyCode,SunriseMethodCode,Notes)
                VALUES (@chartResultId,@ruleSetId,@SourceRefCode,@SourceVariantCode,@YogaCode,@SourceLocator,
                 @Present,@EvaluationStatus,@Missing,@Sex,@IsNightBirth,
                 @Elongation,@Waxing,@FullMoon,@Policy,@SunriseMethodCode,@Notes)
                """, new {
                    chartResultId, ruleSetId, row.Result.SourceRefCode, row.Result.SourceVariantCode,
                    row.Result.YogaCode, row.Result.SourceLocator, row.Result.Present, row.Result.EvaluationStatus,
                    Missing = JsonSerializer.Serialize(row.MissingRequirementCodes), Sex = birth.Sex,
                    IsNightBirth = (bool?)sunTimes.IsNightBirth,
                    Elongation = Phase(charts)?.ElongationDegrees, Waxing = Phase(charts)?.IsWaxing,
                    FullMoon = Phase(charts)?.IsFullMoon, Policy = Phase(charts)?.PolicyCode,
                    SunriseMethodCode = "SWISSEPH_DISC_CENTER_NO_REFRACTION", row.Result.Notes }, transaction);
        transaction?.Commit();
    }

    private static LunarPhase? Phase(IReadOnlyList<ChartAnalysisInput> charts)
    {
        var d1 = charts.FirstOrDefault(x => x.ChartType.Equals("D1", StringComparison.OrdinalIgnoreCase));
        double? Longitude(string planet) => d1?.Planets.FirstOrDefault(x => x.Planet == planet)?.NirayanaLongitudeDegrees;
        return LunarPhase.Calculate(Longitude("Sun"), Longitude("Moon"));
    }
}
