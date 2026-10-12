using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Relationships;

namespace Ikiastrro.Core.LifeMatters;

/// <summary>How a planet reaches the target sign directly.</summary>
[Flags]
public enum InfluenceLink
{
    None = 0,
    Occupies = 1,
    GrahaDrishti = 2,
    RasiDrishti = 4,
    Argala = 8,
    Virodhargala = 16,
}

/// <summary>PVR's relative-house classes counted from the target (step 5): quadrants sustain the
/// matter, trines let it prosper, upachayas let it grow, dusthanas bring obstacles. A house can
/// be in two classes (1 = quadrant + trine, 6 = upachaya + dusthana, 10 = quadrant + upachaya).</summary>
[Flags]
public enum TargetPosition
{
    None = 0,
    Quadrant = 1,
    Trine = 2,
    Upachaya = 4,
    Dusthana = 8,
}

/// <summary>What PVR step 5 says a planet does for the target: its position from the target, and
/// whether it is the target's bādhaka. Mixed = both a helping and an obstructing reason.</summary>
public enum InfluenceLean { Supports, Obstructs, Mixed, Neutral }

/// <summary>One planet read against the target sign.</summary>
/// <param name="Functional">Functional nature for the chart's Lagna; null for Rahu/Ketu (they own no sign).</param>
/// <param name="IsNaturalMalefic">Natural nature, with waning Moon / afflicted Mercury as malefic
/// (<see cref="ArgalaCalculator.IsNaturalMalefic"/>).</param>
public sealed record PlanetInfluence(
    PlanetName Planet,
    ZodiacName Sign,
    int HouseFromTarget,
    TargetPosition Position,
    InfluenceLink Links,
    FunctionalNature? Functional,
    bool IsNaturalMalefic,
    bool IsLord,
    bool IsBaadhaka,
    bool InBaadhakaSthaana,
    bool IsKaraka,
    InfluenceLean Lean)
{
    /// <summary>Occupies, aspects (graha or rāśi) or intervenes by Argala on the target.</summary>
    public bool Touches => Links != InfluenceLink.None;
}

/// <summary>Every planet's influence on one target sign (a house or an Āruḍha pada), with the
/// target's lord and bādhaka. <see cref="Planets"/> lists all nine, lord and kārakas first, then
/// the planets that touch the target, then the rest.</summary>
public sealed record TargetInfluenceReading(
    ZodiacName Target,
    PlanetName Lord,
    int LordHouseFromTarget,
    TargetPosition LordPosition,
    BaadhakaResult Baadhaka,
    IReadOnlyList<PlanetInfluence> Planets)
{
    public IEnumerable<PlanetInfluence> Supporting => Planets.Where(p => p.Lean == InfluenceLean.Supports);
    public IEnumerable<PlanetInfluence> Obstructing => Planets.Where(p => p.Lean == InfluenceLean.Obstructs);
}

/// <summary>
/// PVR step 5 (SRC_PVR_INTEGRATED Ch. 13; docs/cli/reading/PVR_read_horoscope.md): the influences
/// on the house or Āruḍha pada a life matter is read from — graha and rāśi dṛṣṭi onto it, Argala
/// on it, every planet's position counted from it as the new reference, and its bādhaka (§13.3).
/// Functional nature (§13.2) is reported for each planet but does not decide the lean: PVR
/// applies it when judging, not when listing. Pure; the caller supplies the chart's placements
/// and the Argala facts it already reads.
/// </summary>
public static class TargetInfluences
{
    private static readonly int[] Quadrants = [1, 4, 7, 10];
    private static readonly int[] Trines = [1, 5, 9];
    private static readonly int[] Upachayas = [3, 6, 10, 11];
    private static readonly int[] Dusthanas = [6, 8, 12];

    public static TargetPosition PositionOf(int houseFromTarget) =>
        (Quadrants.Contains(houseFromTarget) ? TargetPosition.Quadrant : 0)
        | (Trines.Contains(houseFromTarget) ? TargetPosition.Trine : 0)
        | (Upachayas.Contains(houseFromTarget) ? TargetPosition.Upachaya : 0)
        | (Dusthanas.Contains(houseFromTarget) ? TargetPosition.Dusthana : 0);

