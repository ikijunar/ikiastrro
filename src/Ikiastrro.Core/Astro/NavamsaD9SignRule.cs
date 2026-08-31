using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D9 Navamsa - wraps AstroMath.GetNavamsaSign.</summary>
public sealed class NavamsaD9SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetNavamsaSign(siderealLongitude);
}
