namespace Ikiastrro.Core.Models;

/// <summary>One row of vw_Chart_HouseNakshatraSpan — a (house → sign → nakshatra-pada) slice.
/// Whole-sign, so each house has exactly 9 pada rows (2.25 nakshatras).</summary>
public record HouseNakshatraSpanRow(
    int ChartResultId, int BirthDetailId, string ChartType, int HouseNumber, string HouseSign,
    byte HouseSignId, string LordPlanet, byte NakshatraId, string NakshatraName, byte PadaNumber,
    decimal PadaStartDegree, decimal PadaEndDegree, string NakshatraLordName, string NavamsaSignName);
