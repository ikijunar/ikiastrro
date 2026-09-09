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
    public IReadOnlyList<DashaPeriodRecord> Periods => _periods;
    public IReadOnlyList<DashaPeriodRecord> AntarPeriods => Maha?.Children ?? [];

    // Saved dasha timestamps are birth-offset wall times, not UTC or machine-local time.
    public DateTimeOffset PeriodUtc(DateTime wallTime) =>
        new DateTimeOffset(DateTime.SpecifyKind(wallTime, DateTimeKind.Unspecified), _periodOffset).ToUniversalTime();

    public void SetDate(DateTime istDate)
    {
        ActiveUtc = new DateTimeOffset(DateTime.SpecifyKind(istDate.Date, DateTimeKind.Unspecified), IstOffset).ToUniversalTime();
        Maha = _periods.FirstOrDefault(ContainsActiveInstant);
        Antar = Maha?.Children.FirstOrDefault(ContainsActiveInstant);
    }

    public void SelectMaha(int id)
    {
        var period = _periods.FirstOrDefault(p => p.Id == id);
        if (period is null) return;
        Maha = period;
        ActiveUtc = PeriodUtc(period.StartDate);
        Antar = period.Children.FirstOrDefault(ContainsActiveInstant);
    }

    public void SelectAntar(int id)
    {
        var period = AntarPeriods.FirstOrDefault(p => p.Id == id);
        if (period is null) return;
        Antar = period;
        ActiveUtc = PeriodUtc(period.StartDate);
    }

    private bool ContainsActiveInstant(DashaPeriodRecord period) =>
        PeriodUtc(period.StartDate) <= ActiveUtc && ActiveUtc < PeriodUtc(period.EndDate);
}
