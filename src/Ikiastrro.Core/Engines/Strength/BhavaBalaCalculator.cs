using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Strength;

/// <summary>
/// Foundational Bhava Bala using the PVR profile and Raman's referenced three-part model:
/// Bhavadhipati (house lord's Shadbala), Bhava Dig (sign-nature direction), and Bhava Drik
/// (benefic/malefic aspects to the house midpoint). Values are retained in virupas for audit.
/// </summary>
public static class BhavaBalaCalculator
{
    private static readonly IReadOnlyDictionary<string, string> SignNature = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Aries"] = "CHATUSPADHA", ["Taurus"] = "CHATUSPADHA", ["Gemini"] = "NARA",
        ["Cancer"] = "JALACHARA", ["Leo"] = "CHATUSPADHA", ["Virgo"] = "NARA",
        ["Libra"] = "NARA", ["Scorpio"] = "KEETA", ["Sagittarius"] = "CHATUSPADHA",
        ["Capricornus"] = "CHATUSPADHA", ["Aquarius"] = "NARA", ["Pisces"] = "JALACHARA"
    };

    private static readonly IReadOnlyDictionary<string, double[]> DigByNature = new Dictionary<string, double[]>
    {
        ["NARA"] = new[] { 60d, 50, 40, 30, 20, 10, 0, 10, 20, 30, 40, 50 },
        ["JALACHARA"] = new[] { 30d, 40, 50, 60, 50, 40, 30, 20, 10, 0, 10, 20 },
        ["CHATUSPADHA"] = new[] { 30d, 20, 10, 0, 10, 20, 30, 40, 50, 60, 50, 40 },
        ["KEETA"] = new[] { 0d, 10, 20, 30, 40, 50, 60, 50, 40, 30, 20, 10 }
    };

    public static IReadOnlyList<BhavaBalaResult> Calculate(
        ChartAnalysisInput d1,
        IReadOnlyList<PlanetaryStrengthResult> planetaryStrengths)
    {
        if (!d1.ChartType.Equals("D1", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Bhava Bala requires the D1 chart.", nameof(d1));

        var strengthByPlanet = planetaryStrengths.ToDictionary(x => x.Planet, StringComparer.OrdinalIgnoreCase);
        var results = new List<BhavaBalaResult>(12);
        for (var house = 1; house <= 12; house++)
        {
            var sign = HouseEngine.GetHouseSign(d1.AscendantSign, house);
            var lord = HouseEngine.GetSignLord(sign);
            var components = new List<BhavaBalaComponentResult>();

            var lordVirupas = strengthByPlanet.GetValueOrDefault(lord)?.ShadbalaVirupas ?? 0;
            components.Add(Row(house, "BHAVADHIPATI_BALA", lordVirupas, "HOUSE_LORD_SHADBALA",
                $"House {house} {sign}: lord {lord} Shadbala {lordVirupas:0.###} virupas."));

            var nature = SignNature[sign.ToString()];
            var dig = DigByNature[nature][house - 1];
            components.Add(Row(house, "BHAVA_DIG_BALA", dig, "HOUSE_SIGN_NATURE_DIRECTION",
                $"House {house} {sign}: {nature.ToLowerInvariant()} sign directional value."));

            var drik = HouseDrik(d1, sign, house);
            components.Add(Row(house, "BHAVA_DRIK_BALA", drik, "HOUSE_MIDPOINT_ASPECT",
                "Benefic minus malefic Sputa Drishti on the 15° whole-sign house midpoint, divided by four."));

            var total = components.Sum(x => x.ValueVirupas);
            results.Add(new BhavaBalaResult(house, components, Round(total), Round(total / 60.0)));
        }
        return results;
    }

    private static double HouseDrik(ChartAnalysisInput chart, ZodiacName houseSign, int house)
    {
        var midpoint = ((int)houseSign * 30) + 15;
        var total = 0d;
        foreach (var p in chart.Planets)
        {
            if (!Enum.TryParse<PlanetName>(p.Planet, out var source) || source is PlanetName.Rahu or PlanetName.Ketu)
                continue;
            var longitude = p.NirayanaLongitudeDegrees ?? ((int)Enum.Parse<ZodiacName>(p.Sign) * 30);
            var angle = AngularDistance(longitude, midpoint);
            var strength = AspectStrength(angle, source);
            total += IsNaturalBenefic(source) ? strength : -strength;
        }
        return Math.Min(total / 4.0, 20.0);
    }

    private static BhavaBalaComponentResult Row(int house, string code, double value, string method, string narrative) =>
        new(house, code, Round(value), method, narrative);

    private static bool IsNaturalBenefic(PlanetName p) => p is PlanetName.Moon or PlanetName.Mercury or PlanetName.Jupiter or PlanetName.Venus;
    private static double Round(double value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);
    private static double AngularDistance(double a, double b)
    {
        var d = Math.Abs(((a - b) % 360 + 360) % 360);
        return d > 180 ? 360 - d : d;
    }

    private static double AspectStrength(double angle, PlanetName source)
    {
        var special = source switch
        {
            PlanetName.Mars => new[] { 90d, 210d },
            PlanetName.Jupiter => new[] { 120d, 240d },
            PlanetName.Saturn => new[] { 60d, 270d },
            _ => Array.Empty<double>()
        };
        if (special.Any(x => AngularDistance(angle, x) <= 8) || Math.Abs(angle - 180) <= 8) return 60;
        if (angle < 30 || angle > 150) return 0;
        return angle <= 90 ? angle - 30 : 150 - angle;
    }
}
