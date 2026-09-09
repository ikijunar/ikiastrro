---
last_updated: 2026-09-09
workstream: ui
status: canonical
togaf: B — experience principles
---

# Iki-Astrro brand guide

Canonical visual reference. The supplied main screen
(`reports/brand/ikiastrro-main-screen-reference.png`) is the source of truth for the shell,
colour relationships, typography, Navagraha/Ganesha composition and footer.

## Lockup

**Iki-Astrro | Where Passion, Purpose & Planets Align.**

- `Iki-Astrro` — sunset orange. The pipe — midnight blue. The tagline — midnight blue,
  standard interface typography (not a display-serif element).
- Do not replace the pipe with a decorative divider or change the tagline copy.

## Core palette

Interface colours use these tokens; illustration colours may vary within the artwork.

| Role | Token | Value |
|---|---|---|
| Warm canvas (background, quiet space) | `--brand-canvas` | `#FAF5EA` |
| Raised canvas (fields, rows, elevated) | `--brand-surface` | `#FFFDFC` |
| Midnight blue (headings, body, nav, action fills) | `--brand-midnight` | `#0F2041` |
| Sunset orange (brand name, borders, emphasis, action text) | `--brand-sunset` | `#F47A24` |
| Warm line (dividers) | `--brand-line` | `#D9D1C7` |
| Muted blue (placeholders, secondary metadata) | `--brand-muted` | `#53698D` |
| Soft peach (avatars, low-emphasis orange tint) | `--brand-peach` | `#FFF0E5` |

Semantic astrology colours (dignity, planets, dashas, evidence) are separate tokens, tuned
to stay readable on the warm canvas; they must not redefine the core palette.

## Typography

**Manrope** everywhere. Exactly three interface sizes — Display `clamp(38px, 3.05vw, 53px)`
("Discover Your Path"); Tagline `clamp(20px, 2vw, 37px)`; Control `19px` (nav, "Preferences").
Weight, spacing and colour carry the rest of the hierarchy. Tabular numerals for dates,
degrees, scores, periods.

## Main-screen copy

Primary heading **Discover Your Path** (no subheading). The form is the primary action area
with saved-chart search and rows directly below. Main action label **Generate Chart**.

## Actions

Primary: midnight-blue fill, sunset-orange text, sunset-orange border — never white text.
Selected nav may use the same treatment; secondary nav is midnight-blue text on the canvas.
Focus / hover / pressed states stay visibly distinct and meet accessible contrast.

## Preserved assets (do not redraw or reorder)

The Ganesha illustration; the Navagraha arrangement, orbital relationships and glow; the
mountain/footer artwork and the dedication footer:
**Dedicated to my guru, Sundari Hemachandran — By Ramakrishnan P.**

## Application rule

Every screen is a denser continuation of the main screen: warm, spacious, precise, with
midnight-blue information structure and sunset-orange emphasis. **MudBlazor** is the canonical
component system (light theme, shared tokens, `MudLayout` / `MudAppBar` / `MudMainContent`).
Chart rendering itself stays outside the MudBlazor restyling scope.
