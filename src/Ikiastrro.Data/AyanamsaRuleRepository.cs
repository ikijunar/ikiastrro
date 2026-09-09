using Dapper;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Data;

/// <summary>Loads the active calculation preference from tbl_Rule_Ayanamsa.</summary>
public sealed class AyanamsaRuleRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AyanamsaRuleRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public AyanamsaDefinition GetActiveDefault()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        var row = connection.QuerySingle<AyanamsaRuleRow>("""
            SELECT Code, DisplayName, SwissSiderealMode, IsTropical,
                   IsImplemented, CorrectionDirection, CorrectionDegrees
            FROM dbo.tbl_Rule_Ayanamsa a
            JOIN dbo.tbl_Rule_Sets rs ON rs.Id = a.RuleSetId
            WHERE rs.IsActive = 1 AND a.IsDefault = 1
            """);

        if (!row.IsImplemented)
            throw new InvalidOperationException($"The active default ayanamsa '{row.Code}' is not implemented.");

        return new AyanamsaDefinition(row.Code, row.DisplayName, row.SwissSiderealMode, row.IsTropical,
            row.CorrectionDegrees, row.CorrectionDirection);
    }

    // Keep this as a mutable materialization shape.  The database stores SwissSiderealMode as
    // nullable (custom/tropical rows do not have one); Dapper cannot bind that nullable
    // constructor reliably when the result set contains SQL Server's tinyint/int metadata.
    private sealed class AyanamsaRuleRow
    {
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int? SwissSiderealMode { get; set; }
        public bool IsTropical { get; set; }
        public bool IsImplemented { get; set; }
        public string CorrectionDirection { get; set; } = string.Empty;
        public double CorrectionDegrees { get; set; }
    }
}
