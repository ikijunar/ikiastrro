# ikiastrro — Roadmap

**Status:** living · **Created:** 2026-09-06 · **Owner:** rammyps

Theme-based **Now / Next / Later**, no fixed dates — this project ships in bursts, not sprints.
This file is the public view of direction; the authoritative feature-by-feature state is
[`PRODUCT.md`](PRODUCT.md), and the private prioritisation working doc is `../methods_prodmag.md`.

## How priority is decided

- **Opportunities** are filed as [Feature / opportunity](.github/ISSUE_TEMPLATE/01-feature-opportunity.yml) issues.
- Scored with **ICE** — Impact / Confidence / Ease, each 1–10, `Score = average`. Not RICE:
  at this user count "Reach" is a constant that only adds noise.
- Slotted into Now / Next / Later at triage. Re-scored after each shipped theme, not on a calendar.
- A feature becomes a `FEAT-<AREA>-<NN>` row and moves along the ladder
  `Planned → Designed → DB → Core → Verified → Web → Done` (checklist: DB · Core · Verify · Web · Docs).

## Now

Close the gap between verified engine logic and what the web app actually shows — the
"Missing Web" column in the `PRODUCT.md` rollup.

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

## Change log

- 2026-09-06 — Initial roadmap extracted from `PRODUCT.md` state and the `methods_prodmag.md`
  Now/Next/Later. "Now" set to UI-surfacing of verified engine features.
