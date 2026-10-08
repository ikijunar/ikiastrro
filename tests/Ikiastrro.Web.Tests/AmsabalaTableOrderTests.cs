using Bunit;
using Ikiastrro.Core.Models;
using Ikiastrro.Data;
using Ikiastrro.Web.Components.Charts;
using Ikiastrro.Web.Components.Workspace;
using Xunit;

namespace Ikiastrro.Web.Tests;

/// <summary>Aṁśabala · Vaiśeṣikāṃśa lists planets by their count (own / mūlatrikoṇa / exalted vargas), highest first.</summary>
public sealed class AmsabalaTableOrderTests : BunitContext
{
    private static AmsabalaRow Row(string planet, byte good) => new(planet, "SHADVARGA", 6, good, null, "");

    [Fact]
    public void Rows_AreOrderedByCountDescending_TiesKeepSunToSaturnOrder()
    {
        // Classical order is Sun, Moon, Mars, Mercury, Jupiter, Venus, Saturn.
        AmsabalaRow[] rows =
        [
            Row("SUN", 1), Row("MOON", 3), Row("MARS", 0), Row("MERCURY", 3),
            Row("JUPITER", 5), Row("VENUS", 1), Row("SATURN", 0)
        ];
        var cut = Render<AmsabalaTable>(p => p
            .Add(x => x.Rows, rows)
            .Add(x => x.Groups, [new AmsabalaGroupMember("SHADVARGA", "D1")])
            .Add(x => x.Vargottama, [])
            .Add(x => x.Charts, new Dictionary<string, LoadedChart>())
            .Add(x => x.Scheme, "SHADVARGA"));

        var order = cut.FindAll(".am-bar-planet").Select(e => e.TextContent.Trim()).ToArray();
        Assert.Equal(["Jupiter", "Moon", "Mercury", "Sun", "Venus", "Mars", "Saturn"], order);
    }
}
