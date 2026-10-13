using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Engines.PlanetNature;

/// <summary>Natural (naisargika) nature. Mixed only for Mercury when its companions are evenly split (PVR is silent there).</summary>
public enum NaturalNature { Benefic, Malefic, Mixed }

/// <param name="Nature">The reading used for the app's natural column (PVR §3.2.2).</param>
/// <param name="Why">The rule that decided it, with the companions that mattered.</param>
/// <param name="BphsDiffers">Set when BPHS ch.3 (and Phaladeepika, Saravali) would read the planet the other way; null otherwise.</param>
public sealed record NaturalNatureReading(string Planet, NaturalNature Nature, string Why, string? BphsDiffers);

/// <summary>
/// Natural benefic/malefic per planet in ONE chart. Jupiter and Venus benefic; Sun, Mars, Saturn, Rahu, Ketu malefic; the
/// Moon by paksha (waxing = benefic); Mercury by its companions in the same sign of this chart. The rule is PVR §3.2.2
/// (Mercury benefic alone or with more benefics, malefic with more malefics); BPHS ch.3, Phaladeepika ch.2 v.27 and Saravali
/// ch.3 v.9 make Mercury malefic when joined by any malefic, and BPHS lets a benefic conjunction turn a waning Moon benefic —
/// those readings are reported in <see cref="NaturalNatureReading.BphsDiffers"/>, not blended in.
/// The Moon's phase is taken from real longitudes, so it is the birth phase whatever chart is shown.
/// </summary>
public static class NaturalNatureReader
{
    public static IReadOnlyDictionary<string, NaturalNatureReading> Read(
        IEnumerable<ChartKeyDetail> grahas, bool? moonWaxing)
    {
        var list = grahas.Where(k => k.PointKind == "Graha" && k.Planet != "Ascendant").ToList();
        var waxing = moonWaxing ?? true;
        var result = new Dictionary<string, NaturalNatureReading>();

        NaturalNature Fixed(string planet) => planet switch
        {
            "Jupiter" or "Venus" => NaturalNature.Benefic,
            "Moon" => waxing ? NaturalNature.Benefic : NaturalNature.Malefic,
            _ => NaturalNature.Malefic,
        };

        foreach (var g in list)
        {
            var companions = list.Where(o => o.Planet != g.Planet && o.Sign == g.Sign && o.Planet != "Mercury").ToList();
            var benefics = companions.Where(o => Fixed(o.Planet) == NaturalNature.Benefic).Select(o => o.Planet).ToList();
            var malefics = companions.Where(o => Fixed(o.Planet) == NaturalNature.Malefic).Select(o => o.Planet).ToList();

            switch (g.Planet)
            {
                case "Mercury":
                {
                    var nature = malefics.Count > benefics.Count ? NaturalNature.Malefic
                        : benefics.Count > malefics.Count || (benefics.Count == 0 && malefics.Count == 0) ? NaturalNature.Benefic
                        : NaturalNature.Mixed;
                    var why = companions.Count == 0 ? "alone in its sign — benefic (PVR §3.2.2)"
                        : $"with {Join(benefics)} benefic and {Join(malefics)} malefic — " +
                          (nature == NaturalNature.Mixed ? "evenly split; PVR names only a majority" : $"{nature.ToString().ToLowerInvariant()} by majority (PVR §3.2.2)");
                    string? differs = malefics.Count > 0 && nature != NaturalNature.Malefic
                        ? $"malefic: joined by {string.Join(", ", malefics)} (BPHS ch.3, Phaladeepika, Saravali — any malefic companion)" : null;
                    result[g.Planet] = new(g.Planet, nature, why, differs);
                    break;
                }
                case "Moon":
                {
                    var nature = Fixed("Moon");
                    var why = moonWaxing is null ? "paksha unknown — waxing assumed"
                        : waxing ? "waxing (Shukla paksha) — benefic" : "waning (Krishna paksha) — malefic";
                    string? differs = !waxing && benefics.Count > 0
                        ? $"benefic: a waning Moon joined by {string.Join(", ", benefics)} turns benefic (BPHS ch.3 note)" : null;
                    result[g.Planet] = new(g.Planet, nature, why, differs);
                    break;
                }
                default:
                {
                    var nature = Fixed(g.Planet);
                    var why = g.Planet is "Rahu" or "Ketu" ? "node — malefic in every school here" : "fixed natural " + nature.ToString().ToLowerInvariant();
                    result[g.Planet] = new(g.Planet, nature, why, null);
                    break;
                }
            }
        }
        return result;
    }

    private static string Join(List<string> planets) => planets.Count == 0 ? "no" : string.Join("+", planets);

    /// <summary>The rule-class token for a reading: null when the planet is Mixed (no rule applies).</summary>
    public static string? RuleClass(NaturalNatureReading r) => r.Nature switch
    {
        NaturalNature.Benefic => "NATURAL_BENEFIC",
        NaturalNature.Malefic => "NATURAL_MALEFIC",
        _ => null,
    };
}
