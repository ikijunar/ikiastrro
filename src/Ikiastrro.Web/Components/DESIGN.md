# Web UI design rule

> **2026-09-08 canonical brand override:** ../../../brandguide_ikiastrro.md and its supplied main-screen image supersede the former dark-only parchment/serif direction. Use one light warm-canvas theme, midnight blue, sunset orange, and Manrope throughout. Primary actions use midnight-blue fills with sunset-orange text and borders; preserve the Ganesha/Navagraha composition and dedication footer.

One design language exists in this app — the parchment/serif "South Indian style" look
originally built for the chart view. Everything else should match it, not invent
its own palette.

**Tokens** live in `wwwroot/css/tokens.css` (`--paper`, `--ink`, `--accent`, the 7
`--dignity-*` colors, etc.) as real `:root` custom properties. Read them with
`var(--token-name)` — never a raw hex or a CSS named color (the "rebeccapurple"/"burlywood"
placeholders once in the pages that are now `SavedCharts.razor` and `Workspace.razor` were exactly
that: never-revisited scaffolding, not a real color choice). Dark-only (2026-08-28, rammyps's explicit call): there's
no `prefers-color-scheme` split anymore — the app has one theme, not a light/dark pair, so a
new token just needs one value, not two.

**Structure**: every page or component gets its own Blazor CSS-isolation file
(`ComponentName.razor.css`) — never an inline `<style>` block inside the `.razor` markup.
Isolation is what already made the chart view's scoped CSS safe to write bare `table`/`th`/`td`
selectors in without leaking into the rest of the app; the fix for the rest of the app is
using the same mechanism consistently, not adopting a new CSS framework or component library.

**One exception — data visualization**: `Syncfusion.Blazor` (Charts / Gauge, Community
License) is allowed for charts, heatmaps, timelines, and the polar longitude wheel — the
things this design language never covered. Its theme CSS must be scoped (wrap in
`.sf-scope`, keep the Syncfusion sheet out of hand-rolled components) and its chart
`Palettes` fed the same hex values `tokens.css` defines. Layout, tables, and the
North/South Indian chart *diagrams* stay hand-rolled. Full rules:
`../../docs/uidesign-dataviz.md`.

> **2026-09-03 update** (mirrors `docs/uidesign-specs.md` §1): the varga-centric rebuild did
> **not** take the Syncfusion dependency. `PolarWheel.razor` (the polar longitude wheel), the
> South / North Indian grids, `MiniGrid`, and every chart diagram are hand-rolled SVG / CSS —
> **no `Syncfusion.Blazor` package is referenced by `Ikiastrro.Web`**. Syncfusion remains a
> documented *option* for a later dataviz pass, not a current dependency.

**Tooling**: no new dependency is needed to enforce this — run `dotnet format` before
committing. If a new page's styling can't be expressed with the existing tokens, extend
`tokens.css`, don't hard-code around it.
