
## 2026-09-09 — Production yoga composition and persistence

Added ProductionYogaEngine as the single composition root for every implemented
Raman/PVR evaluator and the pending Raman 201–300 ledger. Chart generation now
persists 337 source variants per saved chart. Backfilled Ananya and Ramakrishnan;
all 300 Raman numbers and 18 PVR variants are represented with no duplicate
source identities. 124 yoga tests, Web/CLI builds, and transactional persistence
verification passed. Remaining unsupported predicates stay NOT_EVALUATED.

## 2026-09-09 — Yoga input calculation and sex persistence

Implemented the seven migration-51 context requirements for Raman 25, 58, 59,
66 and 68. Home saves optional Sex to BirthDetails; ChartBundle exposes the
shared evaluator and chart generation persists typed facts with missing codes,
phase policy and sunrise-method provenance. Fixed after-sunset day/night status.
Migration 052 applied twice; verified yoga migrations folded into baseline.
122 domain tests passed; Web/CLI builds passed; transactional DB verification
passed and rolled back fixtures. Full Web.Tests remains blocked by references
to the deleted CombinedD1D9Grid. Changes uncommitted; existing charts require
regeneration for new facts. See the active yoga handoff for full-Moon policy.

## 2026-09-07 — Persisted transit position reference
Added db/45_create_transit_position_reference.sql and GocharaRepository.SaveSnapshots. Exact requested-time positions are upserted by planet and UTC timestamp with sign, longitude, degree, nakshatra/pada, speed, motion, ayanamsa, and sign-window qualifiers. Solution build passes.

