using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;

namespace Ikiastrro.Core.Engines.Strength;

/// <summary>
/// P.V.R. Table 6 dignity segments plus the Raman/BPHS Saptavargaja relationship ladder.
/// Dignity and relationship remain separate axes; only Saptavargaja maps the latter to points.
/// </summary>
public sealed record PvrDignityResult(
    string DignityTypeCode,
    int DignityScore,
    string SignLord,
    string? CompoundRelationshipCode,
    int? RelationshipScore,
    double SaptavargajaPoints);

public static class PvrDignityEvaluator
{
    private sealed record Segment(ZodiacName Sign, double Start, double End, string Code, int Score);

    private static readonly IReadOnlyDictionary<PlanetName, Segment[]> Segments =
        new Dictionary<PlanetName, Segment[]>
        {
            [PlanetName.Sun] = new[] { S(ZodiacName.Aries, "EXALTED", 4), S(ZodiacName.Libra, "DEBILITATED", -2), S(ZodiacName.Leo, "MOOLATRIKONA", 0, 20, 3), S(ZodiacName.Leo, "OWN", 20, 30, 2) },
            [PlanetName.Moon] = new[] { S(ZodiacName.Taurus, "EXALTED", 0, 3, 4), S(ZodiacName.Taurus, "MOOLATRIKONA", 3, 30, 3), S(ZodiacName.Scorpio, "DEBILITATED", -2), S(ZodiacName.Cancer, "OWN", 2) },
            [PlanetName.Mars] = new[] { S(ZodiacName.Capricornus, "EXALTED", 4), S(ZodiacName.Cancer, "DEBILITATED", -2), S(ZodiacName.Aries, "MOOLATRIKONA", 0, 12, 3), S(ZodiacName.Aries, "OWN", 12, 30, 2), S(ZodiacName.Scorpio, "OWN", 2) },
            [PlanetName.Mercury] = new[] { S(ZodiacName.Virgo, "EXALTED", 0, 15, 4), S(ZodiacName.Virgo, "MOOLATRIKONA", 15, 20, 3), S(ZodiacName.Virgo, "OWN", 20, 30, 2), S(ZodiacName.Gemini, "OWN", 2), S(ZodiacName.Pisces, "DEBILITATED", -2) },
            [PlanetName.Jupiter] = new[] { S(ZodiacName.Cancer, "EXALTED", 4), S(ZodiacName.Capricornus, "DEBILITATED", -2), S(ZodiacName.Sagittarius, "MOOLATRIKONA", 0, 10, 3), S(ZodiacName.Sagittarius, "OWN", 10, 30, 2), S(ZodiacName.Pisces, "OWN", 2) },
            [PlanetName.Venus] = new[] { S(ZodiacName.Pisces, "EXALTED", 4), S(ZodiacName.Virgo, "DEBILITATED", -2), S(ZodiacName.Libra, "MOOLATRIKONA", 0, 15, 3), S(ZodiacName.Libra, "OWN", 15, 30, 2), S(ZodiacName.Taurus, "OWN", 2) },
            [PlanetName.Saturn] = new[] { S(ZodiacName.Libra, "EXALTED", 4), S(ZodiacName.Aries, "DEBILITATED", -2), S(ZodiacName.Aquarius, "MOOLATRIKONA", 0, 20, 3), S(ZodiacName.Aquarius, "OWN", 20, 30, 2), S(ZodiacName.Capricornus, "OWN", 2) },
            [PlanetName.Rahu] = new[] { S(ZodiacName.Gemini, "EXALTED", 4), S(ZodiacName.Sagittarius, "DEBILITATED", -2), S(ZodiacName.Virgo, "MOOLATRIKONA", 3), S(ZodiacName.Aquarius, "OWN", 2) },
            [PlanetName.Ketu] = new[] { S(ZodiacName.Sagittarius, "EXALTED", 4), S(ZodiacName.Gemini, "DEBILITATED", -2), S(ZodiacName.Pisces, "MOOLATRIKONA", 3), S(ZodiacName.Scorpio, "OWN", 2) }
        };

