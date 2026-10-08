using Bunit;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.LifeMatters.Promise;
using Ikiastrro.Core.Models;
using Ikiastrro.Data;
using Ikiastrro.Web.Components.Charts;
using Xunit;

namespace Ikiastrro.Web.Tests;

/// <summary>Argala &amp; Virodhargala rows are sorted by nature: Subha, then Mixed, then Paapa, then houses with no argala.</summary>
public sealed class ArgalaTableOrderTests : BunitContext
{
    private const ZodiacName Lagna = ZodiacName.Aries;

    private static ArgalaFactRow Argala(int house, PlanetName planet) =>
        new("D1", "House", house.ToString(), (byte)house, "ARGALA", 2, true, planet.ToString(), false, false, null);

    [Fact]
    public void Rows_AreOrdered_Subha_Mixed_Paapa_ThenNone_WithHouseOrderInsideEachGroup()
    {
        var all = Enum.GetValues<PlanetName>().ToList();
        var good = all.Where(p => D1PromiseEngine.NatureDirection(Lagna, p) == Direction.Supportive).Take(2).ToList();
        var bad = all.Where(p => D1PromiseEngine.NatureDirection(Lagna, p) == Direction.Obstructive).Take(2).ToList();
        Assert.Equal(2, good.Count);
        Assert.Equal(2, bad.Count);

        ArgalaFactRow[] facts =
        [
            Argala(1, bad[0]),                         // Paapa
            Argala(2, good[0]),                        // Subha
            Argala(4, good[0]), Argala(4, bad[0]),     // Mixed (one of each)
            Argala(5, bad[1]),                         // Paapa
            Argala(7, good[1])                         // Subha
        ];
        var grahas = new List<ChartKeyDetail>
        {
            new() { PointKind = "Graha", Planet = "Ascendant", Sign = "Aries", NirayanaLongitudeDegrees = 10 },
            new() { PointKind = "Graha", Planet = "Sun", Sign = "Leo", NirayanaLongitudeDegrees = 130 }
        };

        var cut = Render<ArgalaTable>(p => p.Add(x => x.Facts, facts).Add(x => x.Grahas, grahas).Add(x => x.AscendantSign, "Aries"));

        var houses = cut.FindAll("tbody tr").Select(r => int.Parse(r.QuerySelector("td")!.TextContent.Trim().TrimEnd('*'))).ToArray();
        // Subha 2,7 · Mixed 4 · Paapa 1,5 · no argala 3,6,8,9,10,11,12
        Assert.Equal([2, 7, 4, 1, 5, 3, 6, 8, 9, 10, 11, 12], houses);
    }
}
