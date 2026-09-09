---
last_updated: 2026-09-09
workstream: ui
component: TransitWheel
route: /transit-wheel · /transit-wheel/{id}
togaf: C — component spec
---

# Component — transit wheel

Natal ↔ transit comparison over a supplied transparent SVG template
(`UI_SVG_Templates/…natal-transit.svg`). The SVG is the visual authority: preserve its ring
order, artwork, transparency, colours and proportions. No legends, cards or external panels
beyond the left comparison rail.

## Layout

- **Heading** "Natal ↔ Transit", with the **transit date selector** directly below it, plus
  **Mahādaśā** and **Antardaśā** selectors beside the date.
- **Wheel** — stays fully square at every width; responsive without distorting the circle.
  Aries 0° at the top, rotated to the natal Ascendant. 27 nakṣatras + 108 padas rendered
  inside the viewport, boundaries astronomically fixed, segments alternating dark-blue / yellow.
- **Centre status** (one layout regardless of what moved):
  ```
  Date: 09 September 2026 · 00:00 IST
  Nakshatra: Shatabhisha · Pada 3
  Saturn (D) · Jupiter (R)
  Saturn (D) · Ar · Jupiter (R) · Aq
  ```
  Sign abbreviations fixed: Ar Ta Ge Cn Le Vi Li Sc Sg Cp Aq Pi.
- **Left comparison rail** — natal planet / sign / degree and selected-date transit
  planet / sign / degree; no internal scrollbar; fits one desktop fold with the wheel.

## Natal layer (fixed)

The selected saved person's **D1** `ChartResult` + `ChartKeyDetails` supply the fixed
whole-sign ring — planet abbreviations and house numbers 1–12 from the stored Ascendant
(no hardcoded chart identity). Natal Gulika and Maandi are shown as fixed compact dark-blue
markers; they are not daily-recalculated in the primary mode.

## Transit layer

- **Movement bodies:** Saturn, Jupiter, Rahu, Ketu (Ketu = Rahu + 180°). Driven by
  Gochara snapshots + coordinate-delta transforms on stable SVG groups.
- **Display-only:** Moon, Mars, Venus — Swiss sidereal position at the active instant, in the
  comparison table and outer annotation. No tails, no movement window, no drag solving.
- Movement window: the selected date through the following 36 months, direct + retrograde.

## Interaction

- **Date + dasha selectors are the movement inputs.** Selecting a dasha sets the active
  analysis instant to that period's real saved start time and updates every transit body —
  it must **not** reset to today or the previous date, and must **not** reset the chosen
  saved chart. Selectors stay visible; they are not hidden after selection.
- **Pointer drag is rolled back** — Saturn/Jupiter/Rahu/Ketu groups were pointer-draggable to
  solve the nearest movement date, but it caused browser lifecycle / shutdown issues and was
  removed. Re-add only behind a fix for that.

## Time & persistence

Calculations internal in UTC, displayed in IST (`Asia/Kolkata`). Exact snapshots persist to
`tbl_TransitPositionReference` (longitude, sign, degree, motion, nakṣatra, pada, ayanāṁśa,
timestamp).

## Acceptance

Whole chart in one desktop fold · wheel square at all widths · fonts/icons don't overlap ·
nakṣatra/pada labels readable at desktop size · Saturn/Jupiter show motion + sign · dasha
selection updates all placements and preserves the chart selection · natal + Gulika + Maandi
fixed while transit layers update.

## Open

Outer-label crowding at conjunctions; small mobile SVG text.
