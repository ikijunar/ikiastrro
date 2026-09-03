using System.Text.Json;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>
/// <c>LINEAR_VARGA</c> — the closed-form "l-th equal part, counted <c>stride</c> signs forward from
/// the rasi sign" family; the data form of <see cref="LinearVargaSignRule"/>.
/// <para>JSON: <c>{ "method": "LINEAR_VARGA", "factor": 3, "stride": 4 }</c></para>
/// <para>
/// <c>l = floor(degreesInRasiSign / (30/factor))</c>; sign = <c>(rasiSign + l*stride) mod 12</c>.
/// Used by D3 (3,4), D4 (4,3), D12 (12,1), D60 (60,1).
/// </para>
/// </summary>
public sealed class LinearVargaInterpreter : IVargaMethodInterpreter
{
    public string Method => "LINEAR_VARGA";

    public ZodiacName SignFor(JsonElement p, double siderealLongitude)
    {
        var factor = p.GetProperty("factor").GetInt32();
        var stride = p.GetProperty("stride").GetInt32();

        var lon = AstroMath.Normalize(siderealLongitude);
        var rasiSign = (int)(lon / 30);
        var degInSign = lon % 30;
        var l = (int)(degInSign / (30.0 / factor));
        var idx = ((rasiSign + l * stride) % 12 + 12) % 12;
        return (ZodiacName)idx;
    }
}
