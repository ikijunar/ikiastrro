using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.PlanetaryStates;
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
    IReadOnlyList<PlanetaryStateFact> States);
    // P2+ add: Dispositors, Strength, Yogas as additive record fields.
