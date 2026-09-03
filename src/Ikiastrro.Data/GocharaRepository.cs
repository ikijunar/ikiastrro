using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>Current sidereal position of the slow grahas (Saturn / Jupiter / Rahu) for the Gochara
/// panel. Thin wrapper over PlanetSignTransitEventsRepository — Mars was outside the transit-table
/// backfill scope, so it is deliberately not here.</summary>
public sealed class GocharaRepository
{
    private static readonly PlanetName[] Slow = { PlanetName.Saturn, PlanetName.Jupiter, PlanetName.Rahu };
    private readonly PlanetSignTransitEventsRepository _transits;

    public GocharaRepository(PlanetSignTransitEventsRepository transits) => _transits = transits;

    public IReadOnlyList<PlanetTransitSnapshot> GetSnapshots(DateTime asOfUtc) =>
        Slow.Select(p => _transits.GetSnapshot(p, asOfUtc))
            .Where(s => s is not null)
            .Select(s => s!)
            .ToList();
}
