namespace Ikiastrro.Core.Astro;

/// <summary>
/// Maps the Core enums to their reference-table primary keys.
///   tbl_Planets.Id       = (int)PlanetName + 1   (Sun=0 -> 1 ... Ketu=8 -> 9)
///   tbl_SignAttributes.Id = (int)ZodiacName + 1  (Aries=0 -> 1 ... Pisces=11 -> 12)
/// The DB backfill uses name joins, not this offset — this is the write-path source
/// for freshly computed rows.
/// </summary>
public static class AstroIds
{
    public const int PlanetIdOffset = 1;
    public const int SignIdOffset = 1;

    public static int PlanetId(PlanetName planet) => (int)planet + PlanetIdOffset;

    public static int SignId(ZodiacName sign) => (int)sign + SignIdOffset;

    /// <summary>Null for "Ascendant" (not a graha); otherwise the tbl_Planets.Id for the name.</summary>
    public static int? PlanetIdOrNull(string? planetName) =>
        string.IsNullOrEmpty(planetName) || planetName == "Ascendant"
            ? null
            : Enum.TryParse<PlanetName>(planetName, out var p) ? PlanetId(p) : null;
}
