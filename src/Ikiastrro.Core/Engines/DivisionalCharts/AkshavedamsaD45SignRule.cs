using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>D45 Akshavedamsa - movable/fixed/dual signs count the l-th part from
/// Aries / Leo / Sagittarius (same shape as D16). PyJHora akshavedamsa_chart
/// method 1.</summary>
public sealed class AkshavedamsaD45SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 45));
        var expr = (sign % 3) switch { 0 => l, 1 => l + 4, _ => l + 8 };
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
