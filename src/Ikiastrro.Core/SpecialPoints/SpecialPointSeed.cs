namespace Ikiastrro.Core.SpecialPoints;

/// <summary>A special point's D1 sidereal longitude, before any varga projection.
/// PointKind is 'Arudha' | 'SpecialLagna' | 'Upagraha'. Code: 'AL', 'A2'..'A12',
/// 'HL', 'Gulika', 'Maandi'.</summary>
public sealed record SpecialPointSeed(string Code, string PointKind, double NirayanaLongitudeDegrees);
