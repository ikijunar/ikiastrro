# Bhava (House) Analysis Coverage — vedic_horo_gen

**Purpose:** Maps the classical 8-point Bhava-analysis checklist (strength/aspects/conjunctions of
the house lord, house strength, natural qualities, yogas, exaltation/debilitation, Navamsa
placement, subject context, sign-specific lord relations) against what `vedic_horo_gen` actually
computes and stores today, so future feature work can target the real gaps instead of re-deriving
them.
**As of:** 2026-08-30
**Source:** `memproj_vedic_horo_gen.md`, `ikiastrro.md`, `methods_prodmag.md`
(Opportunity Backlog).

The 8 points (as given):

1. Strength, aspects, conjunctions and location of the house lord.
2. The strength of the house itself.
3. Natural qualities of the house, its lord, and planets in/aspecting it.
4. Whether any yoga occurring in a house alters its influence.
5. Exaltation/debilitation of the house lords.
6. The situation of the house-lord (or the lord of the house it sits in) in the Navamsa.
7. The age, position, status and sex of the subject.
8. Sign-specific well/ill-disposed planets and the lord's relation to them (e.g. Sun for Aries
   natives).

---

## Scorecard

| # | Point | Status | What exists / what's missing |
|---|---|---|---|
| 1 | Location, aspects, conjunctions of house lord | 🟡 **Partially covered** | House lordship (`tbl_Chart_HouseLords`), conjunctions (`tbl_Chart_Conjunctions`) and aspects (`tbl_Chart_Aspects`) are all computed and stored generically for every planet — a house lord's location/conjunctions/aspects can be read off these tables. "Strength" in the formal sense (Shadbala) is **not** computed — ICE-scored but parked in the "Later" tier (ICE 3.7), never built. |
| 2 | Strength of the house itself (Bhava Bala) | ❌ **Not covered — and now confirmed blocked, not just unscoped** | No Bhava Bala or house-strength concept anywhere in the codebase or backlog — distinct from Shadbala (which scores planets, not houses). **2026-08-30**: mined `how-to-judje-a-horoscope-i_p1-312` (Raman) looking for sourcable data — the book explicitly defers Bhava Bala to its own separate companion volume, *Graha and Bhava Balas*, and contains none of the method itself. Now added to the Opportunity Backlog (`methods_prodmag.md`) as **Blocked**, not just missing. |
| 3 | Natural qualities of house/lord/occupants | 🟢 **Data exists; reference data now sourced and ready to seed** | The raw computed ingredients (sign, dignity, house-lordship, occupants, aspecting planets) were already there. **2026-08-30**: the missing classical layer — 12-house significations and Sthira Karaka role-per-planet — was found and extracted from `how-to-judje-a-horoscope-i_p1-312` (p.12-13, cross-validated against ~20 case studies). Migration 030 designed and ready in `house-lagna-significations.md`; added to the Opportunity Backlog pending ICE scoring. A synthesis layer that actually assembles this into a per-house reading is still a separate, later step. |
| 4 | Yogas altering house influence | ❌ **Not covered, but no longer unscoped — now has real source material** | "Yoga detection (Raj yogas, doshas, etc.)" was named as a competitive differentiator on day one (`res_horoscopetool.md`) but never carried into `methods_prodmag.md`'s Opportunity Backlog until now. **2026-08-30**: the book pass found substantial usable material — Rajayoga formation rules, the Yogakaraka concept, several named yogas (Chandramangala, Gajakesari, Papakarthari/Subhakarthari), and a stated 12° orb rule — but as case-study prose, not ready-to-seed rows. Added to the backlog, flagged as needing its own scoped design session before building (largest remaining gap by source depth). |
| 5 | Exaltation/debilitation of house lords | ✅ **Covered** | `ClassicalDignity.cs` computes full sign-dignity (exalted/debilitated/own/moolatrikona/friend/enemy) for every planet, including whichever planet is a given house's lord — this is generic, chart-type-agnostic, and shipped since the SwissEphNet rebuild (2026-08-24). |
| 6 | House-lord's situation in the Navamsa | 🟡 **Structurally covered, precision gap open** | D9 gets the same full analytics (dignity/lordship/conjunctions/aspects) as D1 via the shared `ChartAnalyzer` — so a lord's D9 sign/house placement is available. **2026-08-30**: D2 (Hora), D6 (Shashtamsa), D10 (Dasamsa), D11 (Rudramsa) were added at DB/CLI level with the same shared analytics + nakshatra linkage, so a house-lord's placement can now be cross-read in the life-area varga too (D6 health, D2/D11 wealth, D10 career). But D9 (and every varga) **within-sign degree is still not computed** (top "Now" item in the ICE backlog, ICE 7.7, not built) — that's what blocks judging *how* favourable a varga placement is (Vargottama needs the sub-degree) and how tight a same-house varga conjunction is. Computation gap, not a sourcing gap. |
| 7 | Age, position, status, sex of subject | 🟡 **Age covered; position/status/sex not modeled, and still not sourced** | Age is directly wired in via Vimshottari Dasha + the life-in-weeks grid (age-relative, `tbl_Dim_LifeCalendar`) — timing analysis is tied to the subject's actual age. `tbl_BirthDetails` stores only name/date/time/place; there's no gender, social status, or position field, and nothing in the calculation layer conditions on them. **2026-08-30**: searched `how-to-judje-a-horoscope-i_p1-312` for rule material — only the checklist line itself surfaced, no consolidated rule section found; not actionable from this source without a deeper pass or a different reference. |
| 8 | Sign-specific well/ill-disposed planets (e.g. Sun for Aries; lord-to-Sun relations) | 🟢 **Covered by the computed classifier** | `ClassicalRelationships.cs` computes generic planet-to-planet friend/enemy/neutral relations; `Core/Calculators/LagnaFunctionalNature.cs` computes functional benefic/malefic/neutral/yogakaraka per Lagna from house-lordship (shown in the web D1 planet table, checked by CLI `verify-functional-nature`). **2026-08-30**: the named per-Lagna layer from `how-to-judje-a-horoscope-i_p1-312` (p.16-18) was seeded as a cross-check table `tbl_Dim_LagnaFunctionalNature` (migration 031); **2026-08-31 that mirror was removed in full** — the computed classifier stands on its own. |

