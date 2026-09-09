# ikiastrro — Roadmap

**Owner:** rammyps

The **flow** view: theme-based **Now / Next / Later**, pure flow — no sprints, no named
increments, no fixed dates. Work moves continuously; something ships when it's ready.
Feature-by-feature state is [`masterproduct.md`](masterproduct.md); the private
prioritisation working doc is `../methods_prodmag.md`. State vs flow: `STANDARDS.md` §E.1.

## How priority is decided

- **Opportunities** are filed as [Feature / opportunity](.github/ISSUE_TEMPLATE/01-feature-opportunity.yml) issues.
- Scored with **ICE** — Impact / Confidence / Ease, each 1–10, `Score = average`. Not RICE:
  at this user count "Reach" is a constant that only adds noise.
- Slotted into Now / Next / Later at triage; re-scored after each ship, not on a calendar.
- Each Now / Next item is a GitHub **Milestone** (an epic) and a `FEAT-<AREA>-<NN>` row in
  `masterproduct.md`, worked as issues on the flow board and closed along the ladder
  `Planned → Designed → DB → Core → Verified → Web → Done`.

## Velocity

Derived, never hand-tracked: **rolling 4-week merged-PR average**, plus feature-boxes
closed per week (diff the `masterproduct.md` rollup over git). Read on demand:

```
gh pr list --state merged --search "merged:>=$(date -d '-28 days' +%F)" --json number | jq length
```

## Now

Close the gap between verified engine logic and what the web app actually shows — the
"Missing Web" column in the `masterproduct.md` rollup.

- **Divisional charts in the UI** — render D2–D60 (21 varga types), not just D1/D9 · `FEAT-VARGA-01`
- **Jaimini chara karakas panel** — surface the 8-fold Aṣṭa already computed · `FEAT-KARAKA-01`
- **Planetary-state (avastha) display** — `AgeState`, `WakefulnessState` · `FEAT-AVASTHA-01/02`
- **Slow-planet transit history view** — 1930–2060 sign-transit timeline · `FEAT-TRANSIT-01`

## Next

Scoped, not started. Ordering set at the next ICE pass.

- **Bhāva significations + Sthira Kāraka mapping** — Designed; migration 030 drafted · `FEAT-HOUSE-03`
- **Sthira Kāraka / Naisargika Kāraka** — resolve Sapta vs Aṣṭa; needs a cited edition · `FEAT-KARAKA-03/04`
- **Dispositor chains / final dispositor / mutual reception** · `FEAT-DISPOSITOR-01`
- **Compound Maitrī, argala, sambandha** · `FEAT-RELATIONSHIP-04`

## Later

Acknowledged, deliberately deferred.

- **Strength engine** — Ṣaḍbala (6 components), Vimśopaka Bala, Bhāva Bala. Blocked on sourcing a
  cited reference edition · `FEAT-STRENGTH-01/02`
- **Yoga detection** — Pañcha Mahāpuruṣa + Rāja/Dhana first slice. Needs its own design pass to turn
  case-study prose into enumerable rule rows · `FEAT-YOGA-01`
- **Remaining avasthas** — `RadianceState`, `ShameState`, `PostureState`; each needs a cited edition
  and janma-ghaṭi inputs · `FEAT-AVASTHA-03/04/05`
- **Selectable house system** — beyond whole-sign
- **KP system** — sub-lords, significators as a layered sub-system

## Cadence

One dated line per ship event (a `git tag` + GitHub Release). Capped at the last ~6 —
`git log --first-parent origin/master` and the Releases page hold the rest. This is the
only time-phased block in the repo's prose (`STANDARDS.md` §E.1 WORKSTREAM-05).

- 2026-09-06 — Roadmap set to pure-flow Now/Next/Later; "Now" = UI-surfacing of verified
  engine features.
