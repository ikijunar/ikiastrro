using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Astro;

/// <summary>D11 Rudramsa - wraps AstroMath.GetRudramsaSign (Sanjay Rath method).</summary>
public sealed class RudramsaD11SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetRudramsaSign(siderealLongitude);
}
