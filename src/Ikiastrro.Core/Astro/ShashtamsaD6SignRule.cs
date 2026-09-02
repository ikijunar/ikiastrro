using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Astro;

/// <summary>D6 Shashtamsa - wraps AstroMath.GetShashtamsaSign (odd -> Aries..Virgo,
/// even -> Libra..Pisces).</summary>
public sealed class ShashtamsaD6SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetShashtamsaSign(siderealLongitude);
}
