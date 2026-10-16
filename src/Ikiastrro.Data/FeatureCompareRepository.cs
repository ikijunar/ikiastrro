using Dapper;

namespace Ikiastrro.Data;

/// <summary>One comparable fact about one person's chart: a row of vw_Chart_Feature (migration 184).
/// <see cref="SubjectKey"/> is the canonical identity (a YogaCode), never a source variant.</summary>
public sealed record FeatureAtom(
    int BirthDetailId, string PersonName, string FeatureType, string SubjectKey, string ValueKey,
    string? GroupCode, string? GroupName, string? NatureCode, string? TypeCode,
    int VariantCount, string? Variants);

public sealed record FeaturePerson(int BirthDetailId, string PersonName);

public enum FeatureCompareMode
{
    /// <summary>Every subject any chosen person has.</summary>
    All,
    /// <summary>Subjects every chosen person has.</summary>
    Shared,
    /// <summary>Subjects some, but not all, of the chosen people have.</summary>
    Differences,
}

/// <summary>One subject across the chosen people: who has it, with which value, and its descriptive fields.
/// Presence facts (yogas) carry the constant value PRESENT; valued facts (a planet's sign) differ per person.</summary>
public sealed record FeatureRow(
    string FeatureType, string SubjectKey, string? GroupCode, string? GroupName, string? NatureCode, string? TypeCode,
    IReadOnlyDictionary<int, string> Values)
{
    public IReadOnlyCollection<int> PersonIds => Values.Keys.ToList();

    /// <summary>The most people who share one value for this subject (for a yoga: how many have it).</summary>
    public int SharedBy => Values.Values.GroupBy(v => v).Max(g => g.Count());
}

/// <summary>A subject with a value: the unit two people either share or do not.</summary>
public sealed record FeatureFact(string SubjectKey, string ValueKey);

