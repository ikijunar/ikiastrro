namespace Ikiastrro.Core.Models;

/// <summary>
/// One aspect (Drishti) — directional, planet-to-planet-or-Ascendant, per classical house-offset rules.
/// "AspectingPlanet casts a full aspect on AspectedTarget" — not necessarily mutual (Mars/Jupiter/Saturn's
/// special aspects are one-directional; only the universal 7th aspect happens to be symmetric). Shared by
/// every chart type (D1, D9, and any future divisional chart) — see ChartAnalyzer.
/// </summary>
public class ChartAspect
{
    public int Id { get; set; }
    public int ChartResultId { get; set; }
    public int BirthDetailId { get; set; }
    public string ChartType { get; set; } = string.Empty;
    public string AspectingPlanet { get; set; } = string.Empty;
    public int AspectingPlanetId { get; set; }
    /// <summary>A graha name, or "Ascendant".</summary>
    public string AspectedTarget { get; set; } = string.Empty;
    /// <summary>"Planet" | "Ascendant".</summary>
    public string AspectedTargetType { get; set; } = "Planet";
    /// <summary>FK to tbl_Planets; null when AspectedTargetType == "Ascendant".</summary>
    public int? AspectedPlanetId { get; set; }
    /// <summary>e.g. "7th", "4th", "8th", "5th", "9th", "3rd", "10th" — which classical drishti rule fired.</summary>
    public string AspectType { get; set; } = string.Empty;
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
