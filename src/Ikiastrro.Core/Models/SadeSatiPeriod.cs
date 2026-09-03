namespace Ikiastrro.Core.Models;

/// <summary>One row of tvf_Chart_SadeSatiPeriods — a Saturn-from-natal-Moon affliction window.
/// PeriodType ∈ SadeSati_Dhaiya1_Rising / SadeSati_Dhaiya2_Peak / SadeSati_Dhaiya3_Setting /
/// KantakaShani / AshtamaShani. EndDateTimeUtc null = ongoing / past the 2060 backfill boundary.</summary>
public record SadeSatiPeriod(
    string PeriodType, int SortOrder, DateTime? StartDateTimeUtc, DateTime? EndDateTimeUtc, string SaturnSign);
