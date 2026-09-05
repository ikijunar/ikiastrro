using Ikiastrro.Core.Models;

namespace Ikiastrro.Web.Components.Workspace;

/// <summary>Groups the 21 chart types for the Web Workspace's varga rail — by life-area
/// (tbl_Dim_ChartType.PrimaryLifeAreaId -> tbl_Dim_LifeArea.WorkspaceGroupCode, migration 30's
/// PVR Table 11 taxonomy), DB-sourced (rammyps's call, 2026-09-05). Replaces this class's old
/// hardcoded classical Shadvarga(6)/Saptavarga(7)/Dasavarga(10)/Shodasavarga(16)/Extra-vargas
/// bundles, which lived here as a static field, sourced from docs/reference-chart-varga-index.md.
/// Every varga has exactly one WorkspaceGroupCode — the project's own Web-tab grouping
/// (PersonalityHealth / Relationships / Career / Money / Other) already rolled onto the 20 fine
/// PVR areas by migration 30 itself, so this class just reads it rather than inventing a new one.</summary>
public static class VargaBundles
{
    /// <summary>WorkspaceGroupCode -> display title for the rail's Disclosure headers.</summary>
    private static readonly IReadOnlyDictionary<string, string> GroupTitles = new Dictionary<string, string>
    {
        ["PersonalityHealth"] = "Personality & Health",
        ["Relationships"] = "Relationships",
        ["Career"] = "Career",
        ["Money"] = "Money",
        ["Other"] = "Other",
    };

    /// <summary>Category groups in display order (each group ordered first by its lowest member's
    /// DisplayOrder, so the group containing D1 leads), each group's own codes ordered by
    /// DisplayOrder. A chart type with no PrimaryLifeAreaId yet (shouldn't happen — migration 30
    /// mapped all 21) falls into "Other" rather than being dropped.</summary>
    public static IReadOnlyList<(string Title, IReadOnlyList<string> Codes)> Groups(
        IReadOnlyList<ChartTypeRow> chartTypes, IReadOnlyList<LifeAreaRow> lifeAreas)
    {
        var groupCodeByAreaId = lifeAreas.ToDictionary(a => (int)a.Id, a => a.WorkspaceGroupCode);
        string GroupCodeOf(ChartTypeRow t) =>
            t.PrimaryLifeAreaId is int id && groupCodeByAreaId.TryGetValue(id, out var g) ? g : "Other";

        return chartTypes
            .GroupBy(GroupCodeOf)
            .OrderBy(g => g.Min(t => t.DisplayOrder))
            .Select(g => (GroupTitles.GetValueOrDefault(g.Key, g.Key),
                (IReadOnlyList<string>)g.OrderBy(t => t.DisplayOrder).Select(t => t.Code).ToList()))
            .ToList();
    }

    /// <summary>Rail order = the sequence a prev/next in VargaView walks — groups top-to-bottom
    /// (per Groups' own ordering), codes in each group's listed order.</summary>
    public static IReadOnlyList<string> RailOrder(IReadOnlyList<ChartTypeRow> chartTypes, IReadOnlyList<LifeAreaRow> lifeAreas) =>
        Groups(chartTypes, lifeAreas).SelectMany(g => g.Codes).ToList();
}
