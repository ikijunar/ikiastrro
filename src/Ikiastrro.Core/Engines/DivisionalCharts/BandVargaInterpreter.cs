using System.Text.Json;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>
/// <c>BAND_VARGA</c> — the *unequal* degree-band lookup, for vargas whose parts are not equal arcs.
/// Only D30 Trimsamsa needs it (<see cref="TrimsamsaD30SignRule"/>): odd signs break at
/// 5/10/18/25/30°, even signs at 5/12/20/25/30°.
/// <para>
/// JSON: <c>{ "method": "BAND_VARGA", "edges": [e1..ek], "map": [[s0..s11], … k rows] }</c>.
/// Band <c>j</c> is <c>[edges[j-1], edges[j])</c> with <c>edges[-1]</c> implicitly 0, and
/// <c>map[j][rasiSign]</c> is the 0-based sign index. The two parities' break points are carried as
/// one shared, merged edge list (their union) so a single table covers all 12 signs; a band that
/// falls inside one parity's larger band simply repeats that parity's sign.
/// </para>
/// </summary>
public sealed class BandVargaInterpreter : IVargaMethodInterpreter
{
    public string Method => "BAND_VARGA";

    public ZodiacName SignFor(JsonElement p, double siderealLongitude)
    {
        var edges = p.GetProperty("edges");
        var map = p.GetProperty("map");

        var lon = AstroMath.Normalize(siderealLongitude);
        var rasi = (int)(lon / 30);
        if (rasi > 11) rasi = 11;                       // guard a 360.0 rounding to index 12
        var d = lon % 30;

        var last = edges.GetArrayLength() - 1;          // the final edge (30°) is never crossed
        var j = 0;
        while (j < last && d >= edges[j].GetDouble()) j++;
        return (ZodiacName)map[j][rasi].GetInt32();
    }
}
