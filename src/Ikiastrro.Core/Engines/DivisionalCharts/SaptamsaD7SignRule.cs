using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>D7 Saptamsa - odd signs count the l-th part from the sign itself,
/// even signs from the 7th from it. PyJHora saptamsa_chart method 1
/// (PARASARA_EVEN_START_7TH_GO_FORWARD).</summary>
public sealed class SaptamsaD7SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 7));
        var expr = sign % 2 == 0 ? sign + l : sign + l + 6;
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
