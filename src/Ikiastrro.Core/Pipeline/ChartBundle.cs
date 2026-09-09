using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.PlanetaryStates;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Pipeline;

/// <summary>
/// The DB-free result of <see cref="ChartPipeline.Run"/> — everything the compute half of
/// ChartGenerationService produces between "have a BirthDetails" and "write rows", minus
/// persistence. No repository, connection or DB-generated id is involved: <see cref="States"/>
/// carry no ChartResultId (the caller stamps that after inserting the parent ChartResult row,
/// exactly as ChartGenerationService.PersistAnalytics does today).
/// </summary>
public sealed record ChartBundle(
    BirthDetails Birth,
    SiderealPositions Positions,
    SunTimes SunTimes,
    IReadOnlyList<ChartAnalysisInput> Charts,
    IReadOnlyDictionary<string, string> CharaKarakaByPlanet,
    IReadOnlyList<PlanetaryStateFact> States)
{
    public IReadOnlyList<Ikiastrro.Core.Engines.Yoga.YogaInputEvaluation> YogaInputs =>
        Ikiastrro.Core.Engines.Yoga.YogaInputEvaluator.Evaluate(Birth, Charts, SunTimes);
    /// <summary>PVR-first Shadbala results. Strength is computed over the complete chart bundle,
    /// so Saptavargaja can see the available D1/D2/D3/D7/D9/D12/D30 inputs.</summary>
    public IReadOnlyList<PlanetaryStrengthResult> Strengths { get; init; } = Array.Empty<PlanetaryStrengthResult>();

    /// <summary>Bhava Bala for the D1 houses, calculated from the same planetary strengths.</summary>
    public IReadOnlyList<BhavaBalaResult> BhavaStrengths { get; init; } = Array.Empty<BhavaBalaResult>();

    /// <summary>Explicit D1/D9 same-sign results; Vargottama is reported separately from Shadbala points.</summary>
    public IReadOnlyList<VargottamaResult> Vargottama { get; init; } = Array.Empty<VargottamaResult>();
}
