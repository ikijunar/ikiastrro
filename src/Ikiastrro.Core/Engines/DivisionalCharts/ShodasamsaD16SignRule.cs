using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>D16 Shodasamsa (Kalamsa) - movable/fixed/dual signs count the l-th
/// part from Aries / Leo / Sagittarius. PyJHora shodasamsa_chart method 1.</summary>
public sealed class ShodasamsaD16SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 16));
        var expr = (sign % 3) switch { 0 => l, 1 => l + 4, _ => l + 8 };
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
