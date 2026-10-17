using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Engines.Lords;

/// <summary>
/// The three lords that shape one planet, read off its row: the lord of the sign it occupies (its dispositor), the lord of
/// the nakshatra it occupies (the Vimshottari star lord) and the lord of the nakshatra pada it occupies, taken as the
/// lord of the navamsa sign that pada falls in. The KP sub lord rides along because the chart already carries it.
///
/// The sign lord is the only one that changes from one varga to the next: the nakshatra, its lord, the pada and the pada
/// lord all come from the planet's true longitude, so they are the same in every chart a planet appears in.
/// A planet-level fact, not a varga-level one.
///
/// Sourcing: the sign lord and star lord are standard; the pada lord is the lord of the pada's navamsa sign (the nakshatra
/// is split into four padas of 3°20', and consecutive padas run through the navamsa signs from Aries). Whether PVR uses it
/// that way is not checked yet (see docs/research/domain/planet-lords-nature-plan.md); nothing here is a PVR rule.
/// </summary>
public sealed record PlanetLordTriad(
    string Planet, string SignLord, string? StarLord, string? PadaLord, ZodiacName? PadaSign, string? SubLord)
{
    /// <summary>One nakshatra pada, in degrees: 360 / 108.</summary>
    public const double PadaDegrees = 360.0 / 108.0;

    public static PlanetLordTriad For(ChartKeyDetail row)
    {
        var padaSign = PadaSignOf(row.NirayanaLongitudeDegrees);
        return new PlanetLordTriad(
            row.Planet, row.SignLordPlanet ?? "", row.NakshatraLordPlanet,
            HouseEngine.GetSignLord(padaSign), padaSign, row.NakshatraSubLordPlanet);
    }

    /// <summary>The navamsa sign of a longitude: pada number 0-107 counted from 0° Aries, modulo the twelve signs.</summary>
    public static ZodiacName PadaSignOf(double siderealLongitude)
    {
        var slot = (int)Math.Floor(AstroMath.Normalize(siderealLongitude) / PadaDegrees);
        return (ZodiacName)(Math.Min(slot, 107) % 12);
    }

    /// <summary>The distinct planets that stand behind this one (its lords other than itself), in the order sign, star, pada.</summary>
    public IReadOnlyList<string> Lords() =>
        new[] { SignLord, StarLord, PadaLord }
            .Where(l => !string.IsNullOrEmpty(l) && l != Planet).Select(l => l!).Distinct().ToList();
}
