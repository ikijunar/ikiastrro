using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Astro;

/// <summary>D40 Khavedamsa (Chatvarimsamsa) - odd signs count the l-th part from
/// Aries, even signs from Libra. PyJHora khavedamsa_chart method 1.</summary>
public sealed class KhavedamsaD40SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 40));
        var expr = sign % 2 == 0 ? l : l + 6;
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
