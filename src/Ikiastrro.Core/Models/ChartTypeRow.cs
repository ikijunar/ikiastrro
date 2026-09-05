namespace Ikiastrro.Core.Models;

/// <summary>One row of dbo.tbl_Dim_ChartType — the controlled vocabulary for ChartResults.ChartTypeId.</summary>
public record ChartTypeRow(
    int Id, string Code, string DisplayName, int? DivisionalFactor, string Category, int DisplayOrder,
    /// <summary>Short reader-facing phrase for what the chart signifies (e.g. D1's "Personality,
    /// Expression, Logic"), shown in the South Indian chart template's dynamic heading. Null for
    /// any chart type that hasn't been given one yet (only D1 is seeded so far).</summary>
    string? Description = null,
    /// <summary>Short "Primary Domain" phrase (e.g. D1's "Self / Overall Life", D9's "Marriage /
    /// Dharma"), distinct from <see cref="Description"/>. Every chart type has one.</summary>
    string? ChartShortDescription = null,
    /// <summary>FK to tbl_Dim_LifeArea — the PVR Table 11 "primary area of life" this divisional
    /// chart is read for (migration 30, D:\@ClaudeSpace\ikiastrro's own life-area taxonomy work).
    /// Drives the Web workspace's varga rail grouping via LifeAreaRow.WorkspaceGroupCode — see
    /// VargaBundles.cs (2026-09-05).</summary>
    int? PrimaryLifeAreaId = null);
