using Bunit;
using Ikiastrro.Web.Components.Charts;
using Xunit;

namespace Ikiastrro.Web.Tests;

/// <summary>
/// Golden-SVG snapshots for the hand-rolled visual chart components. Baseline is written on
/// first run (or with IKIASTRRO_UPDATE_SNAPSHOTS=1) to docs/artifacts/ui/&lt;Name&gt;-sample.svg
/// and committed. See docs/uidesign-dataviz.md §6 and that folder's README.
///
/// WDAC on the dev machine blocks terminal `dotnet test`; run these from Visual Studio's
/// Test Explorer. `dotnet build Ikiastrro.slnx` still compiles the project as the CI gate.
/// </summary>
public class ChartSnapshotTests : BunitContext
{
    public ChartSnapshotTests()
    {
        // These components are pure markup, but a transitive shared component might poke JS;
        // loose mode keeps that from throwing instead of rendering.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void PolarWheel()
    {
        var cut = Render<PolarWheel>(ps => ps
            .Add(p => p.Points, ChartFixture.WheelPoints)
            .Add(p => p.Chords, ChartFixture.WheelChords));

        cut.MatchesGolden(nameof(PolarWheel));
    }

    [Fact]
    public void SouthIndianGrid()
    {
        var cut = Render<SouthIndianGrid>(ps => ps
            .Add(p => p.AscendantSign, "Aries")
            .Add(p => p.MoonSign, "Taurus")
            .Add(p => p.PlanetsBySign, ChartFixture.GridGlyphs)
            .Add(p => p.CenterTitle, "D1 · Rasi")
            .Add(p => p.CenterMeta, "<span>Lagna Aries · Moon Taurus</span>"));

        cut.MatchesGolden(nameof(SouthIndianGrid));
    }

    [Fact]
    public void MiniGrid()
    {
        var cut = Render<MiniGrid>(ps => ps
            .Add(p => p.PlanetsBySign, ChartFixture.GridNames)
            .Add(p => p.LagnaSign, "Aries")
            .Add(p => p.Caption, "D1"));

        cut.MatchesGolden(nameof(MiniGrid));
    }

    [Fact]
    public void CombinedD1D9Grid()
    {
        var cut = Render<CombinedD1D9Grid>(ps => ps
            .Add(p => p.D1KeyDetails, ChartFixture.D1)
            .Add(p => p.D9KeyDetails, ChartFixture.D9)
            .Add(p => p.LagnaSign, "Aries"));

        cut.MatchesGolden(nameof(CombinedD1D9Grid));
    }

    [Fact]
    public void VargottamaStrip()
    {
        var cut = Render<VargottamaStrip>(ps => ps
            .Add(p => p.D1Grahas, ChartFixture.D1)
            .Add(p => p.DnGrahas, ChartFixture.D9)
            .Add(p => p.DnCode, "D9"));

        cut.MatchesGolden(nameof(VargottamaStrip));
    }

    [Fact]
    public void ChartFrame()
    {
        var cut = Render<ChartFrame>(ps => ps
            .Add(p => p.View, "grid")
            .Add(p => p.BaseHref, "/charts/1")
            .Add(p => p.GridContent, "<div class=\"stub-grid\">grid</div>")
            .Add(p => p.WheelContent, "<div class=\"stub-wheel\">wheel</div>"));

        cut.MatchesGolden(nameof(ChartFrame));
    }
}
