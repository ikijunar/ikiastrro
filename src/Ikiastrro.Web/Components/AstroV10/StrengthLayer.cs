using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Data;

namespace Ikiastrro.Web.Components.AstroV10;

/// <summary>One graha's strength, from the figures the classic Strength tab already shows: Ṣaḍbala as a share of its required
/// minimum, Iṣṭa/Kaṣṭa, and its own Bhinnāṣṭakavarga bindus in the sign it stands in (null for Rahu and Ketu, which have none).</summary>
public sealed record PlanetStrengthFacts(string Planet, decimal? Rupas, decimal? PercentOfMinimum, StrengthTier Tier,
    decimal? Ishta, decimal? Kashta, int? OwnBindus, StrengthTier BindusTier);

/// <summary>One house's strength: the Sarvāṣṭakavarga bindus of its sign and its Bhava Bala in rūpas.</summary>
public sealed record HouseStrength(int House, int? SavBindus, StrengthTier SavTier, decimal? BhavaRupas, StrengthTier BhavaTier);

/// <summary>A present yoga reduced to what a chip needs. <see cref="WeakestPercent"/> is the Ṣaḍbala % of the weakest planet that
/// makes it — a yoga is only as strong as its weakest cause.</summary>
public sealed record YogaChipFact(string Code, string Name, string Nature, string? Based, IReadOnlyList<string> Planets,
    decimal? WeakestPercent, string? Rule, string? Inference);

/// <summary>A planet's nakshatra and pada, read off its true longitude so it is the same in every varga.</summary>
public sealed record NakshatraPlace(string Name, int Pada)
{
    public static NakshatraPlace Of(ChartKeyDetail row)
    {
        var (nak, pada) = AstroMath.GetNakshatraAndPada(row.NirayanaLongitudeDegrees);
        return new NakshatraPlace(string.IsNullOrEmpty(row.Nakshatra) ? nak.ToString() : row.Nakshatra, row.NakshatraPada ?? pada);
    }
}

