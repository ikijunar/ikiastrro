using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>D30 Trimsamsa - the classical unequal five-part scheme. The sign is
/// chosen by which degree band of the rasi sign the planet falls in (odd vs
/// even sign), not by an equal l-part. PyJHora trimsamsa_chart method 1.</summary>
public sealed class TrimsamsaD30SignRule : IVargaSignRule
{
    // (upperExclusive, signIndex) - the last band is inclusive of 30.0 via its Max.
    private static readonly (double Max, int Sign)[] Odd =
        { (5, 0), (10, 10), (18, 8), (25, 2), (30.0001, 6) };
    private static readonly (double Max, int Sign)[] Even =
        { (5, 1), (12, 5), (20, 11), (25, 9), (30.0001, 7) };

    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var deg = lon % 30;
        var table = sign % 2 == 0 ? Odd : Even;
        foreach (var (max, s) in table)
            if (deg < max) return (ZodiacName)s;
        return (ZodiacName)table[^1].Sign;   // unreachable; deg is always < 30
    }
}
