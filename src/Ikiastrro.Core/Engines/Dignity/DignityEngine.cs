using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.Dignity;

/// <summary>One planet's classical dignity within a computed D1 chart.</summary>
public record DignityResult(
    string? OwnSigns,
    string? ExaltationSign,
    string? DebilitationSign,
    string? MoolatrikonaSign,
    string? MoolatrikonaRange,
    string? SignLordPlanet,
    string? DignityStatus);

/// <summary>
/// Classical Parashari planetary dignity: exaltation (Uchcha), debilitation (Neecha), Moolatrikona,
/// own sign (Swakshetra), and Panchadha Maitri (five-fold compound relationship — Great Friend /
/// Friend / Neutral / Enemy / Great Enemy), combining Naisargika Maitri (fixed natural friendship)
/// with Tatkalika Maitri (temporary friendship, computed per-chart from sign distance).
///
/// Pure classical reference-table lookups, applied to the signs this project's own AstroMath/
/// SwissEphemerisProvider pipeline computes — not derived from any engine's own relationship helpers.
///
/// Rahu/Ketu: exaltation/debilitation are genuinely disputed across classical texts. This project uses
/// the Parashari convention (Rahu exalted Taurus/debilitated Scorpio, Ketu exalted Scorpio/debilitated
/// Taurus), per rammyps's decision (2026-08-24, ikiastrro.md). Rahu/Ketu are shadow points,
/// not part of the Naisargika Maitri table, and have no Moolatrikona or own sign in standard Parashari
/// texts — their DignityStatus is limited to Exalted/Debilitated/Neutral.
/// </summary>
public static class DignityEngine
{
    private static readonly Dictionary<string, ZodiacName[]> OwnSigns = new()
    {
        ["Sun"] = new[] { ZodiacName.Leo },
        ["Moon"] = new[] { ZodiacName.Cancer },
        ["Mars"] = new[] { ZodiacName.Aries, ZodiacName.Scorpio },
        ["Mercury"] = new[] { ZodiacName.Gemini, ZodiacName.Virgo },
        ["Jupiter"] = new[] { ZodiacName.Sagittarius, ZodiacName.Pisces },
        ["Venus"] = new[] { ZodiacName.Taurus, ZodiacName.Libra },
        ["Saturn"] = new[] { ZodiacName.Capricornus, ZodiacName.Aquarius }
    };

    private static readonly Dictionary<string, ZodiacName> ExaltationSign = new()
    {
        ["Sun"] = ZodiacName.Aries,
        ["Moon"] = ZodiacName.Taurus,
        ["Mars"] = ZodiacName.Capricornus,
        ["Mercury"] = ZodiacName.Virgo,
        ["Jupiter"] = ZodiacName.Cancer,
        ["Venus"] = ZodiacName.Pisces,
        ["Saturn"] = ZodiacName.Libra,
        ["Rahu"] = ZodiacName.Taurus,     // Parashari convention (chosen 2026-08-24)
        ["Ketu"] = ZodiacName.Scorpio
    };

    private static readonly Dictionary<string, ZodiacName> DebilitationSign = new()
    {
        ["Sun"] = ZodiacName.Libra,
        ["Moon"] = ZodiacName.Scorpio,
        ["Mars"] = ZodiacName.Cancer,
        ["Mercury"] = ZodiacName.Pisces,
        ["Jupiter"] = ZodiacName.Capricornus,
        ["Venus"] = ZodiacName.Virgo,
        ["Saturn"] = ZodiacName.Aries,
        ["Rahu"] = ZodiacName.Scorpio,
        ["Ketu"] = ZodiacName.Taurus
    };

    // (Sign, low degree, high degree) — standard BPHS Moolatrikona ranges. Classical planets only.
    private static readonly Dictionary<string, (ZodiacName Sign, double Low, double High)> Moolatrikona = new()
    {
        ["Sun"] = (ZodiacName.Leo, 0, 20),
        ["Moon"] = (ZodiacName.Taurus, 4, 30),
        ["Mars"] = (ZodiacName.Aries, 0, 12),
        ["Mercury"] = (ZodiacName.Virgo, 16, 20),
        ["Jupiter"] = (ZodiacName.Sagittarius, 0, 10),
        ["Venus"] = (ZodiacName.Libra, 0, 15),
        ["Saturn"] = (ZodiacName.Aquarius, 0, 20)
    };

    /// <summary>Naisargika Maitri (fixed natural friendship) — deliberately asymmetric, per BPHS.</summary>
    private static readonly Dictionary<string, (string[] Friends, string[] Neutrals, string[] Enemies)> NaturalRelationship = new()
    {
        ["Sun"] = (new[] { "Moon", "Mars", "Jupiter" }, new[] { "Mercury" }, new[] { "Venus", "Saturn" }),
        ["Moon"] = (new[] { "Sun", "Mercury" }, new[] { "Mars", "Jupiter", "Venus", "Saturn" }, Array.Empty<string>()),
        ["Mars"] = (new[] { "Sun", "Moon", "Jupiter" }, new[] { "Venus", "Saturn" }, new[] { "Mercury" }),
        ["Mercury"] = (new[] { "Sun", "Venus" }, new[] { "Mars", "Jupiter", "Saturn" }, new[] { "Moon" }),
        ["Jupiter"] = (new[] { "Sun", "Moon", "Mars" }, new[] { "Saturn" }, new[] { "Mercury", "Venus" }),
        ["Venus"] = (new[] { "Mercury", "Saturn" }, new[] { "Mars", "Jupiter" }, new[] { "Sun", "Moon" }),
        ["Saturn"] = (new[] { "Mercury", "Venus" }, new[] { "Jupiter" }, new[] { "Sun", "Moon", "Mars" })
    };

