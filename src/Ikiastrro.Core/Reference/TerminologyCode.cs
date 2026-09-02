namespace Ikiastrro.Core.Reference;

/// <summary>
/// Every terminology <c>Code</c> that C#/Razor names as a compile-time constant. Resolve display
/// text through <see cref="TerminologyCatalog"/>, never by writing a bare Code string literal.
/// <para>
/// Only the Codes that engine/UI code actually references live here (Planets x9, Signs x12,
/// Karakas x8). The full ~236-row catalogue (houses, nakshatras + padas, vargas, avastha states,
/// dignity states, relationships, special points, ayanamsa) lives in
/// <c>dbo.tbl_Astro_Terminology</c> — add a const here only when code needs to name that Code.
/// </para>
/// </summary>
public static class TerminologyCode
{
    // Planets — PLANET_&lt;PlanetName enum member, upper-invariant&gt;
    public const string PlanetSun = "PLANET_SUN";
    public const string PlanetMoon = "PLANET_MOON";
    public const string PlanetMars = "PLANET_MARS";
    public const string PlanetMercury = "PLANET_MERCURY";
    public const string PlanetJupiter = "PLANET_JUPITER";
    public const string PlanetVenus = "PLANET_VENUS";
    public const string PlanetSaturn = "PLANET_SATURN";
    public const string PlanetRahu = "PLANET_RAHU";
    public const string PlanetKetu = "PLANET_KETU";

    // Signs — SIGN_&lt;ZodiacName enum member, upper-invariant&gt;; the enum's "Capricornus" maps to SIGN_CAPRICORN
    public const string SignAries = "SIGN_ARIES";
    public const string SignTaurus = "SIGN_TAURUS";
    public const string SignGemini = "SIGN_GEMINI";
    public const string SignCancer = "SIGN_CANCER";
    public const string SignLeo = "SIGN_LEO";
    public const string SignVirgo = "SIGN_VIRGO";
    public const string SignLibra = "SIGN_LIBRA";
    public const string SignScorpio = "SIGN_SCORPIO";
    public const string SignSagittarius = "SIGN_SAGITTARIUS";
    public const string SignCapricorn = "SIGN_CAPRICORN";
    public const string SignAquarius = "SIGN_AQUARIUS";
    public const string SignPisces = "SIGN_PISCES";

    // Jaimini chara karakas — KARAKA_&lt;CharaKaraka enum member, upper-invariant&gt;
    public const string KarakaAtmakaraka = "KARAKA_AK";
    public const string KarakaAmatyakaraka = "KARAKA_AMK";
    public const string KarakaBhratrikaraka = "KARAKA_BK";
    public const string KarakaMatrikaraka = "KARAKA_MK";
    public const string KarakaPitrikaraka = "KARAKA_PIK";
    public const string KarakaPutrakaraka = "KARAKA_PK";
    public const string KarakaGnatikaraka = "KARAKA_GK";
    public const string KarakaDarakaraka = "KARAKA_DK";
}