    private static readonly IReadOnlyDictionary<PlanetName, (PlanetName[] Friends, PlanetName[] Enemies)> Natural =
        new Dictionary<PlanetName, (PlanetName[], PlanetName[])>
        {
            [PlanetName.Sun] = (new[] { PlanetName.Moon, PlanetName.Mars, PlanetName.Jupiter }, new[] { PlanetName.Venus, PlanetName.Saturn }),
            [PlanetName.Moon] = (new[] { PlanetName.Sun, PlanetName.Mercury }, Array.Empty<PlanetName>()),
            [PlanetName.Mars] = (new[] { PlanetName.Sun, PlanetName.Moon, PlanetName.Jupiter }, new[] { PlanetName.Mercury }),
            [PlanetName.Mercury] = (new[] { PlanetName.Sun, PlanetName.Venus }, new[] { PlanetName.Moon }),
            [PlanetName.Jupiter] = (new[] { PlanetName.Sun, PlanetName.Moon, PlanetName.Mars }, new[] { PlanetName.Mercury, PlanetName.Venus }),
            [PlanetName.Venus] = (new[] { PlanetName.Mercury, PlanetName.Saturn }, new[] { PlanetName.Sun, PlanetName.Moon }),
            [PlanetName.Saturn] = (new[] { PlanetName.Mercury, PlanetName.Venus }, new[] { PlanetName.Sun, PlanetName.Moon, PlanetName.Mars })
        };

    public static PvrDignityResult Evaluate(PlanetName planet, ZodiacName sign, double degreeInSign,
        IReadOnlyDictionary<string, ZodiacName>? chartSigns = null)
    {
        var segment = Segments[planet].FirstOrDefault(x => x.Sign == sign && degreeInSign >= x.Start && degreeInSign < x.End);
        var lordName = HouseEngine.GetSignLord(sign);
        var dignityCode = segment?.Code ?? "NEUTRAL";
        var dignityScore = segment?.Score ?? 0;
        if (!Enum.TryParse<PlanetName>(lordName, out var lord) || !Natural.ContainsKey(planet) || chartSigns is null)
            return new PvrDignityResult(dignityCode, dignityScore, lordName, null, null, SaptavargajaPoints(dignityCode, null));

        var tempFriend = chartSigns.TryGetValue(lordName, out var lordSign) && TemporaryFriend(sign, lordSign);
        var natural = Natural[planet];
        var naturalKind = natural.Friends.Contains(lord) ? "FRIEND" : natural.Enemies.Contains(lord) ? "ENEMY" : "NEUTRAL";
        var code = naturalKind switch
        {
            "FRIEND" when tempFriend => "ADHIMITRA",
            "FRIEND" => "SAMA",
            "ENEMY" when tempFriend => "SAMA",
            "ENEMY" => "ADHISHATRU",
            _ when tempFriend => "MITRA",
            _ => "SHATRU"
        };
        var relationshipScore = code switch { "ADHIMITRA" => 2, "MITRA" => 1, "SAMA" => 0, "SHATRU" => -1, _ => -2 };
        return new PvrDignityResult(dignityCode, dignityScore, lordName, code, relationshipScore,
            SaptavargajaPoints(dignityCode, code));
    }

    public static double SaptavargajaPoints(string dignityCode, string? compoundCode) => dignityCode switch
    {
        "MOOLATRIKONA" => 45.0,
        "OWN" => 30.0,
        "EXALTED" or "DEBILITATED" or "NEUTRAL" => compoundCode switch
        {
            "ADHIMITRA" => 22.5, "MITRA" => 15.0, "SAMA" => 7.5,
            "SHATRU" => 3.75, "ADHISHATRU" => 1.875, _ => 7.5
        },
        _ => 7.5
    };

    private static Segment S(ZodiacName sign, string code, int score) => new(sign, 0, 30, code, score);
    private static Segment S(ZodiacName sign, string code, double start, double end, int score) => new(sign, start, end, code, score);
    private static bool TemporaryFriend(ZodiacName planetSign, ZodiacName lordSign)
    {
        var from = Array.IndexOf(HouseEngine.SignOrder, planetSign);
        var to = Array.IndexOf(HouseEngine.SignOrder, lordSign);
        var distance = ((to - from + 12) % 12) + 1;
        return distance is 2 or 3 or 4 or 10 or 11 or 12;
    }
}
