using Ikiastrro.Core.Engines.Dasha;

namespace Ikiastrro.Web.Components.Pages;

/// <summary>One active instant shared by the date input and saved dasha selections.</summary>
public sealed class TransitSelection
{
    public static readonly TimeSpan IstOffset = TimeSpan.FromMinutes(330);
    private readonly IReadOnlyList<DashaPeriodRecord> _periods;
    private readonly TimeSpan _periodOffset;

    public TransitSelection(IReadOnlyList<DashaPeriodRecord> periods, TimeSpan periodOffset, DateTimeOffset now)
    {
        _periods = periods;
        _periodOffset = periodOffset;
        SetDate(now.ToOffset(IstOffset).Date);
    }

    public DateTimeOffset ActiveUtc { get; private set; }
    public DateTime ActiveIst => ActiveUtc.ToOffset(IstOffset).DateTime;
    public DashaPeriodRecord? Maha { get; private set; }
    public DashaPeriodRecord? Antar { get; private set; }
    public DashaPeriodRecord? Pratyantar { get; private set; }
    public IReadOnlyList<DashaPeriodRecord> Periods => _periods;
    public IReadOnlyList<DashaPeriodRecord> AntarPeriods => Maha?.Children ?? [];
    public IReadOnlyList<DashaPeriodRecord> PratyantarPeriods => Antar?.Children ?? [];

    // Levels 4-7 are not stored; they are made on demand from the Vimshottari proportions (DashaSubdivision).
    private readonly DashaPeriodRecord?[] _deep = new DashaPeriodRecord?[4];
    public DashaPeriodRecord? Sookshma => _deep[0];
    public DashaPeriodRecord? Prana => _deep[1];
    public DashaPeriodRecord? Deha => _deep[2];
    public DashaPeriodRecord? Level7 => _deep[3];
    public DashaPeriodRecord? Deep(int index) => _deep[index];

    /// <summary>The periods to choose from at deep level <paramref name="index"/> (0 = Sookshma … 3 = level 7): the children of the level above.</summary>
    public IReadOnlyList<DashaPeriodRecord> DeepPeriods(int index)
    {
        var parent = index == 0 ? Pratyantar : _deep[index - 1];
        if (parent is null || Maha is null || Antar is null || Pratyantar is null) return [];
        var lords = new List<string> { Maha.Lord, Antar.Lord, Pratyantar.Lord };
        for (var i = 0; i < index; i++) lords.Add(_deep[i]!.Lord);
        return DashaSubdivision.Children(parent, lords);
    }

    public void SelectDeep(int index, int id)
    {
        var period = DeepPeriods(index).FirstOrDefault(p => p.Id == id);
        if (period is null) return;
        _deep[index] = period;
        ActiveUtc = PeriodUtc(period.StartDate);
        ResolveDeep(index + 1);
    }

    private void ResolveDeep(int from)
    {
        for (var i = from; i < _deep.Length; i++)
            _deep[i] = DeepPeriods(i).FirstOrDefault(ContainsActiveInstant);
    }

    // Saved dasha timestamps are birth-offset wall times, not UTC or machine-local time.
    public DateTimeOffset PeriodUtc(DateTime wallTime) =>
        new DateTimeOffset(DateTime.SpecifyKind(wallTime, DateTimeKind.Unspecified), _periodOffset).ToUniversalTime();

    public void SetDate(DateTime istDate)
    {
        ActiveUtc = new DateTimeOffset(DateTime.SpecifyKind(istDate.Date, DateTimeKind.Unspecified), IstOffset).ToUniversalTime();
        Maha = _periods.FirstOrDefault(ContainsActiveInstant);
        Antar = Maha?.Children.FirstOrDefault(ContainsActiveInstant);
        Pratyantar = Antar?.Children.FirstOrDefault(ContainsActiveInstant);
        ResolveDeep(0);
    }

    public void SelectMaha(int id)
    {
        var period = _periods.FirstOrDefault(p => p.Id == id);
        if (period is null) return;
        Maha = period;
        ActiveUtc = PeriodUtc(period.StartDate);
        Antar = period.Children.FirstOrDefault(ContainsActiveInstant);
        Pratyantar = Antar?.Children.FirstOrDefault(ContainsActiveInstant);
        ResolveDeep(0);
    }

    public void SelectAntar(int id)
    {
        var period = AntarPeriods.FirstOrDefault(p => p.Id == id);
        if (period is null) return;
        Antar = period;
        ActiveUtc = PeriodUtc(period.StartDate);
        Pratyantar = period.Children.FirstOrDefault(ContainsActiveInstant);
        ResolveDeep(0);
    }

    public void SelectPratyantar(int id)
    {
        var period = PratyantarPeriods.FirstOrDefault(p => p.Id == id);
        if (period is null) return;
        Pratyantar = period;
        ActiveUtc = PeriodUtc(period.StartDate);
        ResolveDeep(0);
    }

    private bool ContainsActiveInstant(DashaPeriodRecord period) =>
        PeriodUtc(period.StartDate) <= ActiveUtc && ActiveUtc < PeriodUtc(period.EndDate);
}
