namespace Ikiastrro.Core.Dasha;

/// <summary>
/// One row of dbo.tvf_Chart_LifeWeeks — a single week (1-4000, rammyps's "assume every person has
/// 4000 weeks" framing, 2026-08-27) with the Mahadasha/Antardasha/Pratyantardasha lord active
/// during it. Backs the life-in-weeks grid (Web/Components/Pages/LifeWeeks.razor).
/// </summary>
public class LifeWeek
{
    public int WeekNumber { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public string? MahaLord { get; set; }
    public string? AntarLord { get; set; }
    public string? PratyantarLord { get; set; }
}
