namespace Ikiastrro.Core.Engines.PlanetNature;

/// <summary>One school of benefic/malefic reading (tbl_Dim_BeneficMaleficSchools). <see cref="Basis"/> says whether its
/// placement rules are written for natural or functional nature; <see cref="SourceRefCode"/> is its main source
/// (docs/research/domain/planet-benefic-malefic-perspectives.md).</summary>
public sealed record BeneficMaleficSchool(
    string Code, string Name, string ShortName, string Basis, string SourceRefCode, int DisplayOrder, string? Notes);

/// <summary>One row of tbl_Rule_BeneficMaleficSchool. <see cref="Topic"/> is LORDSHIP (the functional nature a lord of
/// <see cref="HouseNumber"/> takes) or PLACEMENT (how a planet of <see cref="NatureClass"/> does in that house).
/// <see cref="Planet"/> is set only for a planet-specific exception; <see cref="NatureClass"/> is ANY when the rule does
/// not depend on the planet's natural nature. A missing rule means the school states nothing, never a neutral verdict.</summary>
public sealed record SchoolRule(
    string SchoolCode, string Topic, string NatureClass, string? Planet, int HouseNumber, string Result,
    string? Note, string SourceRefCode);

public static class SchoolCodes
{
    public const string Pvr = "PVR";
    public const string Bphs = "BPHS";
    public const string Raman = "RAMAN";
    public const string Saravali = "SARAVALI";
    public const string Phaladeepika = "PHALADEEPIKA";
}

/// <summary>Read-side index over the rule rows: planet-specific rule first, then the nature class, then ANY.</summary>
public sealed class SchoolRuleBook
{
    private readonly Dictionary<(string School, string Topic, string Class, string? Planet, int House), SchoolRule> _byKey;

    public SchoolRuleBook(IEnumerable<BeneficMaleficSchool> schools, IEnumerable<SchoolRule> rules)
    {
        Schools = schools.OrderBy(s => s.DisplayOrder).ToList();
        _byKey = rules.ToDictionary(r => (r.SchoolCode, r.Topic, r.NatureClass, r.Planet, r.HouseNumber));
    }

    public IReadOnlyList<BeneficMaleficSchool> Schools { get; }

    public IEnumerable<BeneficMaleficSchool> WithTopic(string topic) =>
        Schools.Where(s => _byKey.Keys.Any(k => k.School == s.Code && k.Topic == topic));

    public SchoolRule? Find(string school, string topic, string natureClass, string planet, int house) =>
        _byKey.GetValueOrDefault((school, topic, natureClass, planet, house))
        ?? _byKey.GetValueOrDefault((school, topic, natureClass, null, house))
        ?? _byKey.GetValueOrDefault((school, topic, "ANY", planet, house))
        ?? _byKey.GetValueOrDefault((school, topic, "ANY", null, house));
}
