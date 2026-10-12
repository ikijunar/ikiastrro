using Bunit;
using Ikiastrro.Core.LifeMatters.Promise;
using Ikiastrro.Data.Statistics;
using Ikiastrro.Web.Components.Charts;
using Xunit;

namespace Ikiastrro.Web.Tests;

public sealed class D1PromisePanelOrderTests : BunitContext
{
    private static MatterPromiseReading Matter(string code, string category, PromiseVerdict verdict)
    {
        var roles = new Dictionary<TestimonyRole, Direction>
        {
            [TestimonyRole.Lord] = Direction.Neutral, [TestimonyRole.Karaka] = Direction.Neutral, [TestimonyRole.Influence] = Direction.Neutral
        };
        var d1 = new D1Foundation(verdict, Confidence.Medium, roles, []);
        var promise = new LifeMatterPromise(code, d1, null, [], [], [], [], verdict, Confidence.Medium, [], [], "", "", null);
        var target = new MatterTargetPromise("House 1", 1, default, promise);
        return new MatterPromiseReading(code, $"Matter {code}", category, category, [target], null);
    }

    [Fact]
    public void RowsRunStrongPositiveToAdversePerCategoryKeepingReferenceOrderOnTies()
    {
        MatterPromiseReading[] readings =
        [
            Matter("A1", "SELF", PromiseVerdict.Adverse),
            Matter("A2", "SELF", PromiseVerdict.Mixed),
            Matter("A3", "SELF", PromiseVerdict.StrongPositive),
            Matter("A4", "SELF", PromiseVerdict.WeakLimited),
            Matter("A5", "SELF", PromiseVerdict.PositiveConditional),
            Matter("A6", "SELF", PromiseVerdict.Mixed),
            Matter("B1", "WEALTH", PromiseVerdict.Adverse),
            Matter("B2", "WEALTH", PromiseVerdict.StrongPositive),
        ];

        var cut = Render<D1PromisePanel>(p => p.Add(x => x.Readings, readings));

        var order = cut.FindAll(".dp-matter").Select(e => e.TextContent.Trim()).ToArray();
        Assert.Equal(
            ["Matter A3", "Matter A5", "Matter A2", "Matter A6", "Matter A4", "Matter A1", "Matter B2", "Matter B1"],
            order);
    }

    [Fact]
    public void ShowsTheVargaReconciliationBesideTheVerdictAndSaysWhenNoVargaWasRead()
    {
        var withVarga = Matter("C1", "SELF", PromiseVerdict.StrongPositive);
        var target = withVarga.Targets[0];
        withVarga = withVarga with
        {
            Targets = [target with { Promise = target.Promise with { Domain = new DomainConfirmation("D10", PromiseVerdict.Adverse, "x") } }],
        };
        var without = Matter("C2", "SELF", PromiseVerdict.Mixed);

        var cut = Render<D1PromisePanel>(p => p.Add(x => x.Readings, new[] { withVarga, without }));

        var badges = cut.FindAll(".dp-recon").Select(e => e.TextContent.Trim()).ToArray();
        Assert.Equal(["D10 · Delivery reduced"], badges);
        Assert.Contains("delivery is reduced", cut.Markup);
        Assert.Contains("No divisional chart was read", cut.Markup);
    }
}
