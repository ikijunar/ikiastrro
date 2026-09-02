using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Astro;

/// <summary>D24 Siddhamsa (Chaturvimsamsa) - odd signs count the l-th part from
/// Leo, even signs from Cancer. PyJHora chaturvimsamsa_chart method 1.</summary>
public sealed class SiddhamsaD24SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 24));
        var expr = sign % 2 == 0 ? 4 + l : 3 + l;   // 4 = Leo, 3 = Cancer
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