/// <summary>
/// The strength layer of AF-v10: planet strength, Ashtakavarga and yogas, folded into the same planet and house picks as the
/// argala and avastha layers. Pure reshaping of rows the classic page already reads — no astrology is calculated here, and the
/// cut-offs are <see cref="StrengthBands"/>, the same ones Astro Facts and Key Inference use, so one planet never gets two labels.
/// </summary>
public static class StrengthLayer
{
    public static IReadOnlyDictionary<string, PlanetStrengthFacts> Planets(
        IReadOnlyList<ShadbalaSummaryRow> shadbala, IReadOnlyList<AshtakavargaRow> ashtakavarga,
        string chartType, IReadOnlyList<ChartKeyDetail> grahas)
    {
        var result = new Dictionary<string, PlanetStrengthFacts>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in grahas.Where(k => k.Planet != "Ascendant"))
        {
            var s = shadbala.FirstOrDefault(x => x.Planet.Equals(g.Planet, StringComparison.OrdinalIgnoreCase));
            var bav = SignNumber(g.Sign) is { } sign
                ? ashtakavarga.FirstOrDefault(r => r.ChartType.Equals(chartType, StringComparison.OrdinalIgnoreCase)
                    && r.RecipientCode.Equals(g.Planet, StringComparison.OrdinalIgnoreCase) && r.SignNumber == sign)?.BinduCount
                : null;
            result[g.Planet] = new PlanetStrengthFacts(g.Planet, s?.ShadbalaRupas, s?.PercentOfMinimum,
                StrengthBands.ShadbalaPercentOfMinimum.Classify(s?.PercentOfMinimum), s?.IshtaBala, s?.KashtaBala,
                bav is null ? null : (int)bav, StrengthBands.BhinnaAshtakavargaBindus.Classify(bav));
        }
        return result;
    }

    public static IReadOnlyList<HouseStrength> Houses(
        IReadOnlyList<AshtakavargaRow> ashtakavarga, IReadOnlyList<BhavaBalaSummaryRow> bhava,
        string chartType, string ascendantSign)
    {
        var asc = SignNumber(ascendantSign) ?? 1;
        var rows = ashtakavarga.Where(r => r.ChartType.Equals(chartType, StringComparison.OrdinalIgnoreCase)).ToList();
        var list = new List<HouseStrength>();
        for (var house = 1; house <= 12; house++)
        {
            var sign = (asc - 1 + house - 1) % 12 + 1;
            var inSign = rows.Where(r => r.SignNumber == sign).ToList();
            int? sav = inSign.Count == 0 ? null
                : inSign.Select(r => (int?)r.SarvaBindus).FirstOrDefault(v => v is not null) ?? inSign.Sum(r => (int)r.BinduCount);
            var rupas = chartType == "D1" ? bhava.FirstOrDefault(b => b.HouseNumber == house)?.BhavaBalaRupas : null;
            list.Add(new HouseStrength(house, sav, StrengthBands.SarvaAshtakavargaBindus.Classify(sav), rupas,
                StrengthBands.BhavaBalaRupas.Classify(rupas)));
        }
        return list;
    }

    /// <summary>The yogas present in this chart, once per yoga code (several catalogues list the same yoga), with the planets
    /// that make each. Good first, then mixed, then challenging; strongest delivery first within a nature.</summary>
    public static IReadOnlyList<YogaChipFact> Yogas(
        IReadOnlyList<YogaEvaluationRow> rows, IReadOnlyDictionary<int, PlanetName> houseLords,
        IReadOnlyDictionary<PlanetName, YogaPlacement> placements, IReadOnlyList<ShadbalaSummaryRow> shadbala)
    {
        decimal? Pct(PlanetName p) => shadbala.FirstOrDefault(s => s.Planet.Equals(p.ToString(), StringComparison.OrdinalIgnoreCase))?.PercentOfMinimum;
        return rows.Where(r => r.Present == true)
            .GroupBy(r => r.YogaCode)
            .Select(g => g.OrderBy(r => r.SourceRefCode).ThenBy(r => r.SourceVariantCode).First())
            .Select(r =>
            {
                var planets = YogaPlanetResolver.Resolve(r.YogaCode, r.YogaRule, houseLords, placements);
                var known = planets.Select(Pct).Where(v => v.HasValue).Select(v => v!.Value).ToList();
                return new YogaChipFact(r.YogaCode, NameOf(r), NatureOf(r.OutcomeNatureCode), BasedOf(r.YogaTypeCode),
                    planets.Select(p => p.ToString()).ToList(), known.Count == 0 ? null : known.Min(), r.YogaRule, r.InferenceText);
            })
            .OrderBy(y => NatureRank(y.Nature)).ThenByDescending(y => y.WeakestPercent ?? -1m).ThenBy(y => y.Name)
            .ToList();
    }

    /// <summary>Plain-word reading of a yoga's delivery: how strong is the weakest planet behind it.</summary>
    public static string Delivery(YogaChipFact y) => y.WeakestPercent switch
    {
        null => "no planet strength on file",
        >= 100m => "all its planets at full strength",
        >= 80m => "its weakest planet is moderate",
        _ => "rests on a weak planet — the promise is there, delivery is faint"
    };

    public static string NameOf(YogaEvaluationRow r) =>
        !string.IsNullOrWhiteSpace(r.VariantDisplayName) ? r.VariantDisplayName!.Trim() : Humanise(r.YogaCode);

    /// <summary>"YOGA_GAJAKESARI" → "Gajakesari".</summary>
    public static string Humanise(string code)
    {
        var words = (code.StartsWith("YOGA_", StringComparison.OrdinalIgnoreCase) ? code[5..] : code)
            .Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }

    public static string NatureOf(string? code) => code?.ToUpperInvariant() switch
    {
        "AUSPICIOUS" => "good", "INAUSPICIOUS" => "challenging", _ => "mixed"
    };

    private static int NatureRank(string nature) => nature switch { "good" => 0, "mixed" => 1, _ => 2 };

    private static string? BasedOf(string? type) => type?.ToUpperInvariant() switch
    {
        "SUN" => "Sun based", "MOON" => "Moon based", "LAGNA" => "Lagna based", "COMBINATION" => "Combination", _ => null
    };

    /// <summary>1 (Aries) to 12 (Pisces), or null when the name is not a sign.</summary>
    public static int? SignNumber(string? sign) =>
        Enum.TryParse<ZodiacName>(sign, true, out var z) ? (int)z + 1 : null;
}
