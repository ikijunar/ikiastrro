using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Calculators;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Dasha;

/// <summary>
/// Computes the classical Vimshottari Mahadasha/Antardasha/Pratyantardasha sequence from a birth
/// chart's Moon position, per rammyps's 2026-08-27 decision (3 levels — see methods_prodmag.md
/// for the decision record).
///
/// Deliberately NOT an IChartCalculator — that interface is planet-position/house shaped
/// (ComputeAnalysisInput -> ChartAnalyzer), and Dasha has no such shape. VimshottariDashaRepository
/// (Data) is what gives this a tbl_ChartResults row (ChartType="VimshottariDasha") plus
/// tbl_Chart_DashaPeriods rows, bypassing ChartAnalyzer entirely.
///
/// Algorithm: birth lands at one exact point inside the nested 120-year cycle. All 3 levels are
/// simultaneously "partial" at birth — the first Mahadasha, its first Antardasha, AND that
/// Antardasha's first Pratyantardasha all start already-in-progress. This deliberately avoids the
/// common shortcut bug of only pro-rating the Mahadasha and leaving deeper levels full-length.
/// Real calendar dates use 365.2425 days/year (standard solar year, matching JHora/AstroSage
/// convention) — flagged as a stated assumption, not a classical rule with no alternative.
/// </summary>
public static class VimshottariDashaCalculator
{
    public const string ChartType = "VimshottariDasha";
    private const double DaysPerYear = 365.2425;
    private const int TotalCycleYears = 120;

    /// <summary>Nakshatra-lord cycle order (Aswini's lord first) — nakshatraIndex % 9 indexes directly into this. Shared with ChartAnalyzer via AstroMath.NakshatraLordOrder (2026-08-28) — same classical cycle, one source of truth.</summary>
    private static readonly IReadOnlyList<PlanetName> LordOrder = AstroMath.NakshatraLordOrder;

    /// <summary>Shared with AstroMath (single source of truth) — same classical 120-year split the KP nakshatra sub-lord division uses.</summary>
    private static readonly IReadOnlyDictionary<PlanetName, int> YearsByLord = AstroMath.VimshottariYearsByLord;

