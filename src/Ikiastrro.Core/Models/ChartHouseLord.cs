namespace Ikiastrro.Core.Models;

/// <summary>
/// One row of tbl_Chart_HouseLords — one house's ruling sign, its lord, and where that lord actually
/// sits, for ANY chart type (D1, D9, and any future divisional chart share this one table — see
/// ChartAnalyzer).
/// </summary>
public class ChartHouseLord
{
    public int Id { get; set; }
    public int ChartResultId { get; set; }
    public int HouseNumber { get; set; }
    public string HouseSign { get; set; } = string.Empty;
    public int HouseSignId { get; set; }
    public string LordPlanet { get; set; } = string.Empty;
    public int LordPlanetId { get; set; }
    public int LordPlacedInHouseFromLagna { get; set; }
    public int LordPlacedInHouseFromSun { get; set; }
    public int LordPlacedInHouseFromMoon { get; set; }
    public string LordPlacedInSign { get; set; } = string.Empty;
    public int LordPlacedInSignId { get; set; }
    public string? LordDignityStatus { get; set; }
}
