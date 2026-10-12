---
last_updated: 2026-10-04
workstream: architecture
togaf: C — application (Core inference layer, then Key Inference page)
reflects: proposal on master ad510eb. Nothing in this doc is built yet.
---

# Key Inference: from strength statistics to a promise judgement

Status: proposal, from rammyps's brief of 2026-10-04. Decisions needed are in section 9.

The brief: Life Matters should answer "what does the natal chart promise about this matter, what supports or damages that promise, and does the relevant divisional chart confirm it?" Strength averages must not decide the promise. D1 states the promise, the matter's varga confirms and describes delivery, special lagnas show particular manifestations, and strength says how powerfully each testimony operates.

## 1. What Key Inference does today

| Layer | Where | What it gives |
|---|---|---|
| Matter, focus, karaka, subject→chart | `LifeMatterFocusResolver`, `tbl_Rule_LifeMatterFocus`, `LifeMatterSubjectRule.ChartTypeCode` | The target house or special point per matter, its karakas, and the chart it is read in |
| Strength statistics | `LifeMatterStatistics` / `HouseStatistics` | Capacity (Ṣaḍbala), Consistency (Amsabala), Context (SAV, lord's BAV, independent Bhava Bala, Argala) as three 0–100 axes. `StrengthPercent` is their mean |
| Step 5 influences | `TargetInfluences.Read` | Graha and rāśi dṛṣṭi, Argala and Virodhargala, house-from-target position, bādhaka, functional nature; a per-planet lean (Supports, Obstructs, Mixed, Neutral) |
| Wording | `LifeMatterReading` | Bands, strongest and limiting factors, headline text |
| Page | `KeyInference.razor` (1518 lines) | Percent-led answer, factor lists, population comparison, "every perspective in every chart" |

The evidence is largely there. The gap is the hierarchy: a percentage leads, strength is read as if it were direction, and several signals that share one cause are counted separately.

## 2. Gaps against the brief

1. **No promise verdict or confidence.** The page shows a percent and a band, not Strong positive / Positive but conditional / Mixed / Weak / Adverse / Indeterminate.
2. **Direction and capacity are fused.** A strong malefic reads as strong, not as strongly obstructive. `HouseStatistics.Bands` counts Strong minus Weak.
3. **Correlated signals are counted separately.** Dignity and dīptādi avastha share a placement; the lord's Ṣaḍbala feeds both Capacity and the lord's band; SAV and BAV are both Ashtakavarga; a conjunction and the yoga it forms.
4. **D1 is not a gate.** The varga is read with the same rubric but the two are not reconciled.
5. **Special lagnas are perspectives of equal rank**, selectable but with no per-matter policy of essential, optional or irrelevant.
6. **Nakshatra and sub-lord chain, bādhaka, yogas, conjunction effects on the lord** are shown in places but are not testimonies in a verdict.

## 3. The model

New namespace `Ikiastrro.Core.LifeMatters.Promise`. Pure; the page and the statistics service only feed it.

```csharp
enum Direction { Supportive, Obstructive, Mixed, Neutral }
enum Capacity  { Strong, Moderate, Weak, Unknown }          // reuses StrengthBands, never converted to Direction
enum PromiseVerdict { StrongPositive, PositiveConditional, Mixed, WeakLimited, Adverse, Indeterminate }
enum Confidence { High, Medium, Low }                        // independent of the verdict: Mixed is not Low

record Testimony(
    string SourceCode, string RuleSetVersion,                // SRC_* and rule set, for the audit trail
    string Chart, string Reference,                          // D1/D10, Lagna/AL/...
    TestimonyRole Role,                                      // Target, Lord, Karaka, Influence, Yoga, Varga
    string Subject,                                          // house, pada, planet or yoga involved
    Direction Direction, Capacity Capacity,
    string Family,                                           // independence group, section 5
    string Explanation);

record LifeMatterPromise(
    D1Foundation D1, DomainConfirmation? Domain,
    IReadOnlyList<ManifestationLens> Lenses,
    IReadOnlyList<Testimony> Positive, IReadOnlyList<Testimony> Negative,
    IReadOnlyList<Contradiction> Contradictions,
    PromiseVerdict Verdict, Confidence Confidence,
    IReadOnlyList<string> MissingEvidence,
    int? TechnicalSupportIndex);                             // today's percent, kept but not leading
```

## 4. D1 foundation: an analytical order, not equal votes

Per matter, for the resolved target (house or pada) in D1, in this order, each step producing zero or more `Testimony`:

1. Target house or pada, and its sign.
2. House lord and its placement (house from target and from Lagna, dignity, dispositor).
3. Natural and matter-specific karakas, same questions.
4. Occupants.
5. Graha dṛṣṭi and rāśi dṛṣṭi onto the target (`TargetInfluences` already).
6. Conjunctions touching the lord, karakas or target.
7. Sign dignity and dispositors.
8. Nakshatra lord and sub-lord chain of lord and karakas, as a refinement only.
9. Strength and avasthas: they set `Capacity` on a testimony already created. They never create a direction.
10. Argala and Virodhargala: intervention and whether it is blocked. A separate testimony role, not a strength point.
11. Functional nature, bādhaka, yogas.
12. Synthesis into positive, negative and mixed propositions.

Rules the engine enforces:
- **Strength is capacity.** An exalted 10th lord is a positive promise; if it is combust, poorly placed and under Virodhargala, the promise stays but is damaged (positive direction, weak capacity, with a negative family). High Ṣaḍbala on a functional malefic is strongly obstructive.
- **Nakshatra and avastha refine, they do not outweigh** house lordship and placement. They adjust capacity or add a refinement note, never flip a direction.

## 5. Independence groups

Evidence stays visible, but the verdict counts one testimony per family:

| Family | Members counted once |
|---|---|
| Placement condition | sign dignity, dīptādi avastha, combustion, retrograde of the same planet |
| Planet capacity | Ṣaḍbala, Amsabala, the capacity score derived from them |
| Ashtakavarga | SAV of the sign, the lord's BAV there |
| Conjunction | a conjunction and a yoga formed by that same conjunction |
| Intervention | Argala and its Virodhargala on the same pair |
| Lordship placement | lord in house X from target and from Lagna |

The family list is one static table covered by tests, so it can be audited and changed in one place.

## 6. Classifying the promise and the confidence

The verdict comes from the families, not the percent:

| Verdict | Rule (heuristic, labelled) |
|---|---|
| Strong positive | at least three supportive families across target, lord and karaka roles, and no obstructive family on the lord or target |
| Positive but conditional | supportive on the lord or target, with one obstructive family or a weak capacity on a principal role |
| Mixed | supportive and obstructive families both on principal roles |
| Weak / limited | principal lord, target and karaka cannot sustain what indications exist |
| Adverse | obstructive families dominate, on the lord or target |
| Indeterminate | required evidence missing, or an exact tie |

Confidence: **High** when house, lord and karaka agree and the varga agrees; **Medium** when two principal roles agree; **Low** when data is sparse or contradiction is serious. Mixed and Low are separate fields.

The family counts above are the part I most want checked (section 9).

## 7. Varga reconciliation

The matter's varga comes from `LifeMatterSubjectRule.ChartTypeCode`. The brief's mapping (D2 wealth, D3 siblings, D4 property, D7 children, D9 marriage, D10 career, D12 parents, D16 vehicles, D20 spiritual practice, D24 education, D27 strength, D30 misfortune, D60 only when birth-time reliability suffices) is the cross-check against what the seeds already say. Any difference is listed in a diff, not silently overwritten.

The varga is judged with the same D1 rubric (target, lord, karakas, influences, dignity, strength, Argala), giving its own verdict, then reconciled:

| D1 | Varga | Inference |
|---|---|---|
| Positive | Positive | Confirmed promise |
| Positive | Weak or adverse | Promise exists, delivery reduced, delayed or conflicted |
| Weak or adverse | Positive | Capacity or desire exists, D1 foundation limits realisation |
| Adverse | Adverse | Strong obstruction or denial |
| Mixed | Positive | Conditional improvement |
| Mixed | Adverse | Conflict tends toward difficulty |

The varga never creates a promise D1 lacks. D9 is a confirmation of planetary integrity only where it is the matter's own varga, not an equal-weight input. D60 is excluded unless birth-time reliability is on file (decision 3).

## 8. Special lagnas as lenses

A per-matter lens policy: **essential**, **optional** or **hidden**. Lagna (actual experience), Chandra (mental experience), Sūrya (purpose, authority), Ārūḍha (public image), Hora (wealth), Ghati (power), Indu (fortune, where the method supports it), Karakāṁśa (calling), Pāka (circumstances through the Lagna lord), graha lagna (matter seen from its karaka). Precedence: D1 Lagna, matter varga, relevant ārūḍha, relevant special lagna, Moon and Sun perspectives, then the rest under "Explore perspectives". Career, for example, shows actual career (D1 and D10), public standing (AL/A10), authority (GL), inner vocation (Karakāṁśa) and mental satisfaction (Moon) separately, never averaged.

This needs a small rule table `tbl_Rule_LifeMatterLens` (matter, lens, policy, source). The seeds are the brief's table, tagged `SRC_IKIASTRRO_SYNTHESIS`, not a classical citation.

## 9. Page, delivery and decisions

Page order, replacing the percent-led answer: headline verdict card (promise, confidence, D1 foundation, varga confirmation, dominant support and obstruction, "Why this conclusion?") → promise chain (D1 → varga → lens → final) → principal testimony grouped by role → influences and delivery condition → cross-chart confirmation limited to D1, the matter varga, one sourced universal and the relevant lenses, with every other chart under "Explore other charts". The percentage stays as a labelled technical index.

Delivery phases:
1. Vocabulary and model (section 3), no behaviour change.
2. D1 promise engine (sections 4–6) with synthetic-chart tests: confirmed promise, damaged promise, denial, mixed, missing data.
3. Varga reconciliation (section 7).
4. Lens policy and its table (section 8).
5. Key Inference page rewire, one panel at a time behind the existing page.
6. Verification on real charts, including GowriShankarC, against hand readings.

Triggers (`transit_event_triggers.md`) then group all 138 matters by this D1 promise, as decided.

**Decisions needed**
1. **Start with phases 1 and 2 in Core, tested, with no UI change?** My recommendation. The page stays as it is until the engine is trustworthy.
2. **Verdict rules in section 6:** are the thresholds (three supportive families for Strong positive, and so on) the right shape? They are the heuristic part and are labelled so.
3. **Birth-time reliability:** is there a field for it today? If not, D60 stays out until one exists.
4. **Lens seeds from your table**, tagged synthesis: acceptable?

## 10. As built: phases 1 and 2 (2026-10-04)

Code is in `src/Ikiastrro.Core/LifeMatters/Promise/`: `PromiseModel.cs` (the vocabulary, phase 1), `D1PromiseEngine.cs` and `PromiseFamilies.cs` (phase 2). Tests are `D1PromiseEngineTests` (38 cases on synthetic charts: Aries Lagna, career at the 10th). No page, repository or database change; nothing reads the engine yet.

The caller supplies a `MatterPromiseInput`: the target, each planet's sign, dignity, combustion, Ṣaḍbala band and nakshatra lord, the matter's karakas, the target's house-capacity, SAV and lord-BAV bands, the Argala and Virodhargala planets, and any yogas. The engine touches no database. `LifeMatterPromise` carries the verdict, confidence, positive and negative testimonies, contradictions, missing evidence, refinements, the dominant support and obstruction, and the old percent as `TechnicalSupportIndex`.

Where the build is more specific than sections 4 to 6, or differs:

- **Planets that are the lord or a karaka are read once.** Their placement from the target, dignity and conjunctions are their testimonies; an aspect they cast on the target is appended to the placement text, not counted as a separate influence (the same cause). `Influence:{planet}` rows exist only for other planets.
- **The Target row is neutral.** It carries the house's own capacity and no direction; the house's strength never creates a promise.
- **Verdict rules as implemented**, over families (principal = lord, karaka, target; PS and PO count principal supportive and obstructive families, S and O all families):
  1. Indeterminate: the lord is not placed, or no family has a direction.
  2. Strong positive: PS is at least 3, PO is 0, no obstructive lord family, and no principal supportive family is weak in capacity.
  3. Adverse: no principal support, at least one principal obstruction and more obstructive than supportive families; or at least two obstructive families outnumbering supportive ones with the lord obstructive.
  4. Weak or limited: no principal support but some indication elsewhere; or principal support exists and every supportive principal family is weak in capacity.
  5. Mixed: principal support and obstruction within one family of each other.
  6. Positive but conditional: principal support outnumbers principal obstruction.
- **Confidence on D1 alone is capped at Medium.** `D1Foundation.Confidence` can be High (lord, karakas and the other influences all agree), but `LifeMatterPromise.Confidence` shows Medium until a varga confirms. Low when the lord is missing, evidence is missing, or fewer than two role groups have a direction.
- **Nakshatra lord and dispositor are refinements only**, listed in `Refinements`, never a direction.
- **Combustion lowers capacity one step** and never flips direction; a weak planet with a good placement reads as a damaged promise (Positive but conditional), not as a negative.
- **A functional malefic standing in a target that is the 3rd, 6th, 8th or 12th from the Lagna reads supportive** (PVR 13.2: it spoils a house that should be spoiled). Applied only to occupation of the target.

Not yet: conjunction and aspect effects use functional nature only (no Mercury or Moon natural-nature refinement inside the engine); house-from-Lagna placement of the lord is not a second family; avastha families are not separate inputs. Phases 3 to 6 are unchanged.

### 10.1 Adapter and the real-chart check (2026-10-04)

`LifeMatterPromiseAdapter` and `LifeMatterPromiseService` (`src/Ikiastrro.Data/Statistics/LifeMatterPromiseAdapter.cs`) turn a matter and a person's persisted D1 facts into `MatterPromiseInput`s. They read the same `LifeMatterStatistics`, focus rules and Ṣaḍbala summary Key Inference reads (SAV, the lord's BAV, independent Bhava Bala and Argala bands; each planet's Ṣaḍbala band, dignity, combustion and nakshatra lord), so the two never disagree on a figure, and they add no rule. A matter is read once per Lagna house of its focus, in focus-priority order; no overall verdict is derived across a matter's houses yet. Matters with no Lagna house focus (Āruḍha and other lenses) return no target and a note, until phase 4. Yogas are not wired: the evaluation rows carry no planets or direction. 11 adapter tests are in `LifeMatterPromiseAdapterTests`.

Check it on a saved person from the CLI: `dotnet run --project src/Ikiastrro.Cli -- read-promise <name|id> [--matter CODE] [--category CAT] [--detail]`. It prints each matter's house, verdict, confidence, the lord, karaka and other-influence directions (L, K, I), the old percent, and with `--detail` every testimony.

First run, GowriShankarC: 106 of the 138 matters have reference steps (the other 32 are the karakatwa grid entries, which are not read). Of the 106, 10 have no Lagna house focus; the other 96 give 104 house readings (some matters have two houses), which came out Mixed 68, Positive but conditional 17, Adverse 14, Weak or limited 5. Most matters read Mixed. That is partly real, since a lord, its karakas and a house nearly always carry both support and affliction, and partly the heuristic: any principal family on each side within one of the other reads Mixed. Whether that is too coarse is a judgement for a hand reading, not for tuning blind.

One correction found by this run: planets acting on the target were judged by functional nature alone. PVR step 5 and the brief also weigh where the planet stands from the target, so an influence now combines nature and position (agree: that direction; one silent: the other; disagree: Mixed). A planet in the target itself has no position from it, so only its nature counts, and the bādhaka always obstructs. This is why a malefic aspecting from a quadrant no longer reads flatly obstructive.

### 10.2 The D1-Promise tab (2026-10-04)

Astro Facts has an eighth master tab, **D1-PROMISE** (`?step=promise`), showing the engine's output for every life matter before Key Inference is rewired: `D1PromisePanel` over `LifeMatterPromiseService.ReadAll`, loaded on first open. It is the place to compare the engine with hand readings. It is registered as a print module (`d1promise`) and adds no rule. Key Inference itself is unchanged.

### 10.3 Phase 3: varga reconciliation (2026-10-12)

`PromiseReconciler.Reconcile(LifeMatterPromise)` (`src/Ikiastrro.Core/LifeMatters/Promise/PromiseReconciler.cs`) applies the section 7 table to the D1 verdict and `Domain` (the varga read by `LifeMatterVargaConfirmationAdapter`). It returns a `PromiseReconciliation` (outcome, verdict, confidence, one-line inference). Pure Core; 26 cases in `PromiseReconcilerTests`. Nothing calls it yet: no page, CLI or `LifeMatterPromise` change.

The six rows in section 7 are implemented as written. The cells the table did not cover are my reading, heuristic and open to change: positive D1 with a mixed varga is `ConfirmedWithQualification`; mixed with mixed is `Unresolved`; mixed D1 with a weak varga reads as the adverse row; weak or adverse D1 with a mixed varga is `LimitedByD1`; weak with weak is `ThinPromise`; any other weak/adverse pair is `StrongObstruction`.

Rules enforced: the varga never raises the D1 verdict; only a Strong positive whose delivery is reduced steps down to Positive but conditional; an indeterminate D1 draws no inference; an unread or indeterminate varga leaves D1 standing. Confidence reaches High only when the outcome is Confirmed and the D1 foundation itself is High; every other outcome is capped at Medium.

Still to do: surface it (CLI `read-promise`, D1-Promise tab, then the Key Inference rewire, phase 5), and phases 4 and 6.

`read-promise` now reads the matter's varga and prints it with the reconciliation (`varga=D10:PositiveConditional → Confirmed`, a "Reconciled with varga" tally, and with `--detail` the reconciled verdict, confidence and inference). `LifeMatterPromiseService.ReadAll(id, includeVarga: true)` fills each target's `Domain`; the default stays D1-only. Matters read from D1 itself, or whose varga is not stored, show `varga=-` and `NotEvaluated`. Fixed on the way: the varga Argala build parsed non-graha rows (e.g. `PP`) as planets and threw; both this path and `LifeMatterTimingDataService` now pass graha rows only.

The Astro Facts **D1-PROMISE** tab now reads with `includeVarga: true` and shows the reconciliation: a badge on each row (`D10 · Delivery reduced`, tooltip = the inference) and in the opened row a "D10 reconciliation" line with the varga's own verdict and the reconciled verdict and confidence. Rows with no varga read say so. Sorting and the verdict filter are unchanged (still by D1 verdict).
