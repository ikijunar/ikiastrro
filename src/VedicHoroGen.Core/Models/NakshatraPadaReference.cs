namespace VedicHoroGen.Core.Models;

/// <summary>One row of dbo.tbl_NakshatraPadas (migration 022) — the four 3°20' padas of each
/// nakshatra with their rasi and navamsa sign. 27 nakshatras x 4 = 108 rows. Read-only.</summary>
public record NakshatraPadaReference(
    int Id, byte NakshatraId, byte PadaNumber, decimal StartDegree, decimal EndDegree,
    byte RasiId, byte NavamsaSignId, byte? RulingPlanetId);
