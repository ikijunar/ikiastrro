namespace VedicHoroGen.Core.Models;

/// <summary>Outcome of a ChartGenerationService run — what was (re)written, whether Dasha ran, what was left alone.</summary>
public record GenerationReport(IReadOnlyList<string> ChartTypesWritten, bool DashaWritten, IReadOnlyList<string> Skipped);
