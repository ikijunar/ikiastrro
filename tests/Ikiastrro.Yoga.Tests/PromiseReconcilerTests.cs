using Ikiastrro.Core.LifeMatters.Promise;

namespace Ikiastrro.Yoga.Tests;

/// <summary>Phase 3 of docs/architecture/key_inference_promise.md section 7: the D1 x varga table.</summary>
public sealed class PromiseReconcilerTests
{
    private static LifeMatterPromise Promise(PromiseVerdict d1, PromiseVerdict? varga, Confidence d1Confidence = Confidence.Medium) =>
        new("CAREER",
            new D1Foundation(d1, d1Confidence, new Dictionary<TestimonyRole, Direction>(), []),
            varga is null ? null : new DomainConfirmation("D10", varga.Value, "x"),
            [], [], [], [], d1, Confidence.Medium, [], [], "", "", null);

    [Theory]
    [InlineData(PromiseVerdict.StrongPositive, PromiseVerdict.PositiveConditional, ReconciliationOutcome.Confirmed)]
    [InlineData(PromiseVerdict.PositiveConditional, PromiseVerdict.StrongPositive, ReconciliationOutcome.Confirmed)]
    [InlineData(PromiseVerdict.PositiveConditional, PromiseVerdict.Mixed, ReconciliationOutcome.ConfirmedWithQualification)]
    [InlineData(PromiseVerdict.StrongPositive, PromiseVerdict.WeakLimited, ReconciliationOutcome.DeliveryReduced)]
    [InlineData(PromiseVerdict.PositiveConditional, PromiseVerdict.Adverse, ReconciliationOutcome.DeliveryReduced)]
    [InlineData(PromiseVerdict.Mixed, PromiseVerdict.StrongPositive, ReconciliationOutcome.ConditionalImprovement)]
    [InlineData(PromiseVerdict.Mixed, PromiseVerdict.Mixed, ReconciliationOutcome.Unresolved)]
    [InlineData(PromiseVerdict.Mixed, PromiseVerdict.Adverse, ReconciliationOutcome.ConflictTowardDifficulty)]
    [InlineData(PromiseVerdict.Mixed, PromiseVerdict.WeakLimited, ReconciliationOutcome.ConflictTowardDifficulty)]
    [InlineData(PromiseVerdict.WeakLimited, PromiseVerdict.PositiveConditional, ReconciliationOutcome.LimitedByD1)]
    [InlineData(PromiseVerdict.Adverse, PromiseVerdict.Mixed, ReconciliationOutcome.LimitedByD1)]
    [InlineData(PromiseVerdict.WeakLimited, PromiseVerdict.WeakLimited, ReconciliationOutcome.ThinPromise)]
    [InlineData(PromiseVerdict.Adverse, PromiseVerdict.Adverse, ReconciliationOutcome.StrongObstruction)]
    [InlineData(PromiseVerdict.Adverse, PromiseVerdict.WeakLimited, ReconciliationOutcome.StrongObstruction)]
    [InlineData(PromiseVerdict.WeakLimited, PromiseVerdict.Adverse, ReconciliationOutcome.StrongObstruction)]
    public void The_D1_by_varga_table(PromiseVerdict d1, PromiseVerdict varga, ReconciliationOutcome expected) =>
        Assert.Equal(expected, PromiseReconciler.Reconcile(Promise(d1, varga)).Outcome);

    [Theory]
    [InlineData(PromiseVerdict.WeakLimited)]
    [InlineData(PromiseVerdict.Adverse)]
    [InlineData(PromiseVerdict.Mixed)]
    public void A_varga_never_raises_the_D1_verdict(PromiseVerdict d1) =>
        Assert.Equal(d1, PromiseReconciler.Reconcile(Promise(d1, PromiseVerdict.StrongPositive)).Verdict);

    [Fact]
    public void Reduced_delivery_lowers_a_strong_positive_one_step() =>
        Assert.Equal(PromiseVerdict.PositiveConditional,
            PromiseReconciler.Reconcile(Promise(PromiseVerdict.StrongPositive, PromiseVerdict.Adverse)).Verdict);

    [Fact]
    public void No_varga_leaves_D1_standing_and_says_so()
    {
        var r = PromiseReconciler.Reconcile(Promise(PromiseVerdict.PositiveConditional, null));
        Assert.Equal(ReconciliationOutcome.NotEvaluated, r.Outcome);
        Assert.Equal(PromiseVerdict.PositiveConditional, r.Verdict);
    }

    [Fact]
    public void An_indeterminate_D1_draws_no_inference_even_with_a_supportive_varga()
    {
        var r = PromiseReconciler.Reconcile(Promise(PromiseVerdict.Indeterminate, PromiseVerdict.StrongPositive));
        Assert.Equal(ReconciliationOutcome.Indeterminate, r.Outcome);
        Assert.Equal(Confidence.Low, r.Confidence);
    }

    [Fact]
    public void An_indeterminate_varga_neither_confirms_nor_reduces()
    {
        var r = PromiseReconciler.Reconcile(Promise(PromiseVerdict.StrongPositive, PromiseVerdict.Indeterminate));
        Assert.Equal(ReconciliationOutcome.Indeterminate, r.Outcome);
        Assert.Equal(PromiseVerdict.StrongPositive, r.Verdict);
    }

    [Theory]
    [InlineData(PromiseVerdict.PositiveConditional, Confidence.High, Confidence.High)]    // confirmed: D1's own confidence stands
    [InlineData(PromiseVerdict.PositiveConditional, Confidence.Medium, Confidence.Medium)]
    public void Only_a_confirmed_promise_can_reach_High_confidence(PromiseVerdict d1, Confidence d1Conf, Confidence expected) =>
        Assert.Equal(expected, PromiseReconciler.Reconcile(Promise(d1, PromiseVerdict.StrongPositive, d1Conf)).Confidence);

    [Fact]
    public void A_reduced_or_unevaluated_promise_is_capped_at_Medium()
    {
        Assert.Equal(Confidence.Medium,
            PromiseReconciler.Reconcile(Promise(PromiseVerdict.StrongPositive, PromiseVerdict.Adverse, Confidence.High)).Confidence);
        Assert.Equal(Confidence.Medium,
            PromiseReconciler.Reconcile(Promise(PromiseVerdict.StrongPositive, null, Confidence.High)).Confidence);
    }

    [Fact]
    public void The_inference_names_the_varga_chart() =>
        Assert.Contains("D10", PromiseReconciler.Reconcile(Promise(PromiseVerdict.StrongPositive, PromiseVerdict.PositiveConditional)).Inference);
}
