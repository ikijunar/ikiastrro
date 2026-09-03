namespace Ikiastrro.Core.Models;

/// <summary>
/// One row of dbo.tbl_Rule_VargaScheme - how a varga chart type derives a
/// planet's varga sign, under one rule-set. SignRuleKey names the C# rule
/// (VargaSignRuleFactory); SignRuleKind is a descriptive tag ('Linear' /
/// 'Special'). D1 is NOT represented here (identity rasi).
/// </summary>
public record VargaScheme(
    string ChartType,
    int DivisionFactor,
    string MethodCode,
    string SignRuleKind,
    string SignRuleKey);