    /// <param name="lagnaSign">The chart's own Lagna, for functional nature.</param>
    /// <param name="placements">The nine grahas' signs in this chart.</param>
    /// <param name="target">The house or pada sign being read.</param>
    /// <param name="karakas">The matter's kāraka planets in this chart.</param>
    /// <param name="argalaPlanets">Planets whose Argala on the target holds or is contested.</param>
    /// <param name="virodhargalaPlanets">Planets obstructing Argala on the target.</param>
    /// <param name="moonWaxing">The birth Moon's phase (<see cref="MoonPhase"/>); read from the signs in
    /// <paramref name="placements"/> when null.</param>
    public static TargetInfluenceReading Read(
        ZodiacName lagnaSign,
        IReadOnlyDictionary<PlanetName, ZodiacName> placements,
        ZodiacName target,
        IEnumerable<PlanetName>? karakas = null,
        IEnumerable<PlanetName>? argalaPlanets = null,
        IEnumerable<PlanetName>? virodhargalaPlanets = null,
        bool? moonWaxing = null)
    {
        var moonPhase = moonWaxing ?? MoonPhase.Of(placements);
        var karakaSet = (karakas ?? []).ToHashSet();
        var argalaSet = (argalaPlanets ?? []).ToHashSet();
        var virodhaSet = (virodhargalaPlanets ?? []).ToHashSet();

        var lord = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(target));
        var baadhaka = BaadhakaCalculator.For(target);
        var baadhakaLord = Enum.Parse<PlanetName>(baadhaka.Baadhaka);
        var occupancy = placements.GroupBy(p => p.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PlanetName>)g.Select(p => p.Key).ToList());

        var rows = placements.Select(p =>
        {
            var (planet, sign) = (p.Key, p.Value);
            var house = AstroMath.CountFromSignToSign(target, sign);
            var position = PositionOf(house);
            var links = (house == 1 ? InfluenceLink.Occupies : 0)
                | (house != 1 && RelationshipEngine.AspectsSign(planet.ToString(), sign, target) ? InfluenceLink.GrahaDrishti : 0)
                | (RasiDrishtiCalculator.Aspects(sign, target) ? InfluenceLink.RasiDrishti : 0)
                | (argalaSet.Contains(planet) ? InfluenceLink.Argala : 0)
                | (virodhaSet.Contains(planet) ? InfluenceLink.Virodhargala : 0);
            var isBaadhaka = planet == baadhakaLord;
            var inSthaana = sign == baadhaka.SthaanaSign;

            var helps = (position & (TargetPosition.Quadrant | TargetPosition.Trine | TargetPosition.Upachaya)) != 0;
            var hinders = (position & TargetPosition.Dusthana) != 0 || isBaadhaka || inSthaana;
            var lean = helps && hinders ? InfluenceLean.Mixed
                : helps ? InfluenceLean.Supports
                : hinders ? InfluenceLean.Obstructs
                : InfluenceLean.Neutral;

            return new PlanetInfluence(planet, sign, house, position, links,
                planet is PlanetName.Rahu or PlanetName.Ketu ? null : LagnaFunctionalNature.For(lagnaSign, planet, moonPhase).Nature,
                ArgalaCalculator.IsNaturalMalefic(planet, occupancy),
                planet == lord, isBaadhaka, inSthaana, karakaSet.Contains(planet), lean);
        })
        .OrderByDescending(r => r.IsLord)
        .ThenByDescending(r => r.IsKaraka)
        .ThenByDescending(r => r.Touches)
        .ThenBy(r => r.Planet)
        .ToList();

        var lordHouse = AstroMath.CountFromSignToSign(target, placements[lord]);
        return new TargetInfluenceReading(target, lord, lordHouse, PositionOf(lordHouse), baadhaka, rows);
    }
}
