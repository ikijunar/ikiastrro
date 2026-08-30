namespace VedicHoroGen.Web.Components.Charts;

/// <summary>
/// Maps a Dasha lord name (as stored — "Ketu", "Venus", ... "Mercury") to its CSS custom
/// property in tokens.css (--dasha-ketu etc.). Shared by DashaTimeline, DashaLegend, and
/// LifeWeeks so the lord-&gt;color mapping is defined exactly once.
/// </summary>
public static class DashaLordColors
{
    /// <summary>Classical Vimshottari cycle order — also the fixed order the 9-color palette was validated against (dataviz skill, adjacent pairs), so keep this order when rendering a legend.</summary>
    public static readonly IReadOnlyList<string> LordsInCycleOrder = new[]
    {
        "Ketu", "Venus", "Sun", "Moon", "Mars", "Rahu", "Jupiter", "Saturn", "Mercury"
    };

    public static string CssVar(string lord) => $"var(--dasha-{lord.ToLowerInvariant()})";
}
