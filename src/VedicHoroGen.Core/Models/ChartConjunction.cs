namespace VedicHoroGen.Core.Models;

/// <summary>
/// One conjunction (Yuti) — a pair of grahas sharing the same sign in a given chart. Ascendant
/// excluded (not a graha). Shared by every chart type (D1, D9, and any future divisional chart) —
/// see ChartAnalyzer.
/// </summary>
public class ChartConjunction
{
    public int Id { get; set; }
    public int ChartResultId { get; set; }
    public int BirthDetailId { get; set; }
    public string ChartType { get; set; } = string.Empty;
    public string Planet1 { get; set; } = string.Empty;
    public string Planet2 { get; set; } = string.Empty;
    public string Sign { get; set; } = string.Empty;
    public int HouseNumberFromLagna { get; set; }

    /// <summary>
    /// How tight the conjunction is, 0-180°. Only meaningful for D1 — real ecliptic longitude is a
    /// continuous measure of closeness there. A varga sign is a discrete bucket that repeats every
    /// few degrees across the zodiac, so two grahas "conjunct" in the same D9 sign can sit far apart
    /// in real longitude; that number wouldn't mean "tightness" there, so it's left null.
    /// </summary>
    public decimal? DegreeSeparation { get; set; }
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
