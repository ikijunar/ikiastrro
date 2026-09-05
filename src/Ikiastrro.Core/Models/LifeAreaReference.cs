namespace Ikiastrro.Core.Models;

/// <summary>One row of dbo.tbl_Dim_LifeArea (migration 30) — the 20 PVR "Integrated Approach"
/// Table 11 spheres of life, one per divisional chart's primary reading. Read-only.
/// WorkspaceGroupCode ("PersonalityHealth"/"Relationships"/"Career"/"Money"/"Other") is the
/// project's own Web-tab grouping rolled onto the fine PVR areas, not from the book — it drives
/// the varga rail's category grouping (VargaBundles.cs, 2026-09-05).</summary>
public record LifeAreaRow(
    byte Id, string AreaCode, string AreaName, string Description,
    string PlaneOfExistence, string WorkspaceGroupCode, byte SortOrder);
