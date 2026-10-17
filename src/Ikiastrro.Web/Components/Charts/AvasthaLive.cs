using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.PlanetaryStates;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;
using Ikiastrro.Web.Components.Workspace;

namespace Ikiastrro.Web.Components.Charts;

/// <summary>
/// A chart's avasthas worked out live, with the reason for each state. The stored facts
/// (tbl_Fact_PlanetaryState) carry only state ids and exist for the chart's own run; this runs the same
/// <see cref="PlanetaryStateComputer"/> over the chart's key details, conjunctions and aspects, so it works for any
/// chart type the page shows and every state comes with its reasons. Baaladi and Sayanaadi appear only for D1
/// (they need the exact degree and the birth ghati).
/// </summary>
public static class AvasthaLive
{
    public static IReadOnlyList<PlanetaryStateFact> For(LoadedChart chart, PlanetaryStateRuleSet rules, double? janmaGhatis) =>
        PlanetaryStateComputer.Compute(
            new ChartAnalysisInput(chart.ChartType, Enum.Parse<ZodiacName>(chart.AscendantSign), new List<PlanetPosition>()),
            chart.KeyDetails, rules, janmaGhatis, chart.Conjunctions, chart.Aspects);

    /// <summary>Planet name → its reasons, the shape <c>PlanetaryStateTable</c> reads.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<StateReason>> ReasonsByPlanet(IEnumerable<PlanetaryStateFact> facts) =>
        facts.ToDictionary(f => f.Planet, f => (IReadOnlyList<StateReason>)f.Reasons);
}
