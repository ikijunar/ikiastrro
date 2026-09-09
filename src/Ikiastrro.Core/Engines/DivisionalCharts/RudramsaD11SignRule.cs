using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>D11 Rudramsa — wraps the PVR/BPHS traditional Rudramsa rule.</summary>
public sealed class RudramsaD11SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetRudramsaSign(siderealLongitude);
}
