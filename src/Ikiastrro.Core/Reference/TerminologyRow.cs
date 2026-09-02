namespace Ikiastrro.Core.Reference;

/// <summary>One language-neutral concept row of <c>dbo.tbl_Astro_Terminology</c>.</summary>
public sealed record TerminologyRow(
    int TerminologyId, string Category, string Code, string? ParentCode,
    string? EngineCode, int? NumericKey, string? FormulaSummary, int DisplayOrder, bool IsActive);
