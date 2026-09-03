using System.Text.Json;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>
/// Reads a <c>tbl_Rule_VargaScheme.RuleParametersJson</c> blob and produces the varga sign for a
/// sidereal longitude — the language-independent form of an <see cref="IVargaSignRule"/> class.
/// <para>
/// A port (Python/TS/…) reimplements only the handful of interpreters here, then drives them from
/// the rule table: no per-varga code, just data. The JSON always carries a top-level
/// <c>"method"</c> key naming the interpreter family
/// (<c>LINEAR_VARGA</c> / <c>GRID_VARGA</c> / <c>BAND_VARGA</c>); the row's
/// <c>MethodCode</c> column stays the *classical scheme* name (ParasaraTraditional, UmaShambu, …).
/// </para>
/// <para>
/// <c>dotnet run --project src/Ikiastrro.Cli -- verify-rules</c> proves every scheme's JSON
/// round-trips to the exact same sign as its C# rule class over the whole circle.
/// </para>
/// </summary>
public interface IVargaMethodInterpreter
{
    /// <summary>The <c>"method"</c> tag this interpreter answers to.</summary>
    string Method { get; }

    /// <summary>Varga sign for <paramref name="siderealLongitude"/> under the parameters in
    /// <paramref name="p"/> (the parsed <c>RuleParametersJson</c> root object).</summary>
    ZodiacName SignFor(JsonElement p, double siderealLongitude);
}
