using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D5 Panchamsa - five equal 6 deg parts. Odd signs map the l-th part to
/// Aries/Aquarius/Sagittarius/Gemini/Libra; even signs to
/// Taurus/Virgo/Pisces/Capricorn/Scorpio. PyJHora panchamsa_chart method 1
/// (panchamsa_odd_signs / panchamsa_even_signs).</summary>
public sealed class PanchamsaD5SignRule : IVargaSignRule
{
    private static readonly int[] Odd  = { 0, 10, 8, 2, 6 };
    private static readonly int[] Even = { 1, 5, 11, 9, 7 };

    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / 6.0);
        if (l > 4) l = 4;   // guard the 30.0 deg boundary
        return (ZodiacName)(sign % 2 == 0 ? Odd[l] : Even[l]);
    }
}
