---
last_updated: 2026-09-09
---

# Home screen requirements

- Use the Iki-Astrro brand guide as the visual source of truth: Manrope typography, warm canvas `#FAF5EA`, midnight blue `#0F2041`, sunset orange `#F47A24`, warm line `#D9D1C7`, muted blue `#53698D`, and raised surface `#FFFDFC`.
- The navigation replaces Home with Preferences. Preferences and Charts must have equal dimensions and use the same navigation treatment.
- Preferences opens an Ayanamsa setting. The first preference is an Ayanamsa choice box whose selected value is the active system default from `tbl_Rule_Ayanamsa`; the catalog contains that default plus the 20 other available options.
- The Name field filters existing people and offers `+ Add new name`.
- Selecting an existing person shows details, `Edit details`, and `Open Chart`.
- Adding a name reveals Name, Date of Birth, Time of Birth, City, and Country, with a `Create Chart` CTA.
- Editing reuses the same fields and uses `Save Changes`.
- Creating a chart persists the birth details, runs the database-backed chart generation service, and navigates to the first chart page at `/transit-wheel/{id}`.
- The home screen does not show a Saved Charts section and should fit above the fold at desktop widths.
## MudBlazor migration

The home page uses MudBlazor cards, fields, buttons, and layout primitives while retaining all existing Home, Charts, Preferences, edit, create, and transit-wheel links. The surrounding application shell uses MudLayout and MudAppBar. The transit-wheel chart rendering is unchanged.


## Shared header update

The tagline is displayed beside Iki-Astrro in the shared MudBlazor header. Duplicate homepage navigation is removed; Preferences and Saved Charts remain shared links. Existing Open Chart links continue to /charts/{id} and chart-page names use sunset orange.


## Name selection update

Filtered names are selectable directly and navigate to /charts/{id}. The home page no longer renders a selected-name summary, Edit details action, or Open Chart action. Primary CTAs use sunset orange with midnight text.

