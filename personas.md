---
last_updated: 2026-09-09
togaf: A — Architecture Vision
safe: Solution Vision — who we build for
---

# ikiastrro — who it's for

## Primary — the practising Vedic astrologer / serious student

Reads charts by hand today, from classical sources (BPHS, Raman, PVR's *Integrated
Approach*, Phaladeepika). Uses Jagannatha Hora or similar for the numbers but does the
analysis themselves. Wants:

- every calculation's **convention and source** visible, not a black box;
- the **evidence for and against** a reading in one place — strengths, weaknesses,
  confirmations across vargas, timing relevance — so nothing significant is missed in a large
  body of chart data;
- **uncertainty made explicit** — where sources disagree, where a value is unverified, where
  a rule was not evaluated for lack of input;
- to keep their own judgement. The tool assembles evidence; it does not predict.

**Not** an end consumer wanting a one-line horoscope. **Not** someone who needs the software
to interpret for them.

## Secondary — the developer / researcher

Wants a reproducible, testable computational foundation: deterministic outputs, documented
assumptions, worked reference cases, comparison against independent implementations. Reads
the code and the `verify-*` suite as the specification.

## Operator — the maintainer (currently the author, as product owner)

Runs the CLI to add and regenerate charts, applies migrations, watches the `verify-*` gate,
prioritises the roadmap. Needs the docs to answer "what's true now" and "what's next" without
reading Git history.

## Explicit non-goals

Prediction or interpretation text · scoring a chart · replacing the astrologer's synthesis ·
multi-user / SaaS operation · chart rendering as the end deliverable.
