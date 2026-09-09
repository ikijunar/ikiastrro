using Dapper;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Strength;

namespace Ikiastrro.Data;

public sealed class VargottamaRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public VargottamaRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public void InsertAll(int chartResultId, int ruleSetId, IEnumerable<VargottamaResult> results)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT dbo.tbl_Fact_Vargottama
                (ChartResultId, PlanetId, PlanetCode, D1Sign, D9Sign, IsVargottama, RuleSetId, SourceRefCode)
            VALUES (@ChartResultId, @PlanetId, @PlanetCode, @D1Sign, @D9Sign, @IsVargottama, @RuleSetId, 'SRC_PVR_INTEGRATED')
            """;
        connection.Execute(sql, results.Select(r => new
        {
            ChartResultId = chartResultId,
            PlanetId = Enum.TryParse<PlanetName>(r.Planet, out var p) ? (int?)AstroIds.PlanetId(p) : null,
            PlanetCode = r.Planet,
            r.D1Sign,
            r.D9Sign,
            r.IsVargottama,
            RuleSetId = ruleSetId
        }));
    }

    public void DeleteByChartResultId(int chartResultId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute("DELETE FROM dbo.tbl_Fact_Vargottama WHERE ChartResultId = @ChartResultId", new { ChartResultId = chartResultId });
    }

    /// <summary>Clears every stored chart's vargottama rows for one person — the delete-first step of ChartGenerationService.GenerateAll (this FK to tbl_ChartResults does not cascade).</summary>
    public void DeleteByBirthDetailId(int birthDetailId)
    {
        const string sql = """
            DELETE FROM dbo.tbl_Fact_Vargottama
            WHERE ChartResultId IN (SELECT Id FROM dbo.tbl_ChartResults WHERE BirthDetailId = @BirthDetailId)
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { BirthDetailId = birthDetailId });
    }
}
