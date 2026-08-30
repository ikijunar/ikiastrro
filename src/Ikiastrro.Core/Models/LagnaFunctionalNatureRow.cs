namespace Ikiastrro.Core.Models;

/// <summary>One row of tbl_Dim_LagnaFunctionalNature (migration 031) — Raman's functional
/// classification of one planet for one Lagna. FunctionalNature null = the source does not classify it.</summary>
public record LagnaFunctionalNatureRow(byte Id, byte LagnaSignId, byte PlanetId, string? FunctionalNature, byte? Rank, string? Notes);
