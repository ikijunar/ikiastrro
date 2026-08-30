namespace Ikiastrro.Core.Models;

/// <summary>One row of dbo.tbl_Nakshatras (migration 022) — static nakshatra reference:
/// zodiac span, ruling planet, and traditional attributes (guna, gana, yoni, nadi, varna, tatva). Read-only.</summary>
public record NakshatraReference(
    byte Id, string NakshatraName, decimal StartDegree, decimal EndDegree, byte RulingPlanetId,
    byte SequenceNumber, string? RulingDeity, string? Symbol, string? Guna, string? Gana,
    string? YoniAnimal, string? YoniGender, string? Nadi, string? Varna, string? Tatva, string? Direction,
    byte PrimaryRasiId, bool StraddlesSignBoundary);
