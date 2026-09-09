# Web UI design rule

One design language, everywhere. Full guide:
[`../../../docs/ui/design-language.md`](../../../docs/ui/design-language.md); brand:
[`../../../docs/ui/brand.md`](../../../docs/ui/brand.md).

- **MudBlazor** is the component system — light theme, `MudLayout` / `MudAppBar` /
  `MudMainContent`, MudBlazor controls for chrome/forms/tables/dialogs. Warm Iki-Astrro brand
  (canvas `#FAF5EA`, midnight `#0F2041`, sunset `#F47A24`, Manrope). Primary actions:
  midnight fill, sunset text + border.
- **Tokens, not raw values.** `wwwroot/css/tokens.css` holds the `--brand-*` set plus the
  semantic astrology tokens (`--dignity-*`, `--dasha-*`, `--house-lagna`/`--house-moon`,
  `--wheel-*`, `--cell-fill`, …). Read with `var(--…)` — never a raw hex, never a CSS named
  colour, never an inline `<style>` in `.razor` markup.
- **CSS isolation per component** (`ComponentName.razor.css`). Isolation is what makes bare
  `table` / `th` / `td` / `.cell` selectors safe inside a chart component.
- **Chart diagrams stay hand-rolled** inline SVG / CSS grid (`SouthIndianGrid`, `PolarWheel`,
  `MiniGrid`, `ChartFrame`, `LifeWeeks`). No charting library is referenced by
  `Ikiastrro.Web`; Syncfusion is a deferred option only. See
  [`../../../docs/ui/dataviz.md`](../../../docs/ui/dataviz.md).
- **Additive change** keeps a revert mechanical: never repurpose a token's meaning (add a new
  one); version geometry helpers by addition (`AngleToXyV2` / a parameter).
- Run `dotnet format` before committing. If styling can't be expressed with existing tokens,
  extend `tokens.css` — don't hard-code around it.
