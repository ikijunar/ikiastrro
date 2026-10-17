using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Lords;
using Ikiastrro.Core.Models;
using Xunit;

namespace Ikiastrro.Yoga.Tests;

public class PlanetLordTriadTests
{
    [Theory]
    [InlineData(0.5, ZodiacName.Aries)]        // Ashwini pada 1
    [InlineData(3.4, ZodiacName.Taurus)]       // Ashwini pada 2
    [InlineData(10.5, ZodiacName.Cancer)]      // Ashwini pada 4 is slot 3
    [InlineData(13.4, ZodiacName.Leo)]         // Bharani pada 1 is slot 4
    public void PadaSignOf_RunsThroughTheNavamsaSignsFromAries(double longitude, ZodiacName expected)
    {
        Assert.Equal(expected, PlanetLordTriad.PadaSignOf(longitude));
    }

    [Fact]
    public void PadaSignOf_LastPada_IsPisces_AndWrapsAtTheCircle()
    {
        Assert.Equal(ZodiacName.Pisces, PlanetLordTriad.PadaSignOf(359.9));        // slot 107, 107 mod 12 = 11
        Assert.Equal(ZodiacName.Aries, PlanetLordTriad.PadaSignOf(360.0));          // wraps to 0
        Assert.Equal(ZodiacName.Aries, PlanetLordTriad.PadaSignOf(108 * PlanetLordTriad.PadaDegrees + 0.1));
    }

    [Fact]
    public void For_ReadsTheSignAndStarLords_AndDerivesThePadaLordFromTheLongitude()
    {
        var row = new ChartKeyDetail
        {
            Planet = "Mars", PointKind = "Graha", Sign = "Aries", SignLordPlanet = "Mars",
            NirayanaLongitudeDegrees = 3.4, NakshatraLordPlanet = "Ketu", NakshatraSubLordPlanet = "Venus"
        };

        var triad = PlanetLordTriad.For(row);

        Assert.Equal("Mars", triad.SignLord);
        Assert.Equal("Ketu", triad.StarLord);
        Assert.Equal(ZodiacName.Taurus, triad.PadaSign);
        Assert.Equal("Venus", triad.PadaLord);                  // Taurus is owned by Venus
        Assert.Equal("Venus", triad.SubLord);
        Assert.Equal(["Ketu", "Venus"], triad.Lords());          // Mars is its own sign lord, so it is not listed behind itself
    }

    [Fact]
    public void PadaLord_IsTheSameInEveryVarga_BecauseItComesFromTheTrueLongitude()
    {
        ChartKeyDetail Row(string sign, string signLord) => new()
        {
            Planet = "Sun", PointKind = "Graha", Sign = sign, SignLordPlanet = signLord,
            NirayanaLongitudeDegrees = 100.25, NakshatraLordPlanet = "Saturn"
        };

        var d1 = PlanetLordTriad.For(Row("Cancer", "Moon"));
        var d9 = PlanetLordTriad.For(Row("Scorpio", "Mars"));

        Assert.Equal(d1.PadaLord, d9.PadaLord);
        Assert.Equal(d1.StarLord, d9.StarLord);
        Assert.NotEqual(d1.SignLord, d9.SignLord);              // only the sign lord follows the varga
    }
}
