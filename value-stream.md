---
last_updated: 2026-09-09
togaf: B — Business Architecture
safe: Solution Vision — the value stream
---

# ikiastrro — the value stream

The astrologer's chart-reading workflow. Every feature area in
[`masterproduct.md`](masterproduct.md) serves one step; a feature that serves no step is out
of scope.

| # | Step (what the astrologer does) | What ikiastrro provides | Feature areas |
|---|---|---|---|
| 1 | **Fix the moment** — birth date, time, place; corrections | geocoding, offline historical UTC offset, birth record | UI, INFRA |
| 2 | **Compute the frame** — ayanāṁśa, sidereal positions, Ascendant, sunrise | Swiss-ephemeris longitudes, selectable ayanāṁśa, provenance stored per chart | ASTRO_CALC |
| 3 | **Cast D1 and the vargas** — 21 charts | D1 + 20 vargas, DB-driven varga rules, within-sign varga degree | POSITION, VARGA |
| 4 | **Place and rank planets** — houses (3 reckonings), dignity, retrograde, combustion, nakṣatra | shared analytics on every chart type | HOUSE, NAKSHATRA, DIGNITY, RELATIONSHIP |
| 5 | **Overlay Jaimini** — Chara Karakas, Arudhas, special points, upagrahas | `CharaKaraka` + `PointKind` on every chart type | KARAKA |
| 6 | **Judge strength** — Ṣaḍbala, Bhāva Bala, avasthas | strength facts, planetary-state facts | STRENGTH, AVASTHA |
| 7 | **Read yogas** — named combinations with their source and confidence | source-attributed Raman/PVR corpus, `NOT_EVALUATED` for missing inputs | YOGA |
| 8 | **Time it** — Vimśottari dasha, transits, Sade Sati | 3-level dasha, slow-planet transit log, Sade Sati / Kantaka / Ashtama | DASHA, TRANSIT |
| 9 | **Assemble the evidence** — strengths, weaknesses, confirmations, conflicts, omissions | the evidence tables + the read surfaces | UI |
| 10 | **Decide** — the astrologer's own synthesis | *(deliberately not automated)* | — |

Steps 2–8 are the **DB + CLI** value stream (calculate and persist with provenance).
Steps 1, 9 are the **UI** value stream (capture and present). Step 10 stays with the human.
