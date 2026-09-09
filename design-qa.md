# Iki-Astrro Home screen design QA

## Source visual truth

- Source: `reports/brand/ikiastrro-main-screen-reference.png`
- Source pixels: 1672 × 941
- Source state: supplied Iki-Astrro main screen, two saved charts, empty birth form

## Implementation evidence

- Screenshot: `reports/home-main-screen-implementation.png`
- Comparison: `reports/home-main-screen-comparison.png`
- Route: `/`
- Viewport: 1672 × 941 CSS px, device scale factor 1
- Implementation state: Home screen with the live saved-chart list, empty birth form, canonical brand palette
- Browser: clean extension-free Edge CDP session; automatic dark mode disabled

## Comparison

The source and implementation were normalized to equal 1672 × 941 pixels and reviewed together
in `reports/home-main-screen-comparison.png`. The implementation preserves the main composition:
brand lockup at the top, close navigation, left-side birth form and saved charts, right-side
Ganesha/Navagraha artwork, warm canvas and mountain/footer treatment.

The live database contains three saved people rather than the source image's two. This is expected
content variance, not a layout defect. The implementation uses the exact supplied artwork crop,
not a redraw or placeholder.

## Required fidelity surfaces

- Fonts and typography: Manrope is bundled locally in five weights and is the computed font for
  the Home root. Heading, lockup, labels, fields and actions use the requested single family.
- Spacing and layout rhythm: desktop form, saved-chart rows, right artwork and footer remain in
  the source's two-column rhythm; the form is capped at the source width and the saved list spans
  the wider left workspace.
- Colors and visual tokens: computed root background is `rgb(250, 245, 234)` (`#FAF5EA`), primary
  action background is `rgb(15, 32, 65)` (`#0F2041`), and action text is `rgb(244, 122, 36)`
  (`#F47A24`).
- Image quality and asset fidelity: `ganesha-navagraha-brand.png` and
  `footer-mountains-brand.png` are exact crops from the supplied reference; no CSS/SVG/emoji
  substitute is used.
- Copy and content: `Discover Your Path`, `Iki-Astrro | Where Passion, Purpose & Planets Align.`,
  `Generate Chart`, `Saved Charts`, and the preserved dedication footer are present. The old
  explanatory subheading is absent.

## Interaction checks

- Generate Chart form remains a real `EditForm` with validation, duplicate-name protection,
  geocoding/manual-location fallback, chart generation and navigation to the saved chart.
- Saved-chart name filter was exercised: three rows reduced to one matching row, then restored.
- Primary navigation links remain available.
- Desktop document scroll width (1657) is within the 1672px viewport.
- Browser console exception list was empty.

## Findings

- No actionable P0, P1 or P2 differences remain for this first main-screen pass.

## Follow-up polish

- [P3] The source uses small search and open-chart glyphs; the implementation keeps text-first
  controls to avoid introducing a mismatched icon set. Add a consistent icon library in a later
  app-wide pass if those affordances remain important.
- [P3] Validate the same composition at the project's mobile breakpoint once the chosen mobile
  navigation behavior is finalized.

## Final result

passed
