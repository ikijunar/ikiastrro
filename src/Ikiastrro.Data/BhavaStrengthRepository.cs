using Dapper;
using Ikiastrro.Core.Engines.Strength;

namespace Ikiastrro.Data;

/// <summary>Persists the three auditable Bhava Bala components and twelve house totals.</summary>
public sealed class BhavaStrengthRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public BhavaStrengthRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public void InsertAll(int chartResultId, int ruleSetId, IEnumerable<BhavaBalaResult> results)
    {
        var rows = results.ToList();
        if (rows.Count == 0) return;
        using var connection = _connectionFactory.CreateOpenConnection();
        const string summarySql = """
            INSERT dbo.tbl_Fact_BhavaStrength
                (ChartResultId, HouseNumber, RuleSetId, StrengthProfileCode, FormulaSourceRefCode,
                 BhavaBalaVirupas, BhavaBalaRupas, CalculationNarrative)
            VALUES
                (@ChartResultId, @HouseNumber, @RuleSetId, 'PVR_INTEGRATED_STRENGTH', 'SRC_RAMAN_GRAHA_BHAVA_BALAS',
                 @Virupas, @Rupas, @Narrative)
            """;
        connection.Execute(summarySql, rows.Select(r => new
        {
            ChartResultId = chartResultId, r.HouseNumber, RuleSetId = ruleSetId,
            Virupas = r.BhavaBalaVirupas, Rupas = r.BhavaBalaRupas,
            Narrative = string.Join("; ", r.Components.Select(c => $"{c.ComponentCode}={c.ValueVirupas:0.###}"))
        }));

        const string componentSql = """
            INSERT dbo.tbl_Fact_BhavaStrengthComponent
                (ChartResultId, HouseNumber, RuleSetId, StrengthProfileCode, ComponentCode,
                 ValueVirupas, FormulaSourceRefCode, CalculationNarrative)
            VALUES
                (@ChartResultId, @HouseNumber, @RuleSetId, 'PVR_INTEGRATED_STRENGTH', @ComponentCode,
                 @ValueVirupas, 'SRC_RAMAN_GRAHA_BHAVA_BALAS', @Narrative)
            """;
        connection.Execute(componentSql, rows.SelectMany(r => r.Components.Select(c => new
        {
            ChartResultId = chartResultId, c.HouseNumber, RuleSetId = ruleSetId,
            c.ComponentCode, ValueVirupas = c.ValueVirupas, Narrative = c.Narrative
        })));
    }

    public void DeleteByChartResultId(int chartResultId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute("DELETE FROM dbo.tbl_Fact_BhavaStrengthComponent WHERE ChartResultId = @ChartResultId; DELETE FROM dbo.tbl_Fact_BhavaStrength WHERE ChartResultId = @ChartResultId;", new { ChartResultId = chartResultId });
    }
}
