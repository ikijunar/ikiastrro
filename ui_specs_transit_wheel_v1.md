---
title: Transit Wheel UI Specification
version: 1.1
status: finalized-for-implementation
last_updated: 2026-09-07
---

# Transit Wheel UI Specification v1.1

The supplied transparent SVG is the visual authority. Preserve its ring order, artwork, transparency, colors, and proportions. Do not add legends, cards, or external panels.

## Centre status

Use one standard centre layout regardless of which body was moved:

```text
Date: 07 September 2026 · 09:18 IST
Nakshatra: Shatabhisha · Pada 3
Saturn (D) · Jupiter (R)
Saturn (D) · Ar · Jupiter (R) · Aq
```

The sign abbreviations are fixed: Ar, Ta, Ge, Cn, Le, Vi, Li, Sc, Sg, Cp, Aq, Pi. The initial date defaults to the current IST date. After a movement is committed, replace the default entry date with the solved movement timestamp and do not reset it.

## Fixed natal layer

The selected user chart supplies the fixed whole-sign D1 ring. The Ramakrishnan test chart includes Aries Ascendant, Sun/Mars/Mercury/Venus in Aries, Moon in Scorpio, Jupiter/Saturn in Virgo, Rahu in Cancer, Ketu in Capricorn, and natal Gulika/Mandi in Libra. Natal Gulika and Mandi remain fixed sensitive reference points; daily recalculation is excluded from the primary wheel.

## Transit layer

Saturn, Jupiter, Rahu, and Ketu are movement-analysis bodies. Mars, Venus, and Moon are shown at the selected timestamp only and do not receive movement tails or drag solving. Ketu is derived from Rahu plus 180 degrees.

The movement window is the selected date through the following 36 months, including direct and retrograde motion. Tails follow sampled ephemeris positions and stop only at defined nakshatra boundaries.

## Nakshatra and pada

Render 27 sidereal nakshatras and 108 pada divisions inside the SVG viewport. Aries 0 degrees is placed at the top using the natal Ascendant rotation. Nakshatra boundaries remain astronomically fixed. Drag commits snap to nakshatra starts; a nakshatra start is Pada 1. The current position may display its actual pada.

## Drag solving

Only one transit body is actively draggable at a time. Dragging converts the pointer angle to a sidereal longitude, snaps to a nakshatra start, and searches the three-year ephemeris window for the earliest matching occurrence. Retrograde/direct status is retained. If a source rule requires direct motion, choose the earliest direct occurrence instead. At the solved time, recalculate all transit bodies and evaluate the active dasha.

Gulika and Mandi are draggable only as fixed natal reference markers for inspection; they do not use daily movement solving in the primary mode.

## Dasha selection

Show two levels at minimum: Mahadasha and Antardasha. The selected dasha period becomes the active analysis timestamp/window. Selecting a dasha must update the transit positions to that period and must not reset to today or to the previous entry date. The user may then drag a transit body to a prior or forthcoming nakshatra movement within that dasha period.

```js
const dashaState = {
  levels: ["Maha", "Antar"],
  selectedPeriodId: null,
  selectedStartUtc: null,
  selectedEndUtc: null,
  keepSelectionAfterTransitDrag: true
};
```

## Time and persistence

Store calculations internally in UTC and display all dates and times in Indian Standard Time (`Asia/Kolkata`). Persist exact snapshots in `dbo.tbl_TransitPositionReference`, including longitude, sign, degree, motion, nakshatra, pada, ayanamsa, and timestamp.

## UI acceptance criteria

- The complete chart is visible in one desktop viewport fold.
- The wheel remains fully square at all supported widths.
- The chart is responsive without distorting the circle.
- Fonts and planet icons do not overlap.
- Nakshatra and pada labels remain readable at desktop size.
- Centre date, nakshatra/pada, motion, and sign rows are readable.
- Saturn and Jupiter show motion plus sign, for example `Saturn (D) · Ar` and `Jupiter (R) · Aq`.
- Selecting a dasha updates all transit placements to that period.
- Selecting a dasha does not reset the chart selection.
- Dragging a planet preserves the selected dasha context.
- Natal placements, Gulika, and Mandi remain fixed while transit layers update.

## v1.2 amendment (2026-09-07)

- Keep the transit date selector directly below the Natal ↔ Transit heading.
- Add Mahā-daśā and Antar-daśā selectors beside the date; selecting either updates the main wheel and preserves the selected state.
- Render natal planet, Gulika, and Mandi labels as compact dark-blue abbreviations. Draw dark-blue separators for all 12 natal sign sectors and show house numbers 1–12 from the selected Lagna (Ramakrishnan: Aries is house 1).
- Show two-letter sign abbreviations (for example Ar) and two-letter nakshatra abbreviations, alternating dark-blue and yellow.
- Show each transit planet sign and degree outside the wheel. Date, dasha, and Saturn/Jupiter/Rahu/Ketu drag are movement inputs; Mars/Venus/Moon remain selected-date display-only.
- Add a left-side comparison table with natal planet/sign/degree and selected-date transit planet/sign/degree.
- Record the final date hide/reveal transition in implementation notes after interaction verification.

### v1.2 clarification

The date selector remains visible directly below the heading alongside Mahā-daśā and Antar-daśā selectors. It is not hidden after selection; the centre always shows the active date in IST. Planet dragging and selector changes update the same active state.

### v1.2 implementation note

Dasha selectors currently drive a deterministic preview date so their effect is visible while the period-calendar integration is completed. Venus-Rahu preserves the entered date; other selections resolve to a repeatable offset and are labelled by the selected dasha.

### v1.3 visual and natal-data correction (2026-09-07)

- Ramakrishnan D1 natal degrees are sourced from the JHora export: Sun 8°12′ Aries, Moon 7°17′ Scorpio, Mars 3°57′ Aries, Mercury 1°50′ Aries, Jupiter(R) 8°43′ Virgo, Venus 11°59′ Aries, Saturn(R) 10°57′ Virgo, Rahu 13°03′ Cancer, Ketu 13°03′ Capricorn, Gulika 7°44′ Libra, Maandi 18°07′ Libra.
- The natal ring uses a high-contrast yellow band with placements readable above the dark centre. House numbers remain centered in each whole-sign sector from Aries Lagna.
- The desktop wheel and left rail fit one viewport fold; the left rail has no internal scrollbar. Sign labels, nakshatra labels, and transit degree labels use larger accessible sizes. Nakshatra segments alternate dark-blue/yellow with contrasting text.

### v1.4 template update

The supplied ikiastrro-cancer-asc-natal-transit.svg is now the active visual template. The surrounding page and chart canvas use the warm yellow natal background family rather than white.

### v1.5 saved-chart and theme behavior

- / opens the transit wheel for the first saved chart; /transit-wheel/{Id} binds the wheel to a saved chart identity.
- The transit UI uses the supplied dark-blue and yellow palette for page, controls, labels, and emphasis.

### Dynamic movement implementation

The supplied SVG now exposes stable Saturn, Jupiter, Rahu, and Ketu groups. Date and dasha changes apply coordinate-delta transforms to those groups while preserving the SVG artwork and ring geometry.
