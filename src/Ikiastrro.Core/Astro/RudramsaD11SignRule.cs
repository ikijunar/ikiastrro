using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D11 Rudramsa - wraps AstroMath.GetRudramsaSign (Sanjay Rath method).</summary>
public sealed class RudramsaD11SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetRudramsaSign(siderealLongitude);
}
