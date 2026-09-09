namespace Ikiastrro.Core.Engines.Strength;

public sealed record BhavaBalaComponentResult(
    int HouseNumber,
    string ComponentCode,
    double ValueVirupas,
    string MethodCode,
    string Narrative);

public sealed record BhavaBalaResult(
    int HouseNumber,
    IReadOnlyList<BhavaBalaComponentResult> Components,
    double BhavaBalaVirupas,
    double BhavaBalaRupas);
