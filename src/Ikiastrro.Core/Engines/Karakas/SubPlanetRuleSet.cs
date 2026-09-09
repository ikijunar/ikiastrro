namespace Ikiastrro.Core.Engines.Karakas;

public sealed record SubPlanetSunRule(string Code, int SequenceNo, string Operation, string? InputCode, double? OffsetDegrees);
public sealed record SubPlanetTimeRule(string Code, string RulingPlanet, double PartFraction, int DivisionCount);
public sealed record SubPlanetPartRule(string DayNight, int Weekday, int PartNumber, string? RulingPlanet);

/// <summary>Versioned migration 27 inputs; source SRC_PVR_INTEGRATED.</summary>
public sealed record SubPlanetRuleSet(int RuleSetId, IReadOnlyList<SubPlanetSunRule> SunRules,
    IReadOnlyList<SubPlanetTimeRule> TimeRules, IReadOnlyList<SubPlanetPartRule> Parts)
{
    public void Validate()
    {
        if (SunRules.Count != 5 || TimeRules.Count != 6 || Parts.Count != 112 ||
            SunRules.Select(r => r.Code).Concat(TimeRules.Select(r => r.Code)).Distinct().Count() != 11)
            throw new InvalidOperationException("Expected all 11 sub-planets and 112 day/night part rules.");
        var available = new HashSet<string>();
        foreach (var r in SunRules.OrderBy(r => r.SequenceNo))
        {
            if ((r.InputCode is not null && !available.Contains(r.InputCode)) ||
                r.Operation is not ("ADD" or "COMPLEMENT_360") ||
                (r.Operation == "ADD" && (r.OffsetDegrees is null || !double.IsFinite(r.OffsetDegrees.Value))))
                throw new InvalidOperationException($"Invalid Sun chain rule: {r.Code}.");
            available.Add(r.Code);
        }
        if (SunRules.Select(r => r.SequenceNo).Distinct().Count() != 5)
            throw new InvalidOperationException("Sun chain sequence numbers must be unique.");
        foreach (var period in new[] { "DAY", "NIGHT" })
        for (var weekday = 0; weekday < 7; weekday++)
        {
            var parts = Parts.Where(p => p.DayNight == period && p.Weekday == weekday).ToArray();
            if (!parts.Select(p => p.PartNumber).Order().SequenceEqual(Enumerable.Range(1, 8)) ||
                parts.Count(p => p.RulingPlanet is null) != 1 ||
                parts.Where(p => p.RulingPlanet is not null).Select(p => p.RulingPlanet).Distinct().Count() != 7)
                throw new InvalidOperationException($"Invalid part rulers for {period}, weekday {weekday}.");
            foreach (var r in TimeRules)
                if (r.DivisionCount != 8 || !double.IsFinite(r.PartFraction) || r.PartFraction < 0 || r.PartFraction >= 1 ||
                    parts.Count(p => p.RulingPlanet == r.RulingPlanet) != 1)
                    throw new InvalidOperationException($"Invalid time rule: {r.Code}.");
        }
    }
}
