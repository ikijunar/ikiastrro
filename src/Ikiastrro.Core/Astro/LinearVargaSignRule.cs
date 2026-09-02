using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Astro;

/// <summary>
/// The "l-th part, counted <c>stride</c> signs forward from the rasi sign" family:
/// D3 (factor 3, stride 4 -> 1st/5th/9th), D4 (4, 3), D12 (12, 1), D60 (60, 1).
/// <c>l = floor(degreesInRasiSign / (30/factor))</c>; result = (rasiSign + l*stride) mod 12.
/// PyJHora: _drekkana_chart_parasara / _chaturthamsa_parasara / dwadasamsa_chart m1 /
/// shashtyamsa_chart m1.
/// </summary>
public sealed class LinearVargaSignRule : IVargaSignRule
{
    private readonly int _factor;
    private readonly int _stride;

    public LinearVargaSignRule(int factor, int stride)
    {
        _factor = factor;
        _stride = stride;
    }

    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var rasiSign = (int)(lon / 30);
        var degInSign = lon % 30;
        var l = (int)(degInSign / (30.0 / _factor));
        var idx = ((rasiSign + l * _stride) % 12 + 12) % 12;
        return (ZodiacName)idx;
    }
}
