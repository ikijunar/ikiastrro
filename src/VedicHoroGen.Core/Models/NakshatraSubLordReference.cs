namespace VedicHoroGen.Core.Models;

/// <summary>One row of dbo.tbl_NakshatraSubLords (migration 033) — the Vimshottari-proportioned
/// sub-lord (KP) divisions within each nakshatra. 243 rows. Read-only.</summary>
public record NakshatraSubLordReference(
    int Id, byte NakshatraId, byte SubSequenceNumber, byte SubLordId, decimal StartDegree, decimal EndDegree,
    byte? RulingPlanetId);
