using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D6 Shashtamsa - wraps AstroMath.GetShashtamsaSign (odd -> Aries..Virgo,
/// even -> Libra..Pisces).</summary>
public sealed class ShashtamsaD6SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetShashtamsaSign(siderealLongitude);
}
