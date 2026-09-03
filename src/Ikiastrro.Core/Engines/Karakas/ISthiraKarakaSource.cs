namespace Ikiastrro.Core.Engines.Karakas;

/// <summary>Sthira (fixed) Karaka: planet -> house significator. Built in Plan 2.</summary>
public interface ISthiraKarakaSource
{
    IReadOnlyDictionary<int, string> HouseSignificatorByHouse();   // 1..12 -> planet
}
