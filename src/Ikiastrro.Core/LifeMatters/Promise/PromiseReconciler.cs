namespace Ikiastrro.Core.LifeMatters.Promise;

// Phase 3 of docs/architecture/key_inference_promise.md section 7: reconcile the D1 promise with the
// matter's own varga. The varga never creates a promise D1 lacks, so the reconciled verdict is the
// D1 verdict, at most lowered; the varga only changes what the promise says about delivery.

/// <summary>What the D1 promise and the matter's varga say together.</summary>
public enum ReconciliationOutcome
{
    /// <summary>No varga was read (none applies, or its data is missing): D1 stands alone.</summary>
    NotEvaluated,
    /// <summary>D1 and the varga both support the matter.</summary>
    Confirmed,
    /// <summary>D1 supports; the varga is mixed: promise holds, delivery is qualified.</summary>
    ConfirmedWithQualification,
    /// <summary>D1 supports; the varga is weak or adverse: promise exists, delivery reduced, delayed or conflicted.</summary>
    DeliveryReduced,
    /// <summary>D1 mixed; the varga supports: conditional improvement.</summary>
    ConditionalImprovement,
    /// <summary>D1 and the varga are both mixed: the contest is unresolved.</summary>
    Unresolved,
    /// <summary>D1 mixed; the varga weak or adverse: conflict tends toward difficulty.</summary>
    ConflictTowardDifficulty,
    /// <summary>D1 weak or adverse; the varga supports: capacity or desire exists, D1 limits realisation.</summary>
    LimitedByD1,
    /// <summary>Both weak or adverse, at least one adverse: strong obstruction or denial.</summary>
    StrongObstruction,
    /// <summary>Both weak/limited: the promise is thin and the varga does not rescue it.</summary>
    ThinPromise,
    /// <summary>D1 or the varga is indeterminate: no inference is drawn.</summary>
    Indeterminate,
}

/// <param name="Verdict">The D1 verdict, never raised by the varga; lowered one step where delivery is reduced.</param>
/// <param name="Inference">The sentence for the page.</param>
public sealed record PromiseReconciliation(
    ReconciliationOutcome Outcome, PromiseVerdict Verdict, Confidence Confidence, string Inference);

public static class PromiseReconciler
{
    private enum Tone { Positive, Mixed, Weak, Adverse, Unknown }

    private static Tone ToneOf(PromiseVerdict v) => v switch
    {
        PromiseVerdict.StrongPositive or PromiseVerdict.PositiveConditional => Tone.Positive,
        PromiseVerdict.Mixed => Tone.Mixed,
        PromiseVerdict.WeakLimited => Tone.Weak,
        PromiseVerdict.Adverse => Tone.Adverse,
        _ => Tone.Unknown,
    };

    public static PromiseReconciliation Reconcile(LifeMatterPromise d1)
    {
        ArgumentNullException.ThrowIfNull(d1);
        var chart = d1.Domain?.Chart;
        var d = ToneOf(d1.D1.Verdict);
        var v = d1.Domain is null ? Tone.Unknown : ToneOf(d1.Domain.Verdict);

        if (d == Tone.Unknown)
            return new(ReconciliationOutcome.Indeterminate, d1.D1.Verdict, Confidence.Low,
                "The D1 promise cannot be judged, so the varga is not used to infer one.");
        if (d1.Domain is null)
            return new(ReconciliationOutcome.NotEvaluated, d1.D1.Verdict, Cap(d1.D1.Confidence),
                "No divisional chart was read for this matter; the D1 promise stands without a delivery cross-check.");
        if (v == Tone.Unknown)
            return new(ReconciliationOutcome.Indeterminate, d1.D1.Verdict, Cap(d1.D1.Confidence),
                $"{chart} could not be judged, so it neither confirms nor reduces the D1 promise.");

        var (outcome, text) = (d, v) switch
        {
            (Tone.Positive, Tone.Positive) => (ReconciliationOutcome.Confirmed, $"Confirmed promise: D1 and {chart} both support the matter."),
            (Tone.Positive, Tone.Mixed) => (ReconciliationOutcome.ConfirmedWithQualification, $"The D1 promise holds; {chart} is mixed, so delivery comes with qualifications."),
            (Tone.Positive, _) => (ReconciliationOutcome.DeliveryReduced, $"The promise exists in D1, but {chart} does not support it: delivery is reduced, delayed or conflicted."),
            (Tone.Mixed, Tone.Positive) => (ReconciliationOutcome.ConditionalImprovement, $"D1 is mixed and {chart} supports the matter: conditional improvement."),
            (Tone.Mixed, Tone.Mixed) => (ReconciliationOutcome.Unresolved, $"D1 and {chart} are both mixed: promise and obstruction remain unresolved."),
            (Tone.Mixed, _) => (ReconciliationOutcome.ConflictTowardDifficulty, $"D1 is mixed and {chart} does not support the matter: the conflict tends toward difficulty."),
            (Tone.Weak or Tone.Adverse, Tone.Positive or Tone.Mixed) => (ReconciliationOutcome.LimitedByD1, $"{chart} shows capacity or desire, but the D1 foundation limits realisation; the varga does not create a promise D1 lacks."),
            (Tone.Weak, Tone.Weak) => (ReconciliationOutcome.ThinPromise, $"D1 and {chart} are both weak: the promise is thin and the varga does not rescue it."),
            _ => (ReconciliationOutcome.StrongObstruction, $"D1 and {chart} both point against the matter: strong obstruction or denial."),
        };

        // Only a positive promise whose delivery is reduced steps down; the varga never raises a verdict.
        var verdict = outcome == ReconciliationOutcome.DeliveryReduced && d1.D1.Verdict == PromiseVerdict.StrongPositive
            ? PromiseVerdict.PositiveConditional
            : d1.D1.Verdict;

        // High needs the D1 foundation itself at High and the varga agreeing; a reconciled conflict is Medium at best.
        var confidence = outcome == ReconciliationOutcome.Confirmed ? d1.D1.Confidence : Cap(d1.D1.Confidence);
        return new(outcome, verdict, confidence, text);
    }

    private static Confidence Cap(Confidence c) => c == Confidence.High ? Confidence.Medium : c;
}