    private static readonly ZodiacName[] SignOrder =
    {
        ZodiacName.Aries, ZodiacName.Taurus, ZodiacName.Gemini, ZodiacName.Cancer,
        ZodiacName.Leo, ZodiacName.Virgo, ZodiacName.Libra, ZodiacName.Scorpio,
        ZodiacName.Sagittarius, ZodiacName.Capricornus, ZodiacName.Aquarius, ZodiacName.Pisces
    };

    /// <summary>Ruler of a sign — reverse lookup of OwnSigns.</summary>
    public static string GetSignLord(ZodiacName sign) =>
        OwnSigns.First(kv => kv.Value.Contains(sign)).Key;

    /// <summary>The sign occupying a given house (1-12), Whole Sign counted from the Ascendant sign.</summary>
    public static ZodiacName GetHouseSign(ZodiacName ascendantSign, int houseNumber)
    {
        var ascendantIndex = Array.IndexOf(SignOrder, ascendantSign);
        return SignOrder[(ascendantIndex + houseNumber - 1) % 12];
    }

    /// <summary>1-12 distance counting from sign A to sign B, same convention as CountFromSignToSign (same sign = 1).</summary>
    private static int SignDistance(ZodiacName from, ZodiacName to)
    {
        var fromIndex = Array.IndexOf(SignOrder, from);
        var toIndex = Array.IndexOf(SignOrder, to);
        return ((toIndex - fromIndex + 12) % 12) + 1;
    }

    /// <summary>Tatkalika Maitri (temporary friendship): friend if 2,3,4,10,11,12 signs apart; else enemy.</summary>
    private static bool IsTemporaryFriend(ZodiacName planetSign, ZodiacName otherSign)
    {
        var distance = SignDistance(planetSign, otherSign);
        return distance is 2 or 3 or 4 or 10 or 11 or 12;
    }

    /// <summary>Combines natural + temporary friendship into the five-fold Panchadha Maitri result.</summary>
    private static string CombineToPanchadha(bool naturalFriend, bool naturalEnemy, bool temporaryFriend)
    {
        if (naturalFriend) return temporaryFriend ? "Great Friend" : "Neutral";
        if (naturalEnemy) return temporaryFriend ? "Neutral" : "Great Enemy";
        return temporaryFriend ? "Friend" : "Enemy"; // natural neutral
    }

    /// <summary>
    /// Evaluates one planet's dignity. <paramref name="allSigns"/> must contain every classical
    /// planet's current sign (needed to locate the sign-lord's own position for Panchadha Maitri).
    /// Returns all-null fields for "Ascendant" (Lagna has no dignity concept) except SignLordPlanet,
    /// which is still meaningful (the Lagna lord).
    /// <paramref name="degreeInSign"/> is null for non-D1 chart types — Moolatrikona is specifically a
    /// D1 (continuous-degree) concept, so it's simply never assigned for a varga chart; the sign falls
    /// through to Exalted/Debilitated/Own Sign/Panchadha Maitri exactly as it would for a planet outside
    /// its Moolatrikona range.
    /// </summary>
    public static DignityResult Evaluate(string planet, ZodiacName sign, double? degreeInSign, IReadOnlyDictionary<string, ZodiacName> allSigns)
    {
        if (planet == "Ascendant")
        {
            return new DignityResult(null, null, null, null, null, GetSignLord(sign), null);
        }

        var isShadowPlanet = planet is "Rahu" or "Ketu";
        var exaltation = ExaltationSign[planet];
        var debilitation = DebilitationSign[planet];
        var signLord = GetSignLord(sign);

        string ownSignsDisplay = isShadowPlanet ? "" : string.Join(", ", OwnSigns[planet].Select(s => s.ToString()));
        string? moolatrikonaSign = null;
        string? moolatrikonaRange = null;
        if (!isShadowPlanet && Moolatrikona.TryGetValue(planet, out var moola))
        {
            moolatrikonaSign = moola.Sign.ToString();
            moolatrikonaRange = $"{moola.Low}-{moola.High}";
        }

        string dignityStatus;
        if (sign == exaltation)
        {
            dignityStatus = "Exalted";
        }
        else if (sign == debilitation)
        {
            dignityStatus = "Debilitated";
        }
        else if (!isShadowPlanet && moolatrikonaSign is not null && sign.ToString() == moolatrikonaSign
                 && degreeInSign is not null && degreeInSign >= Moolatrikona[planet].Low && degreeInSign <= Moolatrikona[planet].High)
        {
            dignityStatus = "Moolatrikona";
        }
        else if (!isShadowPlanet && OwnSigns[planet].Contains(sign))
        {
            dignityStatus = "Own Sign";
        }
        else if (isShadowPlanet)
        {
            // Rahu/Ketu aren't part of the Naisargika Maitri table in standard Parashari texts —
            // no finer-grained friend/enemy tiering for them here.
            dignityStatus = "Neutral";
        }
        else
        {
            var (friends, _, enemies) = NaturalRelationship[planet];
            var naturalFriend = friends.Contains(signLord);
            var naturalEnemy = enemies.Contains(signLord);
            var temporaryFriend = IsTemporaryFriend(sign, allSigns[signLord]);
            dignityStatus = CombineToPanchadha(naturalFriend, naturalEnemy, temporaryFriend);
        }

        return new DignityResult(
            isShadowPlanet ? null : ownSignsDisplay,
            exaltation.ToString(),
            debilitation.ToString(),
            moolatrikonaSign,
            moolatrikonaRange,
            signLord,
            dignityStatus);
    }
}
