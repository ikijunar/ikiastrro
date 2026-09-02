using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Astro;

/// <summary>D2 classical two-sign Hora (odd 1st-half -> Leo, 2nd-half -> Cancer;
/// even reversed). Wraps AstroMath.GetHoraSign so the maths stays in one place.</summary>
public sealed class HoraD2ClassicSignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetHoraSign(siderealLongitude);
}
