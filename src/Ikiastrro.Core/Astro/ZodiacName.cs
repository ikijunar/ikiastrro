namespace Ikiastrro.Core.Astro;

/// <summary>
/// The 12 rasis (zodiac signs), Aries-first ordering matching sidereal (nirayana) longitude order.
/// Member names/spelling (incl. "Capricornus", the Latin form) deliberately match VedAstro.Library's
/// own ZodiacName enum, which this project's stored data (DB columns, JSON, published artifacts) was
/// built against — preserves compatibility with everything computed before the engine swap.
/// </summary>
public enum ZodiacName
{
    Aries = 0,
    Taurus = 1,
    Gemini = 2,
    Cancer = 3,
    Leo = 4,
    Virgo = 5,
    Libra = 6,
    Scorpio = 7,
    Sagittarius = 8,
    Capricornus = 9,
    Aquarius = 10,
    Pisces = 11
}
