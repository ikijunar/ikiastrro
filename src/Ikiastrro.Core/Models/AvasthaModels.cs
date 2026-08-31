namespace Ikiastrro.Core.Models;

/// <summary>One row of dbo.tbl_Dim_AvasthaState — the vocabulary of avastha states across every
/// avastha system (Baaladi, Jagradadi, Deeptadi, Lajjitadi, Sayanadi). Read-only reference.</summary>
public record AvasthaStateRow(
    byte Id, string AvasthaSystem, string StateName, byte SequenceOrder, string? Meaning);

/// <summary>One row of dbo.tbl_Rule_BaaladiState — the within-sign degree bands (odd vs even sign)
/// and the classical effect fraction for one Baaladi state, under a given RuleSetId. StateName is
/// joined in from tbl_Dim_AvasthaState for convenience.</summary>
public record BaaladiRuleRow(
    byte Id, byte RuleSetId, byte AvasthaStateId, string StateName,
    decimal OddSignFromDegree, decimal OddSignToDegree,
    decimal EvenSignFromDegree, decimal EvenSignToDegree,
    decimal EffectFraction);

/// <summary>One row of dbo.tbl_Rule_JagradadiState — maps a DignityStatus value to a Jagradadi
/// state (Jagrat / Swapna / Sushupti) under a given RuleSetId. StateName joined in from
/// tbl_Dim_AvasthaState.</summary>
public record JagradadiRuleRow(
    byte Id, byte RuleSetId, string DignityStatus, byte AvasthaStateId, string StateName);

/// <summary>Everything a chart's avastha computation needs from the Rule/Dim layer for one
/// RuleSetId — loaded once by AvasthaRuleRepository, handed to PlanetAvasthaComputer.</summary>
public record AvasthaRuleSet(
    byte RuleSetId,
    IReadOnlyList<BaaladiRuleRow> Baaladi,
    IReadOnlyDictionary<string, JagradadiRuleRow> JagradadiByDignity);

/// <summary>One row of dbo.tbl_Fact_PlanetAvastha — the computed avastha states for one planet in
/// one chart. Ascendant excluded. Baaladi is D1-only (needs within-sign degree); Jagradadi is
/// populated for every chart type. RuleSetId records which rule rows produced this fact.</summary>
public class PlanetAvasthaFact
{
    public int Id { get; set; }
    public int ChartResultId { get; set; }
    public string Planet { get; set; } = string.Empty;
    /// <summary>FK to tbl_Planets for Planet.</summary>
    public byte? PlanetId { get; set; }
    public byte RuleSetId { get; set; }

    /// <summary>FK to tbl_Dim_AvasthaState (AvasthaSystem = 'Baaladi'). D1 only — null otherwise.</summary>
    public byte? BaaladiStateId { get; set; }
    /// <summary>Classical strength fraction for the Baaladi state (Baala .25 / Kumara .50 / Yuva 1 / Vriddha .125 / Mrita 0). D1 only.</summary>
    public decimal? BaaladiEffectFraction { get; set; }

    /// <summary>FK to tbl_Dim_AvasthaState (AvasthaSystem = 'Jagradadi'). Populated for every chart type. Null only if DignityStatus was absent.</summary>
    public byte? JagradadiStateId { get; set; }
}
