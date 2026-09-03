using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>D8 Ashtamsa - movable/fixed/dual signs count the l-th part from
/// Aries / Sagittarius / Leo. PyJHora ashtamsa_chart method 1.</summary>
public sealed class AshtamsaD8SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 8));
        var expr = (sign % 3) switch
        {
            0 => l,          // movable -> from Aries
            1 => l + 8,       // fixed   -> from Sagittarius
            _ => l + 4,       // dual    -> from Leo
        };
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
