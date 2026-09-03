namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>
/// Builds the <see cref="IVargaMethodInterpreter"/> for a <c>RuleParametersJson</c> <c>"method"</c>
/// tag. The interpreters are stateless, so one shared instance each. Pure — no I/O.
/// This switch is the contract with the seeded rule data: every <c>"method"</c> value
/// <c>seed-rule-params</c> writes must have a case here (asserted by <c>verify-rules</c>).
/// </summary>
public static class VargaMethodInterpreterFactory
{
    private static readonly LinearVargaInterpreter Linear = new();
    private static readonly GridVargaInterpreter Grid = new();
    private static readonly BandVargaInterpreter Band = new();

    public static IVargaMethodInterpreter For(string method) => method switch
    {
        "LINEAR_VARGA" => Linear,
        "GRID_VARGA" => Grid,
        "BAND_VARGA" => Band,
        _ => throw new InvalidOperationException(
            $"No IVargaMethodInterpreter for method '{method}'. Add a case in " +
            "VargaMethodInterpreterFactory or fix tbl_Rule_VargaScheme.RuleParametersJson."),
    };
}
