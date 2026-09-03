namespace Ikiastrro.Core.Engines.Strength;

public sealed record ShadbalaResult(string Planet, decimal TotalRupas, IReadOnlyDictionary<string, decimal> ComponentRupas);
