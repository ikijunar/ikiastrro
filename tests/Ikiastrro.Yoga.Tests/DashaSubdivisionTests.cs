using Ikiastrro.Core.Engines.Dasha;
using Xunit;

namespace Ikiastrro.Yoga.Tests;

public class DashaSubdivisionTests
{
    [Fact]
    public void Children_tile_the_parent_starting_with_its_own_lord()
    {
        // A full Mars/Sun/Venus pratyantardasa: 7y x 6y x 20y / 120^2 of a 365.2425-day year, ending 1 Apr 2010.
        var days = 7.0 * 6 * 20 / 14400 * 365.2425;
        var end = new DateTime(2010, 4, 1);
        var pd = new DashaPeriodRecord { Lord = "Venus", LevelNumber = 3, StartDate = end.AddDays(-days), EndDate = end };
        var kids = DashaSubdivision.Children(pd, new[] { "Mars", "Sun", "Venus" });
        Assert.Equal(9, kids.Count);
        Assert.Equal("Venus", kids[0].Lord);
        Assert.Equal(4, kids[0].LevelNumber);
        Assert.Equal(pd.StartDate, kids[0].StartDate, TimeSpan.FromSeconds(1));
        Assert.Equal(pd.EndDate, kids[^1].EndDate, TimeSpan.FromSeconds(1));
        for (var i = 1; i < kids.Count; i++) Assert.Equal(kids[i - 1].EndDate, kids[i].StartDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void A_cut_off_first_period_drops_the_children_that_ended_before_birth()
    {
        var days = 7.0 * 6 * 20 / 14400 * 365.2425;
        var end = new DateTime(2010, 4, 1);
        var pd = new DashaPeriodRecord { Lord = "Venus", LevelNumber = 3, StartDate = end.AddDays(-days / 2), EndDate = end };
        var kids = DashaSubdivision.Children(pd, new[] { "Mars", "Sun", "Venus" });
        Assert.True(kids.Count < 9);
        Assert.Equal(pd.StartDate, kids[0].StartDate);
        Assert.Equal(pd.EndDate, kids[^1].EndDate, TimeSpan.FromSeconds(1));
    }
}
