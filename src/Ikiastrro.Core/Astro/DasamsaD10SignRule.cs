using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D10 Dasamsa - wraps AstroMath.GetDasamsaSign (odd from self, even from 9th).</summary>
public sealed class DasamsaD10SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetDasamsaSign(siderealLongitude);
}
