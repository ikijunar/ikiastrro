using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>tbl_Rule_VargaScheme - the data-driven varga sign rules. Read once at
/// startup by ChartCalculationOrchestrator; also the table the Python comparison
/// layer will read to know how each varga sign was chosen.</summary>
public class VargaSchemeRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public VargaSchemeRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public IReadOnlyList<VargaScheme> GetAll(int ruleSetId)
    {
        const string sql = """
            SELECT ct.Code                    AS ChartType,
                   CAST(vs.DivisionFactor AS INT) AS DivisionFactor,
                   vs.MethodCode             AS MethodCode,
                   vs.SignRuleKind           AS SignRuleKind,
                   vs.SignRuleKey            AS SignRuleKey
            FROM dbo.tbl_Rule_VargaScheme vs
            JOIN dbo.tbl_Dim_ChartType   ct ON ct.Id = vs.ChartTypeId
            WHERE vs.RuleSetId = @RuleSetId
            ORDER BY ct.DisplayOrder, vs.DivisionFactor
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<VargaScheme>(sql, new { RuleSetId = ruleSetId }).ToList();
    }
}
