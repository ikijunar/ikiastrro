---
last_updated: 2026-10-18
workstream: ui
togaf: C — UI standards
status: first slice built at /af-v10/{id}; classic tabbed page untouched at /astro-facts/{id}
---

# AF-v10 — one connected screen

rammyps's brief (2026-10-17): a separate AF-v10 page (AF-v10) with a unified design, fewer tab clicks, a more connected
visual representation instead of text, and a modernised set of Vedic design elements.

## 1. Why a separate page

The classic page (`/astro-facts/{id}`) is ten master tabs with sub-tabs, about 1,300 lines, and a fact in one tab cannot be
seen from another. v10 keeps it untouched and builds beside it, reusing the engines and the chart components, so the two can
be compared and the classic page keeps working while v10 grows.

## 2. Research: what is known about Vedic chart UI

Formal design guidance for Vedic software is thin. The searches found developer tutorials, library docs and app listings,
not usability studies, so the points below are practical conventions, not standards.

- **Chart forms.** North Indian: a diamond, houses fixed, signs rotate. South Indian: a 4×4 grid, signs fixed, planets
  move; described as the easiest to read for beginners. East Indian is a third form. Apps commonly let the user switch
  between forms, and one award-winning Panchang project used the same planet illustrations in both forms to keep them
  visually consistent. *This app already uses the South Indian grid everywhere; v10 keeps it as the single chart.*
- **Interaction conventions in chart libraries.** Tap a planet for details; zoom and pan; aspect lines; retrograde markers;
  lagna highlight; automatic vertical stacking when several planets share a house; a toggle between glyphs and
  abbreviations; Rahu and Ketu kept exactly 180° apart; export as image or PDF.
- **Audience.** One guide recommends progressive disclosure: a simple view by default and an "advanced details" layer for
  technical users.
