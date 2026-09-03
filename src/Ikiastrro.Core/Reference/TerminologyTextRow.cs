namespace Ikiastrro.Core.Reference;

/// <summary>One per-language display/technical text row of <c>dbo.tbl_Astro_TerminologyText</c>,
/// carrying its parent <see cref="TerminologyId"/> so it can be keyed back to a <see cref="TerminologyRow.Code"/>.</summary>
public sealed record TerminologyTextRow(
    int TerminologyId, string LanguageCode, string Script, string Name,
    string? TraditionalName, string? ShortDescription, string? TechnicalDefinition,
    string? CalculationMethod, string? SourceRefCode);
