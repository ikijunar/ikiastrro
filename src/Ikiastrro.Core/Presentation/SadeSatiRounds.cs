using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Presentation;

/// <summary>
/// Shared shaping for tvf_Chart_SadeSatiPeriods rows — folding the raw retrograde-split rows back
/// into contiguous windows, and (new) grouping those windows into the 3 chronological Sade-Sati
/// "cycles" a South Indian chart template shows as flanking/center "Round" panels. Extracted from
/// SadeSatiTable.razor (2026-09-05) so the grouping/age logic has one owner instead of two copies —
/// SadeSatiTable and the new template both call this.
/// </summary>
public static class SadeSatiRounds
{
    /// <summary>One consolidated Saturn-affliction window — contiguous rows folded together by
    /// GroupContiguous, with a human label/detail for display.</summary>
    public record Window(string Label, string Detail, DateTime Start, DateTime? End);

    /// <summary>One chronological Sade Sati cycle: its own Sade Sati (the 3 Dhaiyas already folded
    /// into one window), Kantaka Shani (4th from Moon) and Ashtama Shani (8th from Moon) windows —
    /// the "Round" a South Indian chart template groups these into. Any of the three can be null if
    /// this person's lifetime only reaches N of the ~3 occurrences each type has. ShowDates is true
    /// on exactly the one Round (of up to 3) whose span is nearest `asOf`; the other Rounds are
    /// meant to display as age ranges only (rammyps's explicit call, 2026-09-05).</summary>
    public record Round(int Index, Window? SadeSati, Window? Kantaka, Window? Ashtama, bool ShowDates);

    /// <summary>Folds rows separated by ≤ 400 days into one window (retrograde re-entry gaps are
    /// months; the gap to the next occurrence is ~decades).</summary>
    public static IEnumerable<(DateTime start, DateTime? end, string sign)> GroupContiguous(IReadOnlyList<SadeSatiPeriod> rows)
    {
        if (rows.Count == 0) yield break;
        var start = rows[0].StartDateTimeUtc!.Value;
        var end = rows[0].EndDateTimeUtc;
        var sign = rows[0].SaturnSign;

        for (var i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            var gapFrom = end ?? start;
            if ((r.StartDateTimeUtc!.Value - gapFrom).TotalDays > 400)
            {
                yield return (start, end, sign);
                start = r.StartDateTimeUtc.Value;
                end = r.EndDateTimeUtc;
                sign = r.SaturnSign;
            }
            else if (r.EndDateTimeUtc is null || (end is not null && r.EndDateTimeUtc > end))
            {
                end = r.EndDateTimeUtc;
            }
        }
        yield return (start, end, sign);
    }

    /// <summary>The transit log starts in 1930, so tvf_Chart_SadeSatiPeriods returns windows from
    /// decades before birth — keep only those that reach into this person's lifetime.</summary>
    private static bool InLife(DateTime birthDate, (DateTime start, DateTime? end, string sign) g) =>
        (g.end ?? DateTime.MaxValue) >= birthDate;

    /// <summary>The full in-life, chronologically ordered window list — one row per window, exactly
    /// what SadeSatiTable renders.</summary>
    public static IReadOnlyList<Window> BuildWindows(IReadOnlyList<SadeSatiPeriod> periods, DateTime birthDate)
    {
        var sadeSati = GroupContiguous(periods
                .Where(p => p.PeriodType.StartsWith("SadeSati_Dhaiya") && p.StartDateTimeUtc is not null)
                .OrderBy(p => p.StartDateTimeUtc).ToList())
            .Where(g => InLife(birthDate, g)).ToList();

        var windows = new List<Window>();
        for (var i = 0; i < sadeSati.Count; i++)
            windows.Add(new Window($"Sade Sati {i + 1}", "12th → 2nd from Moon", sadeSati[i].start, sadeSati[i].end));

        foreach (var g in GroupContiguous(periods.Where(p => p.PeriodType == "KantakaShani" && p.StartDateTimeUtc is not null).OrderBy(p => p.StartDateTimeUtc).ToList()).Where(g => InLife(birthDate, g)))
            windows.Add(new Window("Kantaka Shani", $"4th from Moon · {g.sign}", g.start, g.end));

        foreach (var g in GroupContiguous(periods.Where(p => p.PeriodType == "AshtamaShani" && p.StartDateTimeUtc is not null).OrderBy(p => p.StartDateTimeUtc).ToList()).Where(g => InLife(birthDate, g)))
            windows.Add(new Window("Ashtama Shani", $"8th from Moon · {g.sign}", g.start, g.end));

        return windows.OrderBy(w => w.Start).ToList();
    }

