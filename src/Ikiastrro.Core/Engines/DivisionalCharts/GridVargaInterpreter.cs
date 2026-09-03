using System.Text.Json;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>
/// <c>GRID_VARGA</c> — the general "equal part → sign" lookup. Any varga that splits each rasi sign
/// into <c>parts</c> equal arcs of <c>30/parts</c>° and reads the resulting sign off a table is
/// expressible here, whatever its odd/even, movable-fixed-dual or elemental branching: the branch is
/// baked into the map.
/// <para>
/// JSON: <c>{ "method": "GRID_VARGA", "parts": 9, "map": [[s0..s11], … parts rows] }</c> where
/// <c>map[i][rasiSign]</c> is the 0-based sign index for the i-th part of <c>rasiSign</c>.
/// </para>
/// <para>Used by D2, D2-US, D5, D6, D7, D8, D9, D10, D11, D16, D20, D24, D27, D40, D45.</para>
/// </summary>
public sealed class GridVargaInterpreter : IVargaMethodInterpreter
{
    public string Method => "GRID_VARGA";

    public ZodiacName SignFor(JsonElement p, double siderealLongitude)
    {
        var parts = p.GetProperty("parts").GetInt32();
        var map = p.GetProperty("map");

        var lon = AstroMath.Normalize(siderealLongitude);
        var rasi = (int)(lon / 30);
        if (rasi > 11) rasi = 11;                       // guard a 360.0 rounding to index 12
        var part = (int)((lon % 30) / (30.0 / parts));
        if (part >= parts) part = parts - 1;            // guard the 30.0° boundary
        return (ZodiacName)map[part][rasi].GetInt32();
    }
}
