using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>D27 Nakshatramsa (Bhamsa) - fire/earth/air/water signs count the
/// l-th part from Aries / Cancer / Libra / Capricorn. PyJHora
/// nakshatramsa_chart method 1.</summary>
public sealed class NakshatramsaD27SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 27));
        var expr = (sign % 4) switch
        {
            0 => l,          // fire  -> Aries
            1 => l + 3,       // earth -> Cancer
            2 => l + 6,       // air   -> Libra
            _ => l + 9,       // water -> Capricorn
        };
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