**Net (updated 2026-08-30): 3 of 8 fully covered or sourced-and-ready (3, 5, 8), 3 partially
covered (1, 6, 7) — infrastructure exists but synthesis or subject-context data is still missing,
2 not covered and not yet buildable (2, 4) — Bhava Bala is genuinely blocked on a different
source, Yoga detection has real material but needs its own scoped design pass before it can be
built.**

---

## What this means for the backlog

The app is strong at **computing discrete planetary facts** (position, dignity, retrograde,
combustion, nakshatra lord, house-lordship, conjunction, aspect) for D1 and D9 alike — that's the
`ChartAnalyzer` layer, and it's solid. Point 8 is covered by the computed `LagnaFunctionalNature`
classifier (its Raman cross-check table, migration 031, was removed 2026-08-31). Point 3 is
sourced and design-ready (migration 030, `house-lagna-significations.md`) — the remaining work
there is build, not discovery. What's still missing across the rest is the next layer up: **synthesizing
computed facts + classical reference data into house-level judgments**, and two structural gaps
that were invisible to the backlog until 2026-08-30:

1. **Yoga detection (point 4)** — named as a competitive gap since the project's original scope
   doc, dropped when the Opportunity Backlog was first built, now added with real book-sourced
   material behind it (Rajayoga rules, Yogakaraka, several named yogas, a 12° orb rule) — but
   flagged as needing its own scoped design pass, not a same-batch reference-table add.
2. **Bhava Bala (point 2)** — distinct from Shadbala (already a scoped-but-unbuilt "Later" item).
   Now confirmed **blocked**: the sourced extract explicitly defers this to a separate book, so it
   can't be scored or built until that source (or an equivalent) is found.

Points 1 and 6 are cases where the *infrastructure* is already chart-type-generic and mostly
there — closing them is a matter of adding a specific named feature (Shadbala for 1, D9 sub-degree
for 6 — already top of the backlog), not new architecture or new sourcing.

## Suggested next action

All 4 newly-surfaced opportunities (House+Planet Significations, Lagna Functional Nature, Yoga
detection, Bhava Bala) are now in the Opportunity Backlog in `methods_prodmag.md`, deliberately
left un-ICE-scored per rammyps's explicit call to prioritize as its own separate step — run that
scoring pass next, alongside the still-standing D9 sub-degree item, before deciding what to build.
