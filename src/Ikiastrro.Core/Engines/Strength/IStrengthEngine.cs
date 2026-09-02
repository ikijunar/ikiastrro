namespace Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Pipeline;

/// <summary>Ṣaḍbala, Iṣṭa/Kaṣṭa, Bhāva Bala, Vimśopaka. Built in Plan 3.</summary>
public interface IStrengthEngine
{
    IReadOnlyList<ShadbalaResult> Shadbala(ChartBundle bundle);
    IReadOnlyList<VimsopakaResult> Vimsopaka(ChartBundle bundle);
}
