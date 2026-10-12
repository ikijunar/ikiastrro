using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Relationships;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Engines.Houses;

/// <summary>Verdict for a house's benefic/malefic synthesis. Mixed means the counted factors
/// (lord + occupants + aspectors, each equally weighted) tie rather than genuinely cancel —
/// the Rationale spells out the count so a caller can re-weigh if needed. Dignity/combustion of
/// the lord is reported separately (LordDignityStatus) as a modifier, never folded into the
/// count, per B.V. Raman's own framing (a well-placed lord in a weak sign still signals through;
/// dignity changes intensity, not direction).</summary>
public enum HouseBeneficMaleficVerdict { Benefic, Malefic, Mixed, Neutral }

public sealed record HouseBeneficMaleficResult(
    int HouseNumber,
    ZodiacName Sign,
    string LordPlanet,
    FunctionalNature LordFunctionalNature,
    IReadOnlyList<string> BeneficOccupants,
    IReadOnlyList<string> MaleficOccupants,
    IReadOnlyList<string> BeneficAspectors,
    IReadOnlyList<string> MaleficAspectors,
    string? LordDignityStatus,
    HouseBeneficMaleficVerdict Verdict,
    string Rationale);

/// <summary>
/// Sign/house-level benefic-malefic synthesis: B.V. Raman, "How to Judge a Horoscope" Vol. 1,
/// p.14-15 — the same citation LagnaFunctionalNature already carries for planet-level functional
/// nature (kendra/trikona lordship, kendradhipati dosha, maraka/dusthana). Raman's own worked
/// method for judging a house combines (1) the functional nature of its lord [dominant input],
/// (2) its occupants, and (3) aspects upon it, with (4) the lord's dignity/strength as a modifier
/// on intensity rather than a fourth vote. A sign has no benefic/malefic nature of its own outside
/// a chart — this is always evaluated per-Lagna, per-house (docs/research/domain/sign-benefic-malefic.md).
/// Deliberately does not produce a single blended numeric score: DignityScore/RelationshipScore's
/// own precedent is to keep raw ordinal factors separate and let the interpretation layer combine
/// them, not invent a weighted formula here. Rahu/Ketu occupants/aspectors are classed by the
/// natural-malefic convention already implicit elsewhere in this codebase (LagnaFunctionalNature's
/// NaturalBenefics omits them; they own no sign so have no functional nature of their own).
/// </summary>
public static class HouseBeneficMaleficCalculator
{
    public static IReadOnlyList<HouseBeneficMaleficResult> ComputeAll(
        ZodiacName ascendantSign, IReadOnlyList<ChartKeyDetail> grahas)
    {
        var occupants = grahas.Where(k => k.PointKind == "Graha" && k.Planet != "Ascendant").ToList();
        var moonWaxing = MoonPhase.Of(grahas);

        var results = new List<HouseBeneficMaleficResult>(12);
        for (var houseNumber = 1; houseNumber <= 12; houseNumber++)
        {
            var sign = HouseEngine.GetHouseSign(ascendantSign, houseNumber);
            var lordName = HouseEngine.GetSignLord(sign);
            var lordPlanet = Enum.Parse<PlanetName>(lordName);
            var lordFunctional = LagnaFunctionalNature.For(ascendantSign, lordPlanet, moonWaxing);
            var lordDetail = occupants.FirstOrDefault(k => k.Planet == lordName);

            var houseOccupants = occupants.Where(k => k.HouseNumberFromLagna == houseNumber).ToList();
            var beneficOccupants = houseOccupants.Where(k => IsBenefic(ascendantSign, k.Planet, moonWaxing)).Select(k => k.Planet).ToList();
            var maleficOccupants = houseOccupants.Where(k => !IsBenefic(ascendantSign, k.Planet, moonWaxing)).Select(k => k.Planet).ToList();

            var aspectors = occupants
                .Where(k => RelationshipEngine.AspectsSign(k.Planet, Enum.Parse<ZodiacName>(k.Sign), sign))
                .ToList();
            var beneficAspectors = aspectors.Where(k => IsBenefic(ascendantSign, k.Planet, moonWaxing)).Select(k => k.Planet).ToList();
            var maleficAspectors = aspectors.Where(k => !IsBenefic(ascendantSign, k.Planet, moonWaxing)).Select(k => k.Planet).ToList();

            var beneficCount = (lordFunctional.Nature is FunctionalNature.Benefic or FunctionalNature.Yogakaraka ? 1 : 0)
                + beneficOccupants.Count + beneficAspectors.Count;
            var maleficCount = (lordFunctional.Nature == FunctionalNature.Malefic ? 1 : 0)
                + maleficOccupants.Count + maleficAspectors.Count;

            var verdict = (beneficCount, maleficCount) switch
            {
                (0, 0) => HouseBeneficMaleficVerdict.Neutral,
                var (b, m) when b > m => HouseBeneficMaleficVerdict.Benefic,
                var (b, m) when m > b => HouseBeneficMaleficVerdict.Malefic,
                _ => HouseBeneficMaleficVerdict.Mixed
            };

            var rationale = $"Lord {lordName} is {lordFunctional.Nature} ({lordFunctional.Rationale}); " +
                $"occupants benefic=[{string.Join(",", beneficOccupants)}] malefic=[{string.Join(",", maleficOccupants)}]; " +
                $"aspectors benefic=[{string.Join(",", beneficAspectors)}] malefic=[{string.Join(",", maleficAspectors)}]; " +
                $"count {beneficCount} benefic vs {maleficCount} malefic — {verdict}";

            results.Add(new HouseBeneficMaleficResult(
                houseNumber, sign, lordName, lordFunctional.Nature,
                beneficOccupants, maleficOccupants, beneficAspectors, maleficAspectors,
                lordDetail?.DignityStatus, verdict, rationale));
        }

        return results;
    }

    /// <summary>Benefic/malefic by the occupant/aspecting planet's OWN functional nature from this
    /// Lagna (not the house's own lord) — Rahu/Ketu own no sign so fall back to the natural-malefic
    /// convention already implicit in LagnaFunctionalNature.NaturalBenefics.</summary>
    private static bool IsBenefic(ZodiacName ascendantSign, string planetName, bool? moonWaxing)
    {
        if (!Enum.TryParse<PlanetName>(planetName, out var planet) || planet is PlanetName.Rahu or PlanetName.Ketu)
            return false;

        var nature = LagnaFunctionalNature.For(ascendantSign, planet, moonWaxing).Nature;
        return nature is FunctionalNature.Benefic or FunctionalNature.Yogakaraka;
    }
}