/// <summary>How common a fact is among the saved people (vw_Feature_BaseRate, migration 186). Not a population baseline.</summary>
public sealed record FeatureRate(int People, int Population)
{
    public double Rate => Population == 0 ? 0 : (double)People / Population;
    public string Percent => Rate.ToString("0%", System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>Two people's atoms of one feature type split into shared and exclusive facts.</summary>
public sealed record FeatureOverlap(
    IReadOnlyList<FeatureFact> Shared, IReadOnlyList<FeatureFact> OnlyA, IReadOnlyList<FeatureFact> OnlyB)
{
    public int Count => Shared.Count;
}

/// <summary>
/// Cross-person comparison over feature atoms (docs/architecture/compatibility_similarity.md §5.1). The same
/// arithmetic serves every feature type, so a new kind of fact needs a new vw_Chart_Feature branch and no new
/// compare code. Counts, not scores: base rates are not built, so nothing here ranks "most similar".
/// </summary>
public static class FeatureComparer
{
    /// <summary>Atoms grouped by subject, filtered by <paramref name="mode"/> against the chosen people. A subject is
    /// shared only when every chosen person has it with the same value. Ordered by group, then by how many people
    /// share a value (most first), then by key.</summary>
    public static IReadOnlyList<FeatureRow> Compare(
        IEnumerable<FeatureAtom> atoms, IReadOnlyCollection<int> personIds, FeatureCompareMode mode = FeatureCompareMode.All)
    {
        var chosen = personIds.ToHashSet();
        var rows = atoms
            .Where(a => chosen.Contains(a.BirthDetailId))
            .GroupBy(a => (a.FeatureType, a.SubjectKey))
            .Select(g => new FeatureRow(
                g.Key.FeatureType, g.Key.SubjectKey,
                g.Select(x => x.GroupCode).FirstOrDefault(v => v is not null),
                g.Select(x => x.GroupName).FirstOrDefault(v => v is not null),
                g.Select(x => x.NatureCode).FirstOrDefault(v => v is not null),
                g.Select(x => x.TypeCode).FirstOrDefault(v => v is not null),
                g.GroupBy(x => x.BirthDetailId).ToDictionary(x => x.Key, x => x.First().ValueKey)));

        rows = mode switch
        {
            FeatureCompareMode.Shared => rows.Where(r => r.SharedBy == chosen.Count),
            FeatureCompareMode.Differences => rows.Where(r => r.SharedBy < chosen.Count),
            _ => rows,
        };

        return rows
            .OrderBy(r => r.GroupName ?? "~")
            .ThenByDescending(r => r.SharedBy)
            .ThenBy(r => r.SubjectKey, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The overlap of two people on one feature type: facts (subject and value) both have, and each one's own.</summary>
    public static FeatureOverlap Overlap(IEnumerable<FeatureAtom> atoms, int personA, int personB, string featureType)
    {
        var mine = atoms.Where(a => a.FeatureType == featureType).ToList();
        HashSet<FeatureFact> Facts(int id) => mine.Where(x => x.BirthDetailId == id).Select(x => new FeatureFact(x.SubjectKey, x.ValueKey)).ToHashSet();
        var a = Facts(personA);
        var b = Facts(personB);
        static List<FeatureFact> Ordered(IEnumerable<FeatureFact> f) =>
            f.OrderBy(x => x.SubjectKey, StringComparer.Ordinal).ThenBy(x => x.ValueKey, StringComparer.Ordinal).ToList();
        return new FeatureOverlap(Ordered(a.Intersect(b)), Ordered(a.Except(b)), Ordered(b.Except(a)));
    }
}

/// <summary>Reads vw_Chart_Feature. The view dedupes catalogued variants to one atom per person, chart and subject.</summary>
public sealed class FeatureCompareRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public FeatureCompareRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    private const string Columns = """
        BirthDetailId, PersonName, FeatureType, SubjectKey, ValueKey,
        GroupCode, GroupName, NatureCode, TypeCode, VariantCount, Variants
        """;

    /// <summary>Atoms of one feature type for the chosen people on one chart (a varga code; D1 by default).</summary>
    public IReadOnlyList<FeatureAtom> GetAtoms(string featureType, IReadOnlyCollection<int> personIds, string chartType = "D1")
    {
        if (personIds.Count == 0) return Array.Empty<FeatureAtom>();
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<FeatureAtom>($"""
            SELECT {Columns} FROM dbo.vw_Chart_Feature
            WHERE FeatureType = @featureType AND ChartType = @chartType AND BirthDetailId IN @personIds
            """, new { featureType, chartType, personIds }).ToList();
    }

    /// <summary>Everyone who has one subject (one yoga; or, with <paramref name="valueKey"/>, Mars in Aries), across all saved people.</summary>
    public IReadOnlyList<FeaturePerson> WhoHas(string featureType, string subjectKey, string chartType = "D1", string? valueKey = null)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<FeaturePerson>("""
            SELECT BirthDetailId, PersonName FROM dbo.vw_Chart_Feature
            WHERE FeatureType = @featureType AND SubjectKey = @subjectKey AND ChartType = @chartType
              AND (@valueKey IS NULL OR ValueKey = @valueKey)
            ORDER BY PersonName
            """, new { featureType, subjectKey, chartType, valueKey }).ToList();
    }

    /// <summary>How common each fact of one feature type is among the saved people, keyed by subject and value.</summary>
    public IReadOnlyDictionary<FeatureFact, FeatureRate> GetBaseRates(string featureType, string chartType = "D1")
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<(string SubjectKey, string ValueKey, int People, int Population)>("""
            SELECT SubjectKey, ValueKey, People, Population FROM dbo.vw_Feature_BaseRate
            WHERE FeatureType = @featureType AND ChartType = @chartType
            """, new { featureType, chartType })
            .ToDictionary(r => new FeatureFact(r.SubjectKey, r.ValueKey), r => new FeatureRate(r.People, r.Population));
    }

    /// <summary>The distinct subjects present for anyone, for pickers: key, group and how many people have each.</summary>
    public IReadOnlyList<FeatureSubjectCount> SubjectCounts(string featureType, string chartType = "D1")
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<FeatureSubjectCount>("""
            SELECT SubjectKey, MAX(GroupName) AS GroupName, MAX(NatureCode) AS NatureCode, COUNT(DISTINCT BirthDetailId) AS People
            FROM dbo.vw_Chart_Feature
            WHERE FeatureType = @featureType AND ChartType = @chartType
            GROUP BY SubjectKey
            ORDER BY MAX(GroupName), SubjectKey
            """, new { featureType, chartType }).ToList();
    }
}

public sealed record FeatureSubjectCount(string SubjectKey, string? GroupName, string? NatureCode, int People);
