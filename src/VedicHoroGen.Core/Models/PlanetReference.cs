namespace VedicHoroGen.Core.Models;

/// <summary>One row of dbo.tbl_Planets (migration 021) — static graha reference:
/// naisargika nature, sign rulership flag, Vimshottari dasha years and sequence order. Read-only.</summary>
public record PlanetReference(
    byte Id, string PlanetName, string PlanetNameSanskrit, string NaturalNature,
    string? ConditionalRule, bool RulesSign, byte VimshottariYears, byte VimshottariSequenceOrder);
