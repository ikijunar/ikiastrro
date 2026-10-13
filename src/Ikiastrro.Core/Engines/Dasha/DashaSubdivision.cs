namespace Ikiastrro.Core.Engines.Dasha;

/// <summary>
/// Levels below the stored pratyantardasha (Sookshma, Prana, Deha and a 7th) as period records, made on demand from the Vimshottari
/// proportions: 9^7 rows per chart would be millions, so only three levels are stored. Ids are negative and unique only among the
/// children of one parent (-1 … -9); the wall-clock dates are on the same basis as the stored ones.
/// </summary>
public static class DashaSubdivision
{
    private const double DaysPerYear = 365.2425;

    /// <param name="parent">A period at level 3 or deeper.</param>
    /// <param name="lords">The lords from Mahadasha down to <paramref name="parent"/> inclusive, so the parent's full length is known
    /// even when the stored period was cut off at birth.</param>
    public static IReadOnlyList<DashaPeriodRecord> Children(DashaPeriodRecord parent, IReadOnlyList<string> lords)
    {
        var order = Astronomy.AstroMath.NakshatraLordOrder;
        var years = Astronomy.AstroMath.VimshottariYearsByLord;
        var chain = lords.Select(l => Enum.Parse<Astronomy.PlanetName>(l)).ToList();
        var fullDays = chain.Aggregate(1.0, (acc, l) => acc * years[l] / 120.0) * 120 * DaysPerYear;
        var cursor = parent.EndDate.AddDays(-fullDays);
        var from = order.ToList().IndexOf(chain[^1]);
        var result = new List<DashaPeriodRecord>();
        for (var slot = 0; slot < 9; slot++)
        {
            var lord = order[(from + slot) % 9];
            var end = cursor.AddDays(fullDays * years[lord] / 120.0);
            if (end > parent.StartDate)
                result.Add(new DashaPeriodRecord
                {
                    Id = -(slot + 1), LevelNumber = parent.LevelNumber + 1, SequenceInParent = slot + 1, Lord = lord.ToString(),
                    StartDate = cursor < parent.StartDate ? parent.StartDate : cursor,
                    EndDate = end > parent.EndDate ? parent.EndDate : end,
                });
            cursor = end;
        }
        return result;
    }
}
