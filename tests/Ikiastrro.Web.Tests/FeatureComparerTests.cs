using Ikiastrro.Data;
using Xunit;

namespace Ikiastrro.Web.Tests;

public sealed class FeatureComparerTests
{
    private static FeatureAtom Yoga(int person, string key, string? group = "Set A", string? nature = "AUSPICIOUS") =>
        new(person, $"P{person}", "YOGA", key, "PRESENT", group, group, nature, null, 1, null);

    private static FeatureAtom Planet(int person, string planet, string sign, string type = "PLANET_SIGN") =>
        new(person, $"P{person}", type, planet, sign, "PLACEMENT", "Placement", null, null, 1, null);

    private static readonly FeatureAtom[] Atoms =
    {
        Yoga(1, "YOGA_ADHI"), Yoga(1, "YOGA_RUCHAKA"), Yoga(1, "YOGA_KEMADRUMA", "Set B", "INAUSPICIOUS"),
        Yoga(2, "YOGA_ADHI"), Yoga(2, "YOGA_BHADRA"),
        Yoga(3, "YOGA_ADHI"), Yoga(3, "YOGA_RUCHAKA"),
    };

    private static FeatureFact F(string subject, string value = "PRESENT") => new(subject, value);

    [Fact]
    public void AllListsEverySubjectOnceWithItsPeople()
    {
        var rows = FeatureComparer.Compare(Atoms, new[] { 1, 2, 3 });

        Assert.Equal(4, rows.Count);
        Assert.Equal(new[] { 1, 2, 3 }, rows.Single(r => r.SubjectKey == "YOGA_ADHI").PersonIds.Order());
        Assert.Equal(new[] { 1, 3 }, rows.Single(r => r.SubjectKey == "YOGA_RUCHAKA").PersonIds.Order());
    }

    [Fact]
    public void SharedKeepsOnlyWhatEveryChosenPersonHas()
    {
        Assert.Equal(new[] { "YOGA_ADHI" }, FeatureComparer.Compare(Atoms, new[] { 1, 2, 3 }, FeatureCompareMode.Shared).Select(r => r.SubjectKey));
        Assert.Equal(new[] { "YOGA_ADHI", "YOGA_RUCHAKA" }, FeatureComparer.Compare(Atoms, new[] { 1, 3 }, FeatureCompareMode.Shared).Select(r => r.SubjectKey));
    }

    [Fact]
    public void DifferencesDropWhatEveryoneHas()
    {
        var keys = FeatureComparer.Compare(Atoms, new[] { 1, 2, 3 }, FeatureCompareMode.Differences).Select(r => r.SubjectKey).Order();
        Assert.Equal(new[] { "YOGA_BHADRA", "YOGA_KEMADRUMA", "YOGA_RUCHAKA" }, keys);
    }

    [Fact]
    public void PeopleNotChosenAreIgnored()
    {
        var rows = FeatureComparer.Compare(Atoms, new[] { 2 });
        Assert.Equal(new[] { "YOGA_ADHI", "YOGA_BHADRA" }, rows.Select(r => r.SubjectKey).Order());
        Assert.All(rows, r => Assert.Equal(new[] { 2 }, r.PersonIds));
    }

    [Fact]
    public void RowsAreGroupedThenMostSharedFirst()
    {
        var rows = FeatureComparer.Compare(Atoms, new[] { 1, 2, 3 });
        Assert.Equal(new[] { "YOGA_ADHI", "YOGA_RUCHAKA", "YOGA_BHADRA", "YOGA_KEMADRUMA" }, rows.Select(r => r.SubjectKey));
    }

    [Fact]
    public void OverlapSplitsSharedAndExclusiveSubjects()
    {
        var o = FeatureComparer.Overlap(Atoms, 1, 2, "YOGA");

        Assert.Equal(new[] { F("YOGA_ADHI") }, o.Shared);
        Assert.Equal(new[] { F("YOGA_KEMADRUMA"), F("YOGA_RUCHAKA") }, o.OnlyA);
        Assert.Equal(new[] { F("YOGA_BHADRA") }, o.OnlyB);
        Assert.Equal(1, o.Count);
    }

    [Fact]
    public void OverlapOnlyCountsTheRequestedFeatureType()
    {
        var atoms = Atoms.Append(Planet(1, "MARS", "ARIES")).Append(Planet(2, "MARS", "ARIES")).ToList();

        Assert.Equal(new[] { F("YOGA_ADHI") }, FeatureComparer.Overlap(atoms, 1, 2, "YOGA").Shared);
        Assert.Equal(new[] { F("MARS", "ARIES") }, FeatureComparer.Overlap(atoms, 1, 2, "PLANET_SIGN").Shared);
    }

    [Fact]
    public void ValuedFactsAreSharedOnlyWhenTheValueAgrees()
    {
        var atoms = new[]
        {
            Planet(1, "SUN", "ARIES"), Planet(2, "SUN", "ARIES"),
            Planet(1, "MARS", "ARIES"), Planet(2, "MARS", "LEO"),
        };

        var shared = FeatureComparer.Compare(atoms, new[] { 1, 2 }, FeatureCompareMode.Shared);
        var differ = FeatureComparer.Compare(atoms, new[] { 1, 2 }, FeatureCompareMode.Differences);

        Assert.Equal(new[] { "SUN" }, shared.Select(r => r.SubjectKey));
        Assert.Equal(new[] { "MARS" }, differ.Select(r => r.SubjectKey));
        var mars = differ.Single();
        Assert.Equal("ARIES", mars.Values[1]);
        Assert.Equal("LEO", mars.Values[2]);
        Assert.Equal(1, mars.SharedBy);
    }

    [Fact]
    public void SharedByIsTheLargestGroupSharingOneValue()
    {
        var atoms = new[] { Planet(1, "MOON", "CANCER"), Planet(2, "MOON", "CANCER"), Planet(3, "MOON", "LEO") };

        var row = Assert.Single(FeatureComparer.Compare(atoms, new[] { 1, 2, 3 }));
        Assert.Equal(2, row.SharedBy);
    }

    [Fact]
    public void OverlapOfValuedFactsNeedsSubjectAndValueToMatch()
    {
        var atoms = new[]
        {
            Planet(1, "SUN", "ARIES"), Planet(2, "SUN", "ARIES"),
            Planet(1, "MARS", "ARIES"), Planet(2, "MARS", "LEO"),
        };

        var o = FeatureComparer.Overlap(atoms, 1, 2, "PLANET_SIGN");

        Assert.Equal(new[] { F("SUN", "ARIES") }, o.Shared);
        Assert.Equal(new[] { F("MARS", "ARIES") }, o.OnlyA);
        Assert.Equal(new[] { F("MARS", "LEO") }, o.OnlyB);
    }

    [Fact]
    public void RateIsPeopleOverPopulationAndFormatsAsAPercentage()
    {
        var rate = new FeatureRate(18, 47);

        Assert.Equal(18d / 47, rate.Rate, 6);
        Assert.Equal("38%", rate.Percent);
        Assert.Equal(0, new FeatureRate(0, 0).Rate);
    }
}
