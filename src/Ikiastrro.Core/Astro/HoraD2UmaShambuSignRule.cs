using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>
/// D2 Uma Shambu Hora - Jagannatha Hora's default D2 (tagged "D-2 (US)" in its
/// export). Unlike the classical two-sign Hora it distributes across all 12
/// signs: a parivritti-cyclic D2 (each 15 deg half is the next sign forward).
/// idx = (rasiSign*2 + half) mod 12, half in {0,1}. Ported from PyJHora
/// hora_chart (d2 default); confirmed "closest" against the Ramakrishnan export
/// grid - see tbl_Rule_VargaScheme.MethodSource.
/// </summary>
public sealed class HoraD2UmaShambuSignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var half = (lon % 30) < 15.0 ? 0 : 1;
        return (ZodiacName)(((sign * 2 + half) % 12 + 12) % 12);
    }
}
