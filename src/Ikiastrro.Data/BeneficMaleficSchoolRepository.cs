using Dapper;
using Ikiastrro.Core.Engines.PlanetNature;

namespace Ikiastrro.Data;

/// <summary>tbl_Dim_BeneficMaleficSchools and tbl_Rule_BeneficMaleficSchool (migration 180) as the rule book Core's
/// <see cref="PlanetBeneficMaleficReader"/> reads for the Astro Facts "Benefic / malefic" table.</summary>
public sealed class BeneficMaleficSchoolRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public BeneficMaleficSchoolRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public SchoolRuleBook GetRuleBook(int ruleSetId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();

        var schools = connection.Query<(string Code, string Name, string ShortName, string Basis, string Src, byte Order, string? Notes)>("""
            SELECT Code, Name, ShortName, Basis, SourceRefCode, DisplayOrder, Notes
            FROM dbo.tbl_Dim_BeneficMaleficSchools
            ORDER BY DisplayOrder
            """)
            .Select(s => new BeneficMaleficSchool(s.Code, s.Name, s.ShortName, s.Basis, s.Src, s.Order, s.Notes))
            .ToList();

        var rules = connection.Query<(string School, string Topic, string Class, string? Planet, byte House, string Result, string? Note, string Src)>("""
            SELECT s.Code, r.Topic, r.NatureClass, p.PlanetName, r.HouseNumber, r.Result, r.Note, r.SourceRefCode
            FROM dbo.tbl_Rule_BeneficMaleficSchool r
            JOIN dbo.tbl_Dim_BeneficMaleficSchools s ON s.Id = r.SchoolId
            LEFT JOIN dbo.tbl_Planets p ON p.Id = r.PlanetId
            WHERE r.RuleSetId = @ruleSetId
            ORDER BY s.DisplayOrder, r.Topic, r.NatureClass, r.HouseNumber
            """, new { ruleSetId })
            .Select(r => new SchoolRule(r.School, r.Topic, r.Class, r.Planet, r.House, r.Result, r.Note, r.Src))
            .ToList();

        return new SchoolRuleBook(schools, rules);
    }
}