    /// <summary>Groups the Sade Sati / Kantaka / Ashtama windows into up to 3 chronological Rounds
    /// (paired by index — the Nth Sade Sati cycle with the Nth Kantaka and Nth Ashtama window),
    /// flagging whichever Round is nearest `asOf` (past or future) to show real calendar dates.</summary>
    public static IReadOnlyList<Round> BuildRounds(IReadOnlyList<SadeSatiPeriod> periods, DateTime birthDate, DateTime asOf)
    {
        bool InLifeG((DateTime start, DateTime? end, string sign) g) => InLife(birthDate, g);

        var sadeSati = GroupContiguous(periods.Where(p => p.PeriodType.StartsWith("SadeSati_Dhaiya") && p.StartDateTimeUtc is not null).OrderBy(p => p.StartDateTimeUtc).ToList())
            .Where(InLifeG)
            .Select((g, i) => new Window($"Sade Sati {i + 1}", "12th → 2nd from Moon", g.start, g.end))
            .ToList();
        var kantaka = GroupContiguous(periods.Where(p => p.PeriodType == "KantakaShani" && p.StartDateTimeUtc is not null).OrderBy(p => p.StartDateTimeUtc).ToList())
            .Where(InLifeG)
            .Select(g => new Window("Kantaka Shani", $"4th from Moon · {g.sign}", g.start, g.end))
            .ToList();
        var ashtama = GroupContiguous(periods.Where(p => p.PeriodType == "AshtamaShani" && p.StartDateTimeUtc is not null).OrderBy(p => p.StartDateTimeUtc).ToList())
            .Where(InLifeG)
            .Select(g => new Window("Ashtama Shani", $"8th from Moon · {g.sign}", g.start, g.end))
            .ToList();

        var count = new[] { sadeSati.Count, kantaka.Count, ashtama.Count }.Max();
        var rounds = new List<Round>();
        for (var i = 0; i < count; i++)
        {
            rounds.Add(new Round(
                i + 1,
                i < sadeSati.Count ? sadeSati[i] : null,
                i < kantaka.Count ? kantaka[i] : null,
                i < ashtama.Count ? ashtama[i] : null,
                false));
        }
        if (rounds.Count == 0) return rounds;

        double Distance(Window? w)
        {
            if (w is null) return double.MaxValue;
            if (asOf >= w.Start && (w.End is null || asOf <= w.End.Value)) return 0;
            return asOf < w.Start ? (w.Start - asOf).TotalDays : (asOf - w.End!.Value).TotalDays;
        }
        double RoundDistance(Round r) => new[] { Distance(r.SadeSati), Distance(r.Kantaka), Distance(r.Ashtama) }.Min();

        var nearest = rounds.OrderBy(RoundDistance).First().Index;
        return rounds.Select(r => r with { ShowDates = r.Index == nearest }).ToList();
    }

    /// <summary>Age range at Start/End relative to BirthDate, e.g. "1–9" or "24" for a single-year span.</summary>
    public static string FormatAge(DateTime birthDate, DateTime start, DateTime? end)
    {
        var a0 = (int)((start - birthDate).TotalDays / 365.2425);
        var a1 = end is null ? a0 : (int)((end.Value - birthDate).TotalDays / 365.2425);
        return a0 == a1 ? a0.ToString() : $"{a0}–{a1}";
    }

    /// <summary>Calendar-month span, e.g. "Nov 2011 – Jan 2020" ("Apr 2032 –" if still ongoing).</summary>
    public static string FormatDateSpan(DateTime start, DateTime? end) =>
        end is null ? $"{FmtMonth(start)} –" : $"{FmtMonth(start)} – {FmtMonth(end.Value)}";

    private static string FmtMonth(DateTime d) => d.ToString("MMM yyyy");

    /// <summary>One Window's display value per a Round's ShowDates flag — real dates for the
    /// nearest-to-today Round, an age range for the other two.</summary>
    public static string FormatLine(Window w, DateTime birthDate, bool showDates) =>
        showDates ? FormatDateSpan(w.Start, w.End) : FormatAge(birthDate, w.Start, w.End);
}
