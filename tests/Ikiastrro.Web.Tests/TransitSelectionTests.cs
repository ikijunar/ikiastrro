using Ikiastrro.Core.Engines.Dasha;
using Ikiastrro.Web.Components.Pages;
using Xunit;

namespace Ikiastrro.Web.Tests;

public class TransitSelectionTests
{
    [Fact]
    public void InitialDateUsesIstEvenWhenUtcIsStillPreviousDay()
    {
        var state = new TransitSelection([], TimeSpan.Zero, new DateTimeOffset(2026, 9, 7, 20, 0, 0, TimeSpan.Zero));
        Assert.Equal(new DateTime(2026, 9, 8), state.ActiveIst);
        Assert.Equal(new DateTimeOffset(2026, 9, 7, 18, 30, 0, TimeSpan.Zero), state.ActiveUtc);
    }

    [Fact]
    public void PeriodSelectionUsesStoredOffsetAndRetainsExactStartTime()
    {
        var maha = Period(1, new DateTime(2026, 1, 1, 8, 15, 0), new DateTime(2027, 1, 1));
        var antar = Period(2, new DateTime(2026, 9, 10, 9, 45, 0), new DateTime(2026, 12, 1));
        maha.Children.Add(antar);
        var state = new TransitSelection([maha], TimeSpan.FromHours(-4), DateTimeOffset.UtcNow);
        state.SelectMaha(1);
        state.SelectAntar(2);
        Assert.Equal(new DateTimeOffset(2026, 9, 10, 13, 45, 0, TimeSpan.Zero), state.ActiveUtc);
        Assert.Equal(new DateTime(2026, 9, 10, 19, 15, 0), state.ActiveIst);
        Assert.Same(maha, state.Maha);
        Assert.Same(antar, state.Antar);
    }

    [Fact]
    public void DateChangeResolvesThePeriodAtAnExclusiveEndBoundary()
    {
        var first = Period(1, new DateTime(2026, 1, 1), new DateTime(2026, 9, 7));
        var second = Period(2, first.EndDate, new DateTime(2027, 1, 1));
        var state = new TransitSelection([first, second], TransitSelection.IstOffset, DateTimeOffset.UtcNow);
        state.SetDate(new DateTime(2026, 9, 7));
        Assert.Same(second, state.Maha);
        state.SetDate(new DateTime(2028, 1, 1));
        Assert.Null(state.Maha);
        Assert.Null(state.Antar);
    }

    [Fact]
    public void AntarFromAnotherMahaCannotChangeTheActiveSelection()
    {
        var first = Period(1, new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));
        var second = Period(2, first.EndDate, new DateTime(2028, 1, 1));
        second.Children.Add(Period(3, second.StartDate, second.EndDate));
        var state = new TransitSelection([first, second], TimeSpan.Zero, DateTimeOffset.UtcNow);
        state.SelectMaha(1);
        var before = state.ActiveUtc;
        state.SelectAntar(3);
        Assert.Equal(before, state.ActiveUtc);
        Assert.Same(first, state.Maha);
        Assert.Null(state.Antar);
    }

    private static DashaPeriodRecord Period(int id, DateTime start, DateTime end) =>
        new() { Id = id, StartDate = start, EndDate = end, Lord = "Venus" };
}

