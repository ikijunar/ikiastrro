using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>
/// D2 Uma Shambu Hora - Jagannatha Hora's default D2 (tagged "D-2 (US)" in its
/// export). PyJHora hora_chart method 1 = __parivritti_even_reverse(dvf=2):
/// the 24 half-signs are laid out zodiacally, but each even (1-indexed) sign's
/// two halves are filled back-to-front.
///
///   odd 1-indexed sign (rasiSign r even, 0-indexed):  vargaSign = (2r + h) mod 12
///   even 1-indexed sign (r odd, 0-indexed):           vargaSign = (2r + 1 - h) mod 12
///
/// h = 0 for 0-15 deg, 1 for 15-30 deg. Verified against all 10 bodies of the
/// Ramakrishnan JHora export.
/// </summary>
public sealed class HoraD2UmaShambuSignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var r = (int)(lon / 30);
        var h = (lon % 30) < 15.0 ? 0 : 1;
        var idx = r % 2 == 0
            ? (2 * r + h) % 12
            : ((2 * r + 1 - h) % 12 + 12) % 12;
        return (ZodiacName)idx;
    }
}
