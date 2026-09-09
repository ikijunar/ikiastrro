namespace Ikiastrro.Core.Engines.Strength;

/// <summary>One named Shadbala sub-component in virupas.</summary>
public sealed record ShadbalaComponentResult(
    string BalaCode,
    string SubComponentCode,
    double ValueVirupas,
    string MethodCode,
    string Narrative);

/// <summary>Computed PVR-first Shadbala for one classical planet.</summary>
public sealed record PlanetaryStrengthResult(
    string Planet,
    IReadOnlyList<ShadbalaComponentResult> Components,
    double SthanaBalaVirupas,
    double DigBalaVirupas,
    double KalaBalaVirupas,
    double CheshtaBalaVirupas,
    double NaisargikaBalaVirupas,
    double DrikBalaVirupas,
    double ShadbalaVirupas,
    double ShadbalaRupas,
    double? IshtaBala,
    double? KashtaBala);
