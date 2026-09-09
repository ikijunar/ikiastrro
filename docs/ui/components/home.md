---
last_updated: 2026-09-09
workstream: ui
component: Home · Add
route: / · /add
togaf: C — component spec
---

# Component — home & entry

## Home (`/`)

The saved-person selection screen — the visual authority is the canonical main screen
([`../brand.md`](../brand.md)).

- Brand lockup + tagline in the shared header; nav Home · Preferences · Saved Charts (no
  duplicate nav row here).
- **Discover Your Path** heading, no subheading.
- A **Name** field that filters existing saved people (prefix match) and offers `+ Add new`.
- Selecting an existing person → details + Open Chart (`/charts/{id}`).
- The Ganesha / Navagraha illustration and the dedication footer are preserved.
- Fits above the fold at desktop widths.

## Add / Edit (`/add`) — **in progress (`FEAT-UI-03`)**

The birth-details entry form. `HandleSubmit` → `ChartGenerationService.GenerateAll` (compute
+ store all 21 charts + Vimśottari dasha), then navigate to the chart.

Current gaps:

- Renders as **unstyled native inputs** — not migrated to MudBlazor / shared tokens.
- **No Sex field**, though `tbl_BirthDetails.Sex` exists (migration 052) and yoga context
  requirements consume it.
- No geocoding-failure fallback (manual lat/long/offset) in the web form.
- Fields: Name, Date of Birth, Time of Birth, Place City, Place Country, `Generate Chart` CTA.

Target: MudBlazor form on the shared shell, Sex selector, geocode fallback, `Save Changes` on
the edit path.

## Preferences (`/preferences`) — **planned (`FEAT-UI-12`)**

Not built. First preference: an Ayanāṁśa selector whose default is the active
`tbl_Rule_Ayanamsa` system default, listing that plus the other catalogued options.
Preferences and Saved Charts share the nav treatment and dimensions.