- **Desktop tools** (Jagannatha Hora, Parashara's Light and similar) are valued for accuracy and breadth; the reviews found
  say little about interface, and nothing found criticises or praises layout. Take their strength (depth of facts) and
  not their layout model (many windows and tabs).

Coordinated-views research (information visualisation) is better documented and is the basis for the new design:

- **Brushing and linking.** Selecting items in one view highlights the same or related items in the others. It is the most
  common coordination mechanism; it works best when the user can *see* how views are linked.
- **Overview + detail, small multiples, multiform dashboards, overlays.** The five standard multiple-view patterns. v10 uses
  small multiples (12 house tiles, 9 planet cards in one shape), overview + detail (rails + summary), and an overlay (arrows
  on the chart).
- **Two strengths of linking.** A light *highlight* (dim what does not match) and a full *crossfilter* (re-aggregate the other
  views). v10 uses highlight only: the selection never hides data.
- **Known challenges.** Linking views that hold different data, initial state, and showing how the views are linked. v10
  answers with one shared selection, a visible crumb, and a hint line that says what to pick.

Sources: [Roxy: Complete Kundli app features](https://roxyapi.com/blogs/complete-kundli-app-vedic-astrology-birth-chart-software),
[Roxy: Rashi chart visualizer](https://roxyapi.com/blogs/rashi-chart-visualizer-react-native),
[vedic-astrology-chart (SolidJS)](https://cdn.jsdelivr.net/npm/vedic-astrology-chart-solid@1.2.3/README.md),
[Visualising Panchang (Adobe Design Achievement Awards 2019)](https://rajeshx66.myportfolio.com/visualising-panchang-from-vedic-astrology),
[ApexCharts: Linked views and crossfilter](https://apexcharts.com/docs/linked-views/),
[Multiple and Coordinated Views in Information Visualization](https://www.medien.ifi.lmu.de/lehre/ws0809/hs/docs/scherr.pdf),
[Making Data Visual, ch. 6: Multiple and Coordinated Views](https://www.oreilly.com/library/view/making-data-visual/9781491960493/ch06.html).

## 3. Principles

1. **One chart, one selection.** The chart is pinned. Everything else is a view of it. Picking a house, a planet or a lord sets
   one selection; the chart overlay, the summary, the rails and the crumb all follow.
2. **See first, read second.** Each fact has a small visual form (meter, ring, chain, arrow) and a short word beside it.
   Paragraphs appear only inside the summary and behind a "details" fold.
3. **No hidden state in tabs.** The first screen shows all 12 houses and all 9 planets at once; a pick opens detail *in place*
   at the top right of the chart, so it is read without scrolling.
4. **Colour is a scan aid, never the only signal.** Benefic/malefic is colour *plus* a word; verdicts are colour plus pattern
   plus text; alertness is ring thickness and style plus a code. (Existing rule in `dataviz.md`.)
5. **Count each fact once.** A dignity, aspect or conjunction appears in one place (the avastha reason, or the nature chip),
   never repeated beside a state that already contains it (`docs/research/domain/argala-avastha-linkage.md` §3).
6. **Selections survive.** Changing the chart dropdown keeps the picked house or planet and rebuilds it from the new varga.
7. **Tokens only; all three themes.** No raw colours; works in Default Light, Cosmic Light and Cosmic Dark.
8. **Read-only and honest.** Nothing is stored. Anything this app synthesises (rather than takes from PVR) is labelled.

## 4. Anatomy

```
┌ ribbon: Lagna · Moon+nakshatra · chart domain · [Chart ▾] · crumb · Clear ─────────────┐
├───────────────────────┬───────────────────────────────────────────────────────────────┤
│ pinned South-Indian   │ SUMMARY of the selection (top right of the chart)             │
│ chart                 │   house: winner badge, force meter, meaning, lord chain,      │
│  · target outlined    │          the argala/virodhargala pairs, causes (fold)         │
│  · argala arrows      │   planet: its lords, every state and how it was reached,      │
│  · alertness tags     │          its part in argala                                    │
│  · blocked bars       │ HOUSES  12 tiles (number · sign · force meter · winner)       │
│                       │ PLANETS 9 cards (glyph ring · place · nature · alertness)     │
└───────────────────────┴───────────────────────────────────────────────────────────────┘
```

### Where the ten classic tabs go

| Classic master tab | In v10 |
|---|---|
| 1 About (general / planets / houses / relationships) | Ribbon, planet cards, house tiles, summary |
| 2 Transit, 3 Strength, 4 Karakas / Spl Lagnas, 5 Yogas | Later zones: a *Strength strip* under the rails, a *Timing band* above the ribbon; yogas as chips linked to the planets that form them |
| 6 Vargas, 7 All Charts | The chart dropdown (selection survives) and a small-multiples row of vargas |
| 8 Promise, 9 Phalita dasa, 10 Dasha matters | A *Timing band* (dasha lords as chips; picking a lord selects the planet) and the life-matter lens as an overlay on the chart |

## 5. Design elements (the new vocabulary)

All are small, single-purpose components using tokens only. Each carries its words and a tooltip.

| Element | What it shows | Component |
|---|---|---|
| **Bhava tile** | A house as an instrument: number, sign, argala-vs-virodhargala meter, the winner in words, the score | `BhavaTile` |
| **Planet card with alertness ring** | A graha: glyph in a ring (heavy = awake, dashed = dreaming, dotted = asleep), place, dignity, functional nature as an edge colour and a word, damaging-state mark | `PlanetCard` |
| **Lord chain** | Planet → sign lord → star lord → pada lord, each chip showing its nature in three separate parts (functional colour, natural ben/mal, houses owned); every chip is a button that selects that lord | `LordChain` |
| **Force meter** | The argala share against the virodhargala share as one bar, hatched for the opposing force | in `BhavaTile` and the summary |
| **Argala overlay** | Arrows from each argala sign into the target, thickness by how much the strongest planet can deliver, line pattern by verdict, blocked bars for obstructing signs, alertness tags in the cells | `SindUniDtlGrid` `Argala` parameter |
| **Selection crumb** | The current pick in words, with Clear | page ribbon |

### Vedic visual motifs: ideas, not yet built

These come from the tradition's own forms; none of the searches found a source for them, so they are proposals.

- **Graha colour identity**, separate from benefic/malefic colour. The nine Vimshottari lord colours already exist
  (`DashaLordColors`); a thin identity stripe on a card or chip, so a planet is recognisable across the page. Benefic/malefic
  stays on the edge and the word, so the two meanings never share a channel.
- **Nakshatra wheel**: a 27-segment ring, the pointer at the Moon and each planet, segments tinted by star lord. This would
  make star lord and pada visible at a glance and is where a pada lord would be drawn.
- **Dasha ribbon**: the Vimshottari periods as one horizontal band with the current maha/antar highlighted, above the ribbon;
  picking a period selects its lord.
- **Mandala frame** for the summary card: a restrained circular motif behind the title (the North Indian diamond is itself a
  yantra form). Decorative only; it must never carry data.
- **Glyph and name toggle** and automatic vertical stacking in crowded cells (library convention above).

## 6. Built in the first slice (2026-10-17)

`/af-v10/{id}`: ribbon, pinned chart with the argala overlay, summary (house: winner, meter, meaning, house lord
chain, pairs you can pick, causes; planet: lords, states with reasons, damage results, part in argala), 12 house tiles,
9 planet cards, chart dropdown that keeps the selection. Backed by `ArgalaForceModel`, `AvasthaLive`, `PlanetLordTriad`
and `LordNature`. Tests: `AstroV10ComponentTests`, `PlanetLordTriadTests`, `ArgalaForceModelTests`.

## 6a. Strength layer (2026-10-18)

Slices 2 (strength), 4 (yoga chips) and the nakshatra part of 5 are built, in `StrengthLayer` (a pure reshaping of rows the classic
page reads; cut-offs are `StrengthBands`, so a planet never gets two labels).
- **House tile**: SAV bindus of its sign and Bhava Bala (D1 only), each a pill, dashed when weak.
- **Planet card**: nakshatra and pada (from the true longitude, same in every varga), Ṣaḍbala as a bar of 0-150% of the required minimum
  with a tick at 100% (hatched when weak), and the planet's own Bhinnāṣṭakavarga bindus in its sign.
- **Yogas**: one chip per present yoga (deduped by code, good first, 12 shown and the rest folded). A pick lights its planets on the
  cards and opens the rule, inference and delivery (weakest planet's %). Planet and house summaries list the yogas they take part in.
- Ṣaḍbala and Bhava Bala are natal facts; on a varga they stay D1 values, Ashtakavarga and yogas follow the chart on screen.
- Still open: sign-level readout beyond SAV, nakshatra wheel, chart cells selecting houses/planets, yoga planets highlighted on the chart itself.

## 7. Next slices

1. **Linked chart both ways**: click a chart cell to select the house or planet; highlight a pick's lords on the chart.
2. **Strength strip**: Ṣaḍbala, Ashtakavarga and Bhava Bala as thin bars under the rails, linked to the same selection.
3. **Timing band**: the dasha ribbon and transit chips.
4. **Yoga chips** that light the planets forming them.
5. **Nakshatra wheel** and the star/pada lord layer.
6. **Summary card polish**: the house-nature × argala matrix and the PVR-audited house-lord text (blocked on the PVR audit).
7. **Phone layout**: the chart collapses to a sticky mini-chart; the summary becomes a bottom sheet.
8. **Print/export**: the selection summary as an image or PDF.

## 8. Open questions

- Should v10 eventually replace the classic page, or stay a second view? (Today it is additive; nothing links *to* v10 from the
  main navigation yet.)
- Which zone comes next: strength, timing or yogas?
- Is the nine-colour planet identity stripe wanted, given the risk of too many colours beside the benefic/malefic edge?
