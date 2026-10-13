namespace Ikiastrro.Core.Engines.PlanetNature;

public enum PlacementEffect { Good, Challenging, Mixed, NotStated }

/// <param name="Why">The rule applied (with its source's note), or the reason the school states nothing.</param>
public sealed record SchoolPlacementReading(string SchoolCode, PlacementEffect Effect, string Why);

/// <summary>
/// How a planet fares in the house it occupies, per school. PVR's rule is written for FUNCTIONAL nature (a functional benefic in
/// a quadrant or trine is good; a functional malefic in the 3rd or a dusthāna is good); Saravali, BPHS and Phaladeepika state
/// theirs for NATURAL nature. Each school reads its own basis, so a natural benefic and its functional verdict can legitimately
/// land on different sides. Strength is not applied: every school says a strong planet does better.
/// </summary>
public static class SchoolPlacement
{
    public static IReadOnlyList<SchoolPlacementReading> ReadAll(
        int house, NaturalNatureReading natural, SchoolFunctionalReading pvrFunctional, SchoolRuleBook rules)
    {
        var naturalClass = NaturalNatureReader.RuleClass(natural);
        var list = new List<SchoolPlacementReading>();
        foreach (var school in rules.WithTopic("PLACEMENT"))
        {
            string? cls = school.Basis == "FUNCTIONAL"
                ? pvrFunctional.Verdict switch
                {
                    SchoolFunctional.Benefic or SchoolFunctional.Yogakaraka => "FUNCTIONAL_BENEFIC",
                    SchoolFunctional.Malefic => "FUNCTIONAL_MALEFIC",
                    _ => null,
                }
                : naturalClass;

            if (cls is null)
            {
                list.Add(new(school.Code, PlacementEffect.NotStated,
                    school.Basis == "FUNCTIONAL"
                        ? $"{school.ShortName} states placement rules for functional benefics and malefics only; this planet is {pvrFunctional.Verdict.ToString().ToLowerInvariant()}"
                        : "Natural nature is evenly split, so no rule applies"));
                continue;
            }
            var rule = rules.Find(school.Code, "PLACEMENT", cls, natural.Planet, house);
            if (rule is null)
            {
                list.Add(new(school.Code, PlacementEffect.NotStated, $"{school.ShortName} states no rule for a {Label(cls)} in H{house}"));
                continue;
            }
            var effect = rule.Result switch
            {
                "GOOD" => PlacementEffect.Good,
                "CHALLENGING" => PlacementEffect.Challenging,
                _ => PlacementEffect.Mixed,
            };
            list.Add(new(school.Code, effect, $"{Label(cls)} in H{house}: {rule.Note ?? rule.Result.ToLowerInvariant()}"));
        }
        return list;
    }

    private static string Label(string cls) => cls.Replace('_', ' ').ToLowerInvariant();
}