    /// <summary>
    /// Computes the full Mahadasha -> Antardasha -> Pratyantardasha tree for the given birth chart,
    /// covering at least <paramref name="minimumCoverageYears"/> of real time from birth (default
    /// 120 — the full classical cycle, matching tbl_Dim_LifeCalendar's ~121-year range). Whole
    /// Mahadashas are always generated in full — the last one may run somewhat past the requested
    /// coverage rather than being cut off mid-period, same as any Dasha table you'd see elsewhere.
    /// </summary>
    public static List<DashaPeriod> Compute(BirthDetails birthDetails, double minimumCoverageYears = 120)
    {
        var localMoment = BirthMomentFactory.Create(birthDetails);
        var positions = SwissEphemerisProvider.GetSiderealPositions(localMoment, birthDetails.Latitude, birthDetails.Longitude);
        var moonLongitude = positions.PlanetLongitudes[PlanetName.Moon];

        var (nakshatraIndex, fractionElapsed) = AstroMath.GetNakshatraIndexAndFractionElapsed(moonLongitude);
        var mahaLordIndex = nakshatraIndex % 9;
        var mahaElapsedYears = fractionElapsed * YearsByLord[LordOrder[mahaLordIndex]];

        // --- Phase A: find exactly where birth lands in the nested hierarchy ---
        var (antarSlot, antarLordIndex, antarElapsedYears, _) =
            FindSlot(mahaLordIndex, YearsByLord[LordOrder[mahaLordIndex]], mahaElapsedYears);
        var (pratyantarSlot, _, pratyantarElapsedYears, _) =
            FindSlot(antarLordIndex, YearsByLord[LordOrder[mahaLordIndex]] * YearsByLord[LordOrder[antarLordIndex]] / (double)TotalCycleYears, antarElapsedYears);

        // --- Phase B: walk forward from that point, emitting whole periods until coverage is met ---
        var roots = new List<DashaPeriod>();
        var maxDays = minimumCoverageYears * DaysPerYear;
        var cursorDays = 0.0;
        var mahaOffset = 0;

        while (true)
        {
            var thisMahaLordIndex = (mahaLordIndex + mahaOffset) % 9;
            var thisMahaLord = LordOrder[thisMahaLordIndex];
            var isFirstMaha = mahaOffset == 0;
            var mahaFullYears = (double)YearsByLord[thisMahaLord];
            var mahaYearsRemaining = isFirstMaha ? mahaFullYears - mahaElapsedYears : mahaFullYears;

            var mahaStartDays = cursorDays;
            var mahaEndDays = mahaStartDays + mahaYearsRemaining * DaysPerYear;

            var mahaPeriod = new DashaPeriod
            {
                LevelNumber = 1,
                SequenceInParent = thisMahaLordIndex + 1,
                Lord = thisMahaLord,
                StartDate = localMoment.AddDays(mahaStartDays),
                EndDate = localMoment.AddDays(mahaEndDays),
                StartDayOffset = (int)Math.Round(mahaStartDays),
                EndDayOffset = (int)Math.Round(mahaEndDays) - 1
            };
            roots.Add(mahaPeriod);

            var antarStartSlot = isFirstMaha ? antarSlot : 0;
            var antarCursorDays = mahaStartDays;

            for (var antarOffset = antarStartSlot; antarOffset < 9; antarOffset++)
            {
                var thisAntarLordIndex = (thisMahaLordIndex + antarOffset) % 9;
                var thisAntarLord = LordOrder[thisAntarLordIndex];
                var isFirstAntar = isFirstMaha && antarOffset == antarSlot;
                var thisAntarFullYears = mahaFullYears * YearsByLord[thisAntarLord] / TotalCycleYears;
                var antarYearsRemaining = isFirstAntar ? thisAntarFullYears - antarElapsedYears : thisAntarFullYears;

                var antarStartDays = antarCursorDays;
                var antarEndDays = antarStartDays + antarYearsRemaining * DaysPerYear;

                var antarPeriod = new DashaPeriod
                {
                    LevelNumber = 2,
                    SequenceInParent = antarOffset + 1,
                    Lord = thisAntarLord,
                    StartDate = localMoment.AddDays(antarStartDays),
                    EndDate = localMoment.AddDays(antarEndDays),
                    StartDayOffset = (int)Math.Round(antarStartDays),
                    EndDayOffset = (int)Math.Round(antarEndDays) - 1
                };
                mahaPeriod.Children.Add(antarPeriod);

                var pratyaStartSlot = isFirstAntar ? pratyantarSlot : 0;
                var pratyaCursorDays = antarStartDays;

                for (var pratyaOffset = pratyaStartSlot; pratyaOffset < 9; pratyaOffset++)
                {
                    var thisPratyaLordIndex = (thisAntarLordIndex + pratyaOffset) % 9;
                    var thisPratyaLord = LordOrder[thisPratyaLordIndex];
                    var isFirstPratya = isFirstAntar && pratyaOffset == pratyantarSlot;
                    var thisPratyaFullYears = thisAntarFullYears * YearsByLord[thisPratyaLord] / TotalCycleYears;
                    var pratyaYearsRemaining = isFirstPratya ? thisPratyaFullYears - pratyantarElapsedYears : thisPratyaFullYears;

                    var pratyaStartDays = pratyaCursorDays;
                    var pratyaEndDays = pratyaStartDays + pratyaYearsRemaining * DaysPerYear;

                    antarPeriod.Children.Add(new DashaPeriod
                    {
                        LevelNumber = 3,
                        SequenceInParent = pratyaOffset + 1,
                        Lord = thisPratyaLord,
                        StartDate = localMoment.AddDays(pratyaStartDays),
                        EndDate = localMoment.AddDays(pratyaEndDays),
                        StartDayOffset = (int)Math.Round(pratyaStartDays),
                        EndDayOffset = (int)Math.Round(pratyaEndDays) - 1
                    });

                    pratyaCursorDays = pratyaEndDays;
                }

                antarCursorDays = antarEndDays;
            }

            cursorDays = mahaEndDays;
            mahaOffset++;

            if (cursorDays >= maxDays) break;
        }

        return roots;
    }

    /// <summary>
    /// Given a period's own starting-lord index and full duration, plus how many years have already
    /// elapsed within it, finds which of its 9 standard sub-slots (cycling from that same lord)
    /// contains that elapsed point, and the residual elapsed time within that sub-slot (for
    /// recursing one level deeper — Mahadasha -> Antardasha -> Pratyantardasha).
    /// </summary>
    private static (int Slot, int LordIndex, double ElapsedYears, double FullYears) FindSlot(
        int parentLordIndex, double parentFullYears, double elapsedYears)
    {
        var cumulative = 0.0;
        for (var slot = 0; slot < 9; slot++)
        {
            var lordIndex = (parentLordIndex + slot) % 9;
            var slotFullYears = parentFullYears * YearsByLord[LordOrder[lordIndex]] / TotalCycleYears;
            if (elapsedYears < cumulative + slotFullYears || slot == 8)
            {
                return (slot, lordIndex, elapsedYears - cumulative, slotFullYears);
            }
            cumulative += slotFullYears;
        }
        throw new InvalidOperationException("Unreachable — 9 slots always sum to parentFullYears.");
    }
}
