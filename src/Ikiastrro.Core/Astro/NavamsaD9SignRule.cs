using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Astro;

/// <summary>D9 Navamsa - wraps AstroMath.GetNavamsaSign.</summary>
public sealed class NavamsaD9SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetNavamsaSign(siderealLongitude);
}
