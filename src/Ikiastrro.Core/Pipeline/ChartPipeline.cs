using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Karakas;
using Ikiastrro.Core.Engines.PlanetaryStates;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Pipeline;

/// <summary>
/// The DB-free compute façade over the chart engines. <see cref="Run"/> performs exactly the steps
/// ChartGenerationService runs between "have a BirthDetails" and "write rows" — chart calculation,
/// the pure chara-karaka assignment, and the planetary-state (avastha) computation — and returns
/// them as a <see cref="ChartBundle"/>. It opens no connection, touches no repository, and produces
/// no DB-generated id; adding persistence back is the caller's job.
/// </summary>
public sealed class ChartPipeline
{
    private readonly ChartCalculationOrchestrator _orchestrator;
    private readonly PlanetaryStateRuleSet _planetaryStateRules;

    public ChartPipeline(ChartCalculationOrchestrator orchestrator, PlanetaryStateRuleSet planetaryStateRules)
    {
        _orchestrator = orchestrator;
        _planetaryStateRules = planetaryStateRules;
    }

    /// <summary>
    /// Compute every registered chart type for <paramref name="birth"/> plus the derived chara-karaka
    /// labels and planetary-state facts. No I/O beyond the Swiss Ephemeris files the engines already
    /// read; no DB.
    /// </summary>
    public ChartBundle Run(BirthDetails birth)
    {
        // Sidereal positions + sunrise/sunset — computed the same way ChartGenerationService does
        // (SwissEphemerisProvider, no DB). Positions feeds the chara-karaka helper below; SunTimes is
        // carried for downstream consumers (night-birth-sensitive points).
        var positions = SwissEphemerisProvider.GetSiderealPositions(birth);
        var sunTimes = SwissEphemerisProvider.GetSunTimes(birth);

        var computed = _orchestrator.CalculateAll(birth);
        var charts = computed.Select(c => c.Input).ToList();

        var charaKarakaByPlanet = CharaKarakaByPlanet(positions);

        // States — PlanetaryStateComputer.Compute per chart, exactly as ChartGenerationService's
        // PersistAnalytics does now (chara-karaka is stamped onto the local ChartKeyDetail list first,
        // matching that method; PlanetaryStateComputer does not read it). Flattened across all charts;
        // no ChartResultId is set here.
        var states = new List<PlanetaryStateFact>();
        foreach (var input in charts)
        {
            var (keyDetails, _, _, _) = ChartAnalyzer.Compute(input);
            foreach (var r in keyDetails)
                if (r.PointKind == "Graha" && charaKarakaByPlanet.TryGetValue(r.Planet, out var ck))
                    r.CharaKaraka = ck;
            states.AddRange(PlanetaryStateComputer.Compute(input, keyDetails, _planetaryStateRules));
        }

        return new ChartBundle(birth, positions, sunTimes, charts, charaKarakaByPlanet, states);
    }

    /// <summary>
    /// The Jaimini 8-karaka (Ashta) label per graha, from this person's D1 degree-within-sign.
    /// Lifted verbatim from ChartGenerationService.CharaKarakaByPlanet — pure, no I/O.
    /// </summary>
    private static IReadOnlyDictionary<string, string> CharaKarakaByPlanet(SiderealPositions ctx)
    {
        var degIn = new Dictionary<PlanetName, double>();
        foreach (var p in new[] { PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury,
                                  PlanetName.Jupiter, PlanetName.Venus, PlanetName.Saturn, PlanetName.Rahu })
            degIn[p] = ctx.PlanetLongitudes[p] % 30.0;
        return CharaKarakaCalculator.Assign(degIn)
            .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());
    }
}
