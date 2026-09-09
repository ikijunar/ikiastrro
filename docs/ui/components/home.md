---
last_updated: 2026-09-09
workstream: ui
component: Home
route: /
togaf: C — component spec
---

# Component — home & entry

In `wkstream_UI_v2` the Home page absorbs **Preferences** and **Add person** — there is no
separate `/preferences` or `/add` route. One page, two states.

## Screen 1 — Home (select / search)

Shell: MudBlazor `MudLayout` / `MudAppBar` with the brand lockup + tagline
([`../brand.md`](../brand.md)). The canonical main screen is the visual authority.

Layout is two columns on the warm canvas — controls left, art right — plus the dedication
footer. The Ganesha / Navagraha illustration is a **first-class part of the Home layout**
(right column), not tied to any toggle.

- **Preferences — top-left of the content area.** A collapsed disclosure (`MudCollapse` behind
  a text button). Expands **in place on Home**; nothing navigates. Contents:
  1. **Choose Ayanāṁśa** — `MudSelect` over `AyanamsaDefinition.Catalog`; default is the active
     `tbl_Rule_Ayanamsa` row, shown as *Default (Lahiri)*. The choice is passed to
     `ChartGenerationService.GenerateAll(birth, ayanamsa)` for the next generation.
  2. **Choose Chart Type** — `MudSelect`: *South Indian* (default) · *North Indian*. Extensible
     — more styles added when built. North Indian is selectable but its renderer is a later
     feature; until then it falls back to South Indian.
  Persistence: per-browser (`localStorage`) for now; a DB-backed default is a `database`-workstream
  follow-up.
- **Discover Your Path** — heading (`--font-size-display`), no subheading.
- **Name — searchable.** `MudAutocomplete` over saved-people names.
  - Typing filters saved people (contains match).
  - Selecting a person → `/charts/{id}` (the person hub).
  - **When nothing matches, the last option is `Add New`.** Choosing it opens Screen 2.

## Screen 2 — Home (Add New expanded)

Choosing `Add New` unhides the entry fields inline (below the Name field); the person list is
replaced by the form. Fields, in order:

| Field | Control | Notes |
|---|---|---|
| Name | `MudTextField` | required |
| Sex | `MudSelect` (option box) | Male · Female |
| Date of Birth | `MudDatePicker` | required |
| Time of Birth | `MudTimePicker` | **required for the Lagna** — kept though not in the shorthand field list; confirm |
| City | `MudTextField` | required; feeds `IPlaceResolver` |
| Country | `MudTextField` | required; **the last step** |

**Flow:** completing **Country** is the trigger — on a valid form it resolves the place,
runs `ChartGenerationService.GenerateAll` (with the chosen ayanāṁśa), and navigates to
**`/transit-wheel/{id}`**. A `Generate Chart` button is the explicit / accessible fallback.

Geocoding-failure fallback (manual lat / long / offset) is a follow-up, not in the first cut.

## Design system

Per [`../wkstream_UI_v2.md`](../wkstream_UI_v2.md): Manrope only; the three size tokens
`--font-size-display` / `--font-size-tagline` / `--font-size-control` — nothing else;
**sunset orange (`--brand-sunset`) is the button background** and the active/hover/focus
accent; midnight blue for text and structure; tokens only, no raw hex or px literals.
`Home.razor.css` is rewritten from scratch against these rules (the v1 file is the worst
offender — stacked override blocks, raw px, `!important` MudBlazor patches).

## Retired from v1

`+ Add new` as a route jump; the `/?preferences=1` query-string panel; the native `<select>` /
`<input>` / `<details>` markup; the `landing-nav` custom nav (→ `MudAppBar`).
