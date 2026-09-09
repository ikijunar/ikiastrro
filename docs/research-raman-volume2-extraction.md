---
last_updated: 2026-09-07
---

# Volume II extraction: coverage and quality

**Status:** All 482 scan pages extracted; OCR draft complete. Targeted house-rule review
completed for the [PVR/Raman comparison](research-house-placement-pvr-raman.md).
This is not a claim that every sentence, chart or rule has been manually verified.

Bibliography and passage locators: `SRC_RAMAN_HTJH` in the
[source registry](research/reference-sources.md#house-placement-comparison-evidence-2026-09-07).

## Output

| Item | Location |
|---|---|
| Full page-marked OCR | `D:\@ClaudeSpace\BookExtracts\how-to-judge-a-horoscope-2-ocr.md` |
| Per-page OCR, TIFF scans, selected PNGs, extraction script and logs | `D:\@ClaudeSpace\BookExtracts\_work\raman-volume2-2026-09-07\` |
| Machine-readable extraction evidence | `manifest.json` and `progress.jsonl` in that working directory |

The user-supplied DJVU remains unchanged. Its SHA-256 is
`c5c5e2315ee6df3245c40a7e4b3fd3798d9889b4165545b313893aaadfc15cf3`.
The private full-text extract stays in the shared local corpus, outside the Git repository.

## Extraction method and result

- DjVuLibre reports 482 pages; whole-file embedded-text extraction returned zero bytes.
- Rendered every page at native resolution with `ddjvu`; OCR used Tesseract 5.5.0,
  English data, automatic page segmentation (PSM 3).
- Four workers, one OCR thread each, 60-second subprocess timeouts, incremental page
  files and progress records. No source deletions or cleanup of existing extracts.
- Completed in 178.1 seconds with 482 successful pages, zero failed pages and zero pages
  below 100 OCR characters. These are extraction checks, not accuracy scores.
- Ordered PAGE markers use scan order. Printed page 1 starts at scan 8; printed page
  numbers can be misread by OCR, so use scan markers to retrieve evidence.
- Original OCR is preserved. No automatic rewriting of meaning, numbers or conditions.

## Coverage map

All page numbers below are **scan pages**. Bibliographic printed-page locators stay in the registry.
The ranges identify sections and their closing qualifications, not a count of atomic rules.

| House / section | Chapter begins | Lord-placement section | Planet-occupant section |
|---|---:|---:|---:|
| 7 | 8 | 9–12 | 16–19, including modifiers |
| 8 | 78 | 78–83 | 92–95 |
| 9 | 187 | 187–190 | 193–196 |
| 10 | 245 | 246–249 | 255–258 |
| 11 | 369 | 369–372 | 374–375 |
| 12 | 418 | 418–421 | 431–432 |
| Practical illustrations | 459 | Not a placement table | Chart verification not performed |
| Technical index | 476 | Not a placement table | OCR ordering may be unreliable |

The six lord-placement sections provide coverage for destination houses 1–12, and the six
occupant sections include the seven classical planets plus Rahu/Ketu. The raw text is now
available for the 72 lord-placement and 54 occupant cells for houses 7–12. These are
section-coverage counts: conditions, alternative outcomes and sub-rules are not yet a
normalised executable catalogue.

## Verification and known issues

Six selected scans were visually inspected: 65, 81, 188, 255, 421 and 429. They substantiate
key comparison findings: the Venus/seventh qualification, eighth-lord and twelfth-lord
own-house results, Bhava-sandhi use, the Karakamsa overlap, and a source inconsistency.

- **Scan 188:** an afflicted-placement sentence denies domestic unhappiness immediately
  before describing misery/disharmony. The contradiction is present in the scan itself;
  do not silently change its polarity in a rule.
- **Scan 431→432:** the Venus-in-twelfth exaltation exception occurs on the following page.
  Extraction and rule capture must join continuations before interpreting a result.
- **Scan 429:** Ketu is twelfth from **Karakamsa**, not necessarily natal Lagna.
- **Charts/tables:** OCR cannot preserve spatial house positions reliably. No chart-grid
  transcription, chart lint, or birth-data recalculation was performed. Do not seed chart
  facts or numeric tables from this draft without image verification.
- OCR has visible digit/letter substitutions, broken words and occasional reading-order
  problems. The extract is searchable source material, not a corrected edition.

## Reuse

Search the page-marked extract, consult the corresponding TIFF/PNG, then record a short
source assertion with explicit conditions and its registry evidence key. Reuse the source's
existing `SRC_RAMAN_HTJH` code. The research comparison separates genuine disagreement
from missing counterparts, author qualifications and unresolved textual problems.

Validation record: the local working directory's `validation.json` reports 482 ordered
page markers, 674,954 extracted characters, 20 comparison evidence keys, a matching source
hash and no link/frontmatter/whitespace errors. Scoped Git diff checks also passed.
