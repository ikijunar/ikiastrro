namespace Ikiastrro.Core.Engines.Astronomy;

/// <summary>The 7 classical grahas plus the two lunar nodes. Names match VedAstro.Library's own enum.</summary>
public enum PlanetName
{
    Sun,
    Moon,
    Mars,
    Mercury,
    Jupiter,
    Venus,
    Saturn,
    Rahu,
    Ketu
}

/// <summary>Ordered planet lists, mirroring VedAstro.Library's PlanetName.All9Planets iteration order.</summary>
public static class PlanetNames
{
    public static readonly IReadOnlyList<PlanetName> All9 = new[]
    {
        PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury, PlanetName.Jupiter,
        PlanetName.Venus, PlanetName.Saturn, PlanetName.Rahu, PlanetName.Ketu
    };
}
