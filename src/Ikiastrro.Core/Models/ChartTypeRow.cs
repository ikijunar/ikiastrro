namespace Ikiastrro.Core.Models;

/// <summary>One row of dbo.tbl_Dim_ChartType — the controlled vocabulary for ChartResults.ChartTypeId.</summary>
public record ChartTypeRow(
    int Id, string Code, string DisplayName, int? DivisionalFactor, string Category, int DisplayOrder);
