using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>D20 Vimsamsa - movable/dual/fixed signs count the l-th part from
/// Aries / Leo / Sagittarius (dual and fixed offsets are swapped relative to
/// D16/D45). PyJHora vimsamsa_chart method 1.</summary>
public sealed class VimsamsaD20SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 20));
        var expr = (sign % 3) switch
        {
            0 => l,          // movable -> Aries
            2 => l + 4,       // dual    -> Leo
            _ => l + 8,       // fixed   -> Sagittarius
        };
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
