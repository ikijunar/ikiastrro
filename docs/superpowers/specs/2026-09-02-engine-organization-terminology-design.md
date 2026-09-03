# Project foundations + engine organization + terminology + rule-table portability — design

**Status:** executed — Plan 0 merged; Plan 1 complete 2026-09-03 (`feat/ikiastrro-workspace-ui`, commits `2271eec`..`dd2ad67`). §9 interpreter table reconciled below to what shipped (`GRID_VARGA`). · **Created:** 2026-09-02 · **Branch:** `feat/ikiastrro-workspace-ui`
**Supersedes nothing.** Consumes the divisional-chart, avastha, and Jaimini work already merged.
**Sequencing:** Plan 0 (foundations — docs standard, citation registry, `INFRASTRUCTURE.md`,
env-agnostic data access) → Plan 1 (engine reorg + terminology + rule portability) →
Plan 2 (Dispositor + Karaka/Avastha gaps) → Plan 3 (Strength) → Plan 4 (Yoga).

## Research
Status: [ ] complete   [x] partial   [ ] not started
| Research doc | Needed for | Status |
|---|---|---|
| *(none)* | Plan 0 + Plan 1 are structural/additive — no new astrology, no new classical sourcing | complete |
| `docs/research-topic-coverage.md` | P2 avastha slices, P3 Ṣaḍbala, P4 yogas — **not required for this spec** | partial |

---

## 1. Problem

`Ikiastrro.Core` has grown to 78 `.cs` files across 9 folders whose boundaries no longer
match how the domain actually layers. Symptoms:

- **Diagnosis is slow.** When a value is wrong there is no single place that says "the House
  engine owns this." Logic for houses, lordship, nakshatra linkage, conjunctions, and aspects
  all lives inside one 190-line `ChartAnalyzer.cs`.
- **Naming is mixed.** Some identifiers are romanized Sanskrit (`BaaladiAvastha`,
  `NavamsaD9SignRule`), some English (`ClassicalDignity`, `ChartHouseLord`), with no rule for
  which to use, and no single place a term's Sanskrit / English / Tamil forms + definition live.
- **Rules point at code, not at data.** `tbl_Rule_VargaScheme.SignRuleKey` names a C# class. A
  port to another language or database would have to re-derive every varga / avastha / (future)
  yoga rule from the C#, not copy it from the tables.
- **Not-yet-built engines have no home.** Dispositors, Ṣaḍbala / Vimśopaka strength, and Yoga
  detection are on the roadmap with nowhere to land.
- **No documentation standard.** `.md` files sprawl across the repo root and `docs/` with no
  rule for research vs. artifact vs. spec vs. plan, no feature-level completion view, and
  classical-source attributions ("per B.V. Raman", "BPHS ch. 27") scattered inline everywhere.
- **No infrastructure guidance.** The connection string hard-codes DB `ikiastrro` on `localhost`;
  there is no dev / stage / uat / prod story, and no rule for what may carry an environment token.

## 2. Goals

**Foundations (Plan 0 — precedes all engine work):**

0a. A **documentation taxonomy** (`STANDARDS.md §M.2`): four buckets — research, artifacts,
    specs, plans — plus two indexes: `master_ikiastrro.md` (doc map, exists) and `PRODUCT.md`
    (feature map + per-dimension completion checklist).
0b. A **single citation registry** — `tbl_Ref_Source` + its mirror `docs/research/reference-sources.md`.
    Everything (rule tables, terminology, specs, plans, artifacts) cites a `SourceRefCode`
    (`SRC_BPHS_27`, `SRC_RAMAN_HTJH`); author / text / edition details live **only** in the
    registry. `STANDARDS.md §M.3`.
0c. A **research-status checkbox** on every spec and plan: which `docs/research/*` docs it
    depends on, and whether that research is complete, partial, or not started — so unresearched
    work is visible before it is scheduled.
0d. **`INFRASTRUCTURE.md`** — environments (dev / stage / uat / prod), the database-naming rule,
    config & secrets layering, and the migration-application policy per environment.
0e. **Environment-agnostic data access** — `SqlConnectionFactory` reads the full connection
    string from `IConfiguration`; CLI verify / recompute modes take `--db`; `db/ikiastrro.sql`
    uses a `sqlcmd :setvar` for the catalog name. No environment token in any schema object name.

**Engine work (Plan 1):**

1. A **named-engine layering** of every `.cs` file, one folder + one namespace per engine, so
   "which engine is misbehaving" is answerable at a glance.
2. A **hybrid naming convention**: English for structural and greenfield names; the canonical
   romanized Sanskrit noun for established Jyotiṣa concepts; **every** concept bound to a stable
   ASCII `Code`. (`STANDARDS.md §D.2`.)
3. **`tbl_Astro_Terminology` (+ `_Text`)** — the single source of truth for every concept's
   Sanskrit (default) / English / Tamil (script-ready) name + short description + technical
   definition + calculation-method prose + `SourceRefCode`.
4. **Self-describing `tbl_Rule_*` tables** — each rule row carries its structured parameters
   and/or a human-readable method + `SourceRefCode`, so a port copies table data and reimplements
   a small fixed set of generic interpreters rather than every individual rule.
5. A **`ChartPipeline` → `ChartBundle`** façade in Core so the whole engine chain runs and is
   testable without a database.
6. Homes (folder + namespace + interface + table names) reserved for the **Dispositor**,
   **Strength**, and **Yoga** engines, scoped here, built in follow-on plans.

1. A **named-engine layering** of every `.cs` file, one folder + one namespace per engine, so
   "which engine is misbehaving" is answerable at a glance.
2. A **hybrid naming convention**: English for structural and greenfield names; the canonical
   romanized Sanskrit noun for established Jyotiṣa concepts; **every** concept bound to a stable
   ASCII `Code`.
3. **`tbl_Astro_Terminology` (+ `_Text`)** — the single source of truth for every concept's
   Sanskrit (default) / English / Tamil (script-ready) name + short description + technical
   definition + calculation-method prose + source citation.
4. **Self-describing `tbl_Rule_*` tables** — each rule row carries its structured parameters
   and/or a human-readable method + citation, so a port copies table data and reimplements a
   small fixed set of generic interpreters rather than every individual rule.
5. A **`ChartPipeline` → `ChartBundle`** façade in Core so the whole engine chain runs and is
   testable without a database.
6. Homes (folder + namespace + interface + table names) reserved for the **Dispositor**,
   **Strength**, and **Yoga** engines, scoped here, built in follow-on plans.

## 3. Non-goals

- **No new astrology math in Plan 1.** No Ṣaḍbala, no yogas, no dispositor traversal, no new
  avastha slices. Those are Plans 2–4.
- **No mass rename of domain nouns.** `Nakshatra`, `Dasha`, `Karaka`, `Varga`, `Navamsa`, the
  eight Chara-Karaka labels (`AK`…`DK`), `Moolatrikona`, `Arudha`, `Gulika`, `Maandi`,
  `Rahu`/`Ketu` stay as identifiers.
- **No `db/` history scrub.** Still separately pending; not touched here.
- **No renaming of the shipped `tbl_Chart_*` / `tbl_ChartResults` fact tables.** Only additive
  `Code` columns.

## 4. Target architecture — the engine stack

Build order is bottom-up; a Yoga rule "asks down" the same stack top-down. Each engine =
`src/Ikiastrro.Core/Engines/<Name>/` + namespace `Ikiastrro.Core.Engines.<Name>` + (where it
has consumers outside itself) one façade interface `I<Name>Engine`.

| # | Engine (`Engines.<Name>`) | Owns | Status | `EngineCode` |
|---|---|---|---|---|
| 0 | `Geocoding` *(stays `Core.Geocoding`)* | place → lat/long/IANA zone/offset | ✅ built | `GEO` |
| 1 | `Astronomy` | Julian Day, Ayanāṁśa (Lahiri), sidereal graha positions (lon/lat/speed/retro), Ascendant, sidereal time, sunrise/sunset (`swe_rise_trans`) | ✅ built | `ASTRO_CALC` |
| 2 | `Position` | assemble the Rāśi (D1) chart: sign, degree-in-sign, retro, house-from-Lagna per graha + Ascendant | ✅ built | `POSITION` |
| 3 | `DivisionalCharts` | D2–D60 (21 chart types), data-driven from `tbl_Rule_VargaScheme` | ✅ built | `VARGA` |
| 4 | `Houses` | whole-sign houses (Lagna / Sūrya / Chandra), house→sign→lord, functional nature, bhāva significations | ⚠️ embedded in `ChartAnalyzer` | `HOUSE` |
| 5 | `Nakshatras` | nakshatra, pāda, Vimśottari lord, KP sub-lord, reference linkage | ✅ built (embedded) | `NAKSHATRA` |
| 6 | `Dignity` | exaltation / debilitation / Mūlatrikoṇa / own-sign, `DignityStatus` (9-tier) | ✅ built | `DIGNITY` |
| 7 | `Dispositors` | sign-lord traversal, dispositor chains, final dispositor, mutual reception | ❌ NEW | `DISPOSITOR` |
| 8 | `Karakas` | Chara Karakas (8-fold) ✅, special points (AL, 12 Bhāva Arudhas, HL, Gulika, Maandi) ✅, Sthira Karaka ❌, Naisargika Karaka ❌ | ⚠️ 2 of 4 | `KARAKA` |
| 9 | `PlanetaryStates` *(was Avastha)* | `AgeState` (Bālādi) ✅, `WakefulnessState` (Jāgradādi) ✅, `RadianceState` (Dīptādi) ❌, `ShameState` (Lajjitādi) ❌, `PostureState` (Śayanādi) ❌ | ⚠️ 2 of 5 | `AVASTHA` |
| 10 | `Relationships` | Yuti (conjunction) ✅, Dṛṣṭi (aspect) ✅, combustion ✅, compound Pañchadhā Maitrī ❌, argala ❌, sambandha ❌ | ⚠️ core built | `RELATIONSHIP` |
| 11 | `Strength` | Ṣaḍbala (6 components), Iṣṭa/Kaṣṭa Phala, Bhāva Bala, Vimśopaka Bala, Vaiśeṣikāṁśa | ❌ NEW — large | `STRENGTH` |
| 12 | `Dasha` *(stays `Core.Engines.Dasha`)* | Vimśottari ✅; Aṣṭottarī / Yoginī / Chara later | ⚠️ 1 system | `DASHA` |
| 13 | `Yoga` | detection: Pañcha Mahāpuruṣa, Rāja, Dhana, Nābhasa, Neechabhaṅga, Parivartana… composes engines 3/4/6/9/10/11/12 | ❌ NEW — large | `YOGA` |

**Cross-cutting (not `Engines.*`):**

| Namespace | Owns |
|---|---|
| `Ikiastrro.Core.Reference` | `TerminologyCatalog`, `TerminologyCode` constants, the reference-row models (`PlanetReference`, `SignAttributeReference`, `NakshatraReference`, …) |
| `Ikiastrro.Core.Pipeline` | `ChartPipeline`, `ChartBundle`, `IChartCalculator`, `ChartCalculationOrchestrator` |
| `Ikiastrro.Core.Model` | pure DB-row shapes (`ChartKeyDetail`, `ChartHouseLord`, `ChartResult`, `BirthDetails`, `GenerationReport`, …) — renamed from `Models` (singular, matches `Reference` / `Pipeline`) |
| `Ikiastrro.Core.Geocoding` | unchanged |
| `Ikiastrro.Core.LifeAreas` | unchanged (Web workspace grouping) |

Engine-output DTOs move **next to their engine** (`SiderealPositions` / `SunTimes` →
`Engines.Astronomy`; `PlanetPosition` → `Engines.Position`; `ChartAnalysisInput` →
`Ikiastrro.Core.Pipeline`).

## 5. Naming convention (hybrid)

Recorded as a new `STANDARDS.md` §D.2 clause.

| Kind | Identifier rule | Example | `Code` | Display from |
|---|---|---|---|---|
| Engine / structural | English | `HouseEngine`, `StrengthEngine`, `ChartPipeline`, `DispositorChain` | — | — |
| Greenfield domain (no shipped identifier) | English | `AgeState` { `Infant`…`Dead` }, `WakefulnessState`, `PositionalStrength`, `DirectionalStrength` | `AVASTHA_BALA`, `STRENGTH_STHANA` | `tbl_Astro_Terminology` |
| Established Jyotiṣa noun | canonical romanized Sanskrit, one spelling | `Nakshatra`, `Dasha`, `CharaKaraka.AK`, `NavamsaD9SignRule`, `ArudhaCalculator` | `NAK_ASHWINI`, `KARAKA_AK`, `VARGA_D9` | `tbl_Astro_Terminology` |
| Enum member | as above per kind | `ZodiacName.Aries` | `SIGN_ARIES` | `tbl_Astro_Terminology` |

**Rules:**
1. No display string (Sanskrit, English, or Tamil) is hard-coded in C#, a `.razor`, or a SQL
   view. Display resolves through `TerminologyCatalog.Label(code, lang)` / `.Describe(...)`.
2. Every `Code` used in C# is a `const` on `Ikiastrro.Core.Reference.TerminologyCode` — never a
   bare string literal.
3. Spelling normalization (Plan 1, folded into the reorg commits, not a separate churn event):
   pick one romanization for `Baaladi`/`Jagradadi` → **`Baladi`/`Jagradadi`**; keep
   `Capricornus` in the `ZodiacName` enum (stored-data compatibility) but its terminology
   `Code` is `SIGN_CAPRICORN` and every display is "Capricorn". No other enum spelling changes.
4. **Renamed in Plan 1** (English is a genuine improvement, low blast radius — all inside
   `PlanetaryStates`): `BaaladiAvastha`→`AgeState`, `JagradadiAvastha`→`WakefulnessState`,
   `PlanetAvasthaComputer`→`PlanetaryStateComputer`, `tbl_Dim_AvasthaState`→
   `tbl_Dim_PlanetaryState`, `tbl_Rule_BaaladiState`/`_JagradadiState`→
   `tbl_Rule_AgeState`/`_WakefulnessState`, `tbl_Fact_PlanetAvastha`→`tbl_Fact_PlanetaryState`.
   Members: `AgeState { Infant, Child, Youth, Old, Dead }`,
   `WakefulnessState { Awake, Dreaming, Sleeping }`. `verify-avastha` re-baselined.

## 6. Namespace & folder layout

```
src/Ikiastrro.Core/
  Engines/
    Astronomy/        SwissEphemerisProvider, BirthMomentFactory, AstroMath, AstroIds,
                      PlanetName, ZodiacName, ConstellationName, SiderealPositions, SunTimes
    Position/         D1ChartComputer, D1RasiCalculator, PlanetPosition
    DivisionalCharts/ VargaChartComputer, VargaCalculator, IVargaSignRule, VargaSignRuleFactory,
                      + 17 *SignRule, VargaScheme
    Houses/           HouseEngine (new), HouseLordResolver (new), LagnaFunctionalNature,
                      ChartHouseLord   ← house + lordship extracted from ChartAnalyzer
    Nakshatras/       NakshatraEngine (new — thin, wraps AstroMath nak helpers), the 3 *Reference
    Dignity/          DignityEngine (renamed from ClassicalDignity), DignityResult
    Dispositors/      IDispositorEngine, DispositorChain            (NEW — interface only in P1)
    Karakas/          CharaKaraka, CharaKarakaCalculator, ArudhaCalculator, HoraLagnaCalculator,
                      UpagrahaCalculator, SpecialPoint{Seed,Projector,Calculator},
                      ISthiraKarakaSource / INaisargikaKarakaSource   (NEW interfaces in P1)
    PlanetaryStates/  AgeState, WakefulnessState, PlanetaryStateComputer, PlanetaryStateModels
    Relationships/    RelationshipEngine (new — Yuti + Dṛṣṭi extracted from ClassicalRelationships),
                      CombustionEngine (renamed from ClassicalCombustion), CombustionResult
    Strength/         IStrengthEngine, ShadbalaResult, VimsopakaResult   (NEW — interface only in P1)
    Dasha/            VimshottariDashaCalculator, DashaPeriod, DashaPeriodRecord, LifeWeek
    Yoga/             IYogaEngine, YogaResult                            (NEW — interface only in P1)
  Pipeline/           ChartPipeline, ChartBundle, IChartCalculator, ChartCalculationOrchestrator,
                      ChartAnalysisInput
  Reference/          TerminologyCatalog, TerminologyCode, *Reference models
  Model/              ChartKeyDetail, ChartHouseLord, ChartConjunction, ChartAspect, ChartResult,
                      BirthDetails, ChartTypeRow, GenerationReport, VargaScheme?, HouseNakshatraSpanRow,
                      PlanetTransitSnapshot, SadeSatiPeriod
  Geocoding/          (unchanged)
  LifeAreas/          (unchanged)
  Transits/           PlanetTransitEventFinder  (stays; feeds Sade Sati, not the birth-chart pipeline)
```

`Jaimini/` and `SpecialPoints/` folders are **dissolved** into `Engines/Karakas/`.
`Calculators/` and `Astro/` folders are **dissolved** into the engines above.

## 7. File migration map (Plan 1 core)

Every file moves; no file's logic changes except the extractions and the avastha renames.
Full row-per-file table lives in the Plan-1 implementation plan; the shape:

| From | To | Change |
|---|---|---|
| `Astro/SwissEphemerisProvider.cs` | `Engines/Astronomy/` | namespace only |
| `Astro/*SignRule.cs` (17) + `IVargaSignRule` + `VargaSignRuleFactory` | `Engines/DivisionalCharts/` | namespace only |
| `Astro/{PlanetName,ZodiacName,ConstellationName,AstroMath,AstroIds}.cs` | `Engines/Astronomy/` | namespace only |
| `Calculators/{D1ChartComputer,D1RasiCalculator}.cs` | `Engines/Position/` | namespace only |
| `Calculators/{VargaChartComputer,VargaCalculator}.cs` | `Engines/DivisionalCharts/` | namespace only |
| `Calculators/ChartCalculationOrchestrator.cs`, `IChartCalculator.cs`, `ChartAnalysisInput.cs` | `Pipeline/` | namespace only |
| `Calculators/ChartAnalyzer.cs` | **split** → `Engines/Houses/HouseEngine.cs` + `Engines/Nakshatras/NakshatraEngine.cs` + `Engines/Relationships/RelationshipEngine.cs` + a thin `Pipeline/ChartAnalyzer.cs` that composes them | **extract** — behaviour identical, `verify-schema` unchanged |
| `Calculators/ClassicalDignity.cs` | `Engines/Dignity/DignityEngine.cs` | rename type |
| `Calculators/ClassicalRelationships.cs` | `Engines/Relationships/RelationshipEngine.cs` | rename type, merge with the extract above |
| `Calculators/ClassicalCombustion.cs` | `Engines/Relationships/CombustionEngine.cs` | rename type |
| `Calculators/LagnaFunctionalNature.cs` | `Engines/Houses/` | namespace only |
| `Calculators/{BaaladiAvastha,JagradadiAvastha,PlanetAvasthaComputer}.cs` | `Engines/PlanetaryStates/` | **rename** per §5.4 |
| `Calculators/ChartViewModel.cs` | `Ikiastrro.Core.Presentation` (new tiny ns) | namespace only |
| `Calculators/BirthMomentFactory.cs` | `Engines/Astronomy/` | namespace only |
| `Jaimini/*`, `SpecialPoints/*` | `Engines/Karakas/` | namespace only |
| `Dasha/*` | `Engines/Dasha/` | namespace only |
| `Models/*` | `Model/` (+ compute DTOs to their engine) | namespace only |
| `Geocoding/*`, `LifeAreas/*`, `Transits/*` | unchanged | — |

Downstream: `Ikiastrro.Cli`, `Ikiastrro.Data`, `Ikiastrro.Web` update `using` lines only.
`Ikiastrro.Data.ChartGenerationService` additionally adopts the `ChartPipeline` façade (§10).

## 8. Terminology tables

```sql
-- language-neutral concept catalogue
CREATE TABLE dbo.tbl_Astro_Terminology (
    TerminologyId   INT IDENTITY(1,1) CONSTRAINT PK_Astro_Terminology PRIMARY KEY,
    Category        VARCHAR(30)  NOT NULL,   -- Planet|Sign|House|Nakshatra|NakshatraPada|
                                             --  DivisionalChart|Karaka|SpecialPoint|AvasthaState|
                                             --  DignityState|Relationship|StrengthComponent|
                                             --  Dasha|Yoga|Ayanamsa|Concept
    Code            VARCHAR(40)  NOT NULL CONSTRAINT UQ_Astro_Terminology_Code UNIQUE,
    ParentCode      VARCHAR(40)  NULL,       -- self-ref: pada->nakshatra, sub-strength->shadbala
    EngineCode      VARCHAR(30)  NULL,       -- ASTRO_CALC|VARGA|HOUSE|NAKSHATRA|DIGNITY|DISPOSITOR|
                                             --  KARAKA|AVASTHA|RELATIONSHIP|STRENGTH|DASHA|YOGA
    NumericKey      INT          NULL,       -- the enum/dim integer, for cheap joins
    FormulaSummary  VARCHAR(300) NULL,       -- one-line canonical "how"; names the rule table
    DisplayOrder    INT          NOT NULL CONSTRAINT DF_Astro_Terminology_DisplayOrder DEFAULT 0,
    IsActive        BIT          NOT NULL CONSTRAINT DF_Astro_Terminology_IsActive DEFAULT 1,
    CONSTRAINT CK_Astro_Terminology_Category CHECK (Category IN (
        'Planet','Sign','House','Nakshatra','NakshatraPada','DivisionalChart','Karaka',
        'SpecialPoint','AvasthaState','DignityState','Relationship','StrengthComponent',
        'Dasha','Yoga','Ayanamsa','Concept')),
    CONSTRAINT FK_Astro_Terminology_Parent FOREIGN KEY (ParentCode)
        REFERENCES dbo.tbl_Astro_Terminology (Code)
);

-- one row per (concept, language, script)
CREATE TABLE dbo.tbl_Astro_TerminologyText (
    TerminologyTextId    INT IDENTITY(1,1) CONSTRAINT PK_Astro_TerminologyText PRIMARY KEY,
    TerminologyId        INT           NOT NULL,
    LanguageCode         CHAR(2)       NOT NULL,   -- 'sa' (default) | 'en' | 'ta'
    Script               VARCHAR(8)    NOT NULL CONSTRAINT DF_Astro_TerminologyText_Script DEFAULT 'Latn',
    Name                 NVARCHAR(100) NOT NULL,
    TraditionalName      NVARCHAR(100) NULL,
    ShortDescription     NVARCHAR(400) NULL,
    TechnicalDefinition  NVARCHAR(MAX) NULL,
    CalculationMethod    NVARCHAR(MAX) NULL,       -- translated prose of the derivation
    SourceRefCode        VARCHAR(30)   NULL,       -- -> tbl_Ref_Source.Code (§16); no author strings here
    CONSTRAINT FK_Astro_TerminologyText FOREIGN KEY (TerminologyId)
        REFERENCES dbo.tbl_Astro_Terminology (TerminologyId),
    CONSTRAINT CK_Astro_TerminologyText_Lang CHECK (LanguageCode IN ('sa','en','ta')),
    CONSTRAINT CK_Astro_TerminologyText_Script CHECK (Script IN ('Latn','Deva','Taml')),
    CONSTRAINT UQ_Astro_TerminologyText UNIQUE (TerminologyId, LanguageCode, Script)
);
```

**Seed in Plan 1** (`sa`/`Latn` + `en`/`Latn` for every row), mechanically from the existing
enums, `tbl_Dim_*`, `tbl_Rule_VargaScheme`, and the XML-doc summaries already in the source:

| Category | Count | Source |
|---|---|---|
| Planet | 9 | `PlanetName` + `tbl_Planets` |
| Sign | 12 | `ZodiacName` + `tbl_SignAttributes` |
| House | 12 | 1–12 |
| Nakshatra | 27 | `ConstellationName` + `tbl_Nakshatras` |
| NakshatraPada | 108 | `tbl_NakshatraPadas` |
| DivisionalChart | 21 | `tbl_Dim_ChartType` + `tbl_Rule_VargaScheme` |
| Karaka | 8 + special points | `CharaKaraka` + `SpecialPointSeed` codes |
| AvasthaState | 8 (now) → 20+ (P2) | `tbl_Dim_PlanetaryState` |
| DignityState | 9 | `DignityStatus` values |
| Relationship | ~6 | Yuti / Dṛṣṭi / combustion / friendship tiers |
| Ayanamsa | 1 | Lahiri |
| StrengthComponent / Yoga | rows added in P3 / P4 | — |

`ta`/`Taml` and `sa`/`Deva` rows are **pure inserts later** — zero schema change (the goal you
set: build the scope so Tamil is no rework).

**C#:** `Ikiastrro.Core.Reference.TerminologyCatalog` (loaded once, cached) exposes
`Label(code, lang='sa')`, `Traditional(code, lang)`, `Describe(code, lang)`.
`TerminologyCode` holds every `Code` as a `const string`. A CLI `verify-terminology` mode
asserts: every `tbl_Dim_*` / enum value has a matching `Code`; every `Code` has `sa` + `en`
text; no orphan `ParentCode`.

## 9. Self-describing rule tables

**Common portability tail** added to every `tbl_Rule_*` table (some columns already exist):

```sql
  RuleSetId            INT           NOT NULL  -- STANDARDS §D.1 immutability (already required)
  MethodCode           VARCHAR(30)   NOT NULL  -- interpreter tag (enum below)
  RuleParametersJson   NVARCHAR(MAX) NULL      -- the structured rule
  CalculationNarrative NVARCHAR(MAX) NULL      -- human-readable derivation
  SourceCitation       VARCHAR(200)  NULL
  IsActive             BIT           NOT NULL CONSTRAINT ... DEFAULT 1
```

**The fixed interpreter set** — what a port reimplements. The per-rule tag is a top-level
`"method"` key **inside `RuleParametersJson`**, not a column: `tbl_Rule_VargaScheme.MethodCode`
keeps its existing meaning (the classical scheme — `ClassicalTwoSign` / `UmaShambu` /
`ParasaraTraditional` / `SanjayRath`). `tbl_Rule_Catalog.MethodCodes` lists the tags a table's
rows use.

| tag | Meaning | Used by |
|---|---|---|
| `LINEAR_VARGA` | `sign = (rasi + floor(degInSign / (30/factor)) * stride) mod 12`; params `{factor, stride}` | D3, D4, D12, D60 (D1 is the identity, not in `tbl_Rule_VargaScheme`) |
| `GRID_VARGA` | params `{parts, map:[[12 sign indices] × parts]}` — a full `parts × 12` grid `map[part][rasiSign] → sign`, sampled from the C# rule at each part's midpoint. Subsumes the plan's original `TABLE_VARGA` (odd/even part→sign map) **and** `WRAPPED_VARGA` (named closed-form): a 12-column grid also captures the movable/fixed/dual (D9) and elemental (D27) branchings, and a `{"closedForm":"GetNavamsaSign"}` pointer is not executable by a non-C# port. | D2, D2-US, D5, D6, D7, D8, D9, D10, D11, D16, D20, D24, D27, D40, D45 |
| `BAND_VARGA` | unequal degree bands → sign; params `{edges, map:[[12] × bands]}` (edges = the union of the odd/even breakpoints; `map[band][rasiSign] → sign`) | D30 |
| `BAND_LOOKUP` | `(key, degree-band) → value` | Age (Bālādi) state |
| `MAP_LOOKUP` | `key → value` | Wakefulness (Jāgradādi) state, natural relationship |
| `OFFSET_LIST` | `planet → [house offsets]` | aspect offsets |
| `ORB_PAIR` | `planet → (directOrb, retroOrb)` | combustion |
| `DISTANCE_SET` | `{house distances} → relationship` | temporary friendship |
| `WEIGHT_TABLE` | `key → weight / max` | Vimśopaka weights, Ṣaḍbala component weights (P3) |
| `PREDICATE_SET` | ordered boolean predicates + result | Yoga rules (P4) |

The C# closed-forms `GetNavamsaSign` etc. still exist and drive the live compute path; `GRID_VARGA`
is the portable serialization of their output, proven equal by `verify-rules` over the full circle.
Only `LINEAR_VARGA`, `GRID_VARGA`, `BAND_VARGA` have concrete interpreter classes in Plan 1
(`IVargaMethodInterpreter` + `VargaMethodInterpreterFactory`); the lookup/weight/predicate tags are
reserved for P2–P4 rule tables.

The `SourceRefCode VARCHAR(40) NULL` column (portability tail) is validated by a
`CHECK (… LIKE 'SRC[_]%')` and resolved by `verify-sources` against `tbl_Dim_Source`
(shipped as `tbl_Dim_Source`, not the spec's earlier `tbl_Ref_Source`) and its mirror
`docs/research/reference-sources.md`. Author / text / edition strings appear **only** there,
never inline in a rule row, a diagram, or an artifact.

**Existing tables — Plan 1 changes:**

| Table | Add |
|---|---|
| `tbl_Rule_VargaScheme` | `RuleParametersJson` (linear factor/stride; table maps; D30 bands), `CalculationNarrative`; keep `SignRuleKey` as an interpreter hint. Backfill from the 17 rule classes. |
| `tbl_Rule_AspectOffset` | `SourceRefCode` (→ `SRC_BPHS_26`) |
| `tbl_Rule_CombustionOrb` | `SourceRefCode` (→ `SRC_BPHS_COMBUSTION` / `SRC_PHALADEEPIKA`) |
| `tbl_Rule_NaturalRelationship`, `tbl_Rule_TemporaryFriendshipDistance` | `SourceRefCode` |
| `tbl_Rule_AgeState` (renamed), `tbl_Rule_WakefulnessState` (renamed) | `MethodCode`, `SourceRefCode` if absent |

**New tables — created empty/interface-only in Plan 1, populated in later plans:**

| Table | Plan | Holds |
|---|---|---|
| `tbl_Rule_HouseSignification` | P2 | house → significations (migration 030 scope) |
| `tbl_Rule_Karaka` | P2 | Sthira (planet→house), Naisargika ordering (Sapta/Ashta rows), Chara scheme (7/8, Rahu-reverse flag) |
| `tbl_Rule_ShadbalaComponent` | P3 | per bala: weights, max Rūpas, sub-lookups (Naisargika fixed values, Dig best-house, Kāla tri-bhāga, Cheṣṭā motion bands) |
| `tbl_Rule_VimsopakaWeight` | P3 | 16 varga weights × scheme (Ṣaḍ/Sapta/Daśa/Ṣoḍaśa-varga) |
| `tbl_Rule_Yoga` | P4 | `YogaCode`, `YogaCategory`, `RequirementJson`, `CancellationJson`, `ResultCode`, `SourceRefCode` |

**New in Plan 1 — the migration index:**

```sql
CREATE TABLE dbo.tbl_Rule_Catalog (
    RuleTableName   VARCHAR(80)  CONSTRAINT PK_Rule_Catalog PRIMARY KEY,
    EngineCode      VARCHAR(30)  NOT NULL,   -- consuming engine
    MethodCodes     VARCHAR(300) NOT NULL,   -- comma list of MethodCode values it defines
    Purpose         VARCHAR(400) NOT NULL,
    IntroducedIn    VARCHAR(40)  NOT NULL    -- migration / plan
);
```

Seeded for every `tbl_Rule_*` table. This is the one-page answer to "what does a port have to
reimplement."

## 10. `ChartPipeline` / `ChartBundle`

```csharp
namespace Ikiastrro.Core.Pipeline;

// pure, DB-free: given a resolved birth, run every engine and return everything computed
public sealed record ChartBundle(
    BirthDetails Birth,
    SiderealPositions Positions,
    SunTimes SunTimes,
    IReadOnlyList<ChartAnalysisInput> Charts,          // D1 + 20 vargas, incl. SpecialPoints
    IReadOnlyDictionary<string,string> CharaKarakaByPlanet,
    IReadOnlyList<PlanetaryStateFact> States);
    // P2+ extend: Dispositors, Strength, Yogas — additive record fields

public sealed class ChartPipeline
{
    public ChartBundle Run(BirthDetails birth);        // no I/O
}
```

`Ikiastrro.Data.ChartGenerationService` becomes: `pipeline.Run(birth)` → persist each part.
This lets a `verify-pipeline` CLI mode exercise the whole chain against the golden record with
no database.

## 11. Verification

- `dotnet build Ikiastrro.slnx` 0/0 (both `dotnet` and VS).
- **Unchanged golden values:** `verify-schema`, `verify-vargas`, `verify-functional-nature`,
  `verify-jaimini` — Plan 1's extraction is behaviour-preserving; Plan 0 touches no compute path.
- **Re-baselined (rename only, same numbers):** `verify-avastha` → new state names.
- **New in Plan 0:** `verify-sources` (every `SourceRefCode` used anywhere resolves in
  `tbl_Ref_Source`); `db/ikiastrro.sql` builds against `-v DbName=ikiastrro_scratch` unchanged;
  `SqlConnectionFactory` reads a bad env var → clear failure, reads the default → works.
- **New in Plan 1:** `verify-terminology` (every `tbl_Dim_*` / enum value has a `Code`; every
  `Code` has `sa` + `en` text; no orphan `ParentCode`); `verify-rules` (every `tbl_Rule_*` row
  has a valid `MethodCode` listed in `tbl_Rule_Catalog`; every `RuleParametersJson` parses and,
  for the 21 vargas, produces the same sign as its C# rule class; `tbl_Rule_Catalog` covers
  every `tbl_Rule_*` table); `verify-pipeline` (`ChartPipeline.Run` reproduces the
  `1_Ramakrishnan` `tbl_Chart_KeyDetails` set exactly).
- `recompute-keydetails` idempotent; scratch-DB rebuild of `db/ikiastrro.sql` clean; Web smoke
  `/charts/1` → 200 after each plan.

## 12. Plan scope — task groups

### Plan 0 — Foundations (docs + config; no engine logic; own spec-plan)

P0.1 **Doc taxonomy** — `STANDARDS.md §M.2` (four buckets + two indexes); create `docs/artifacts/`
     with `db/ ui/ diagrams/ reference-charts/`; move `docs/dotnet_engine_map.md` +
     `docs/research/*.d2` there; `docs/superpowers/specs/_TEMPLATE.md` +
     `plans/_TEMPLATE.md` (each carries a **Research status** block — see P0.3).
P0.2 **Citation registry** — `STANDARDS.md §M.3`; `tbl_Ref_Source` (migration, folded to
     baseline) + its mirror `docs/research/reference-sources.md`; seed the sources already used
     (BPHS, B.V. Raman *How to Judge a Horoscope*, Phaladeepika, PyJHora, Jagannatha Hora, Sanjay
     Rath); `verify-sources` (every `SourceRefCode` in any table resolves).
P0.3 **Research-status convention** — spec/plan template block:
     `Research: [x] complete | [ ] partial | [ ] not started` + a table of the `docs/research/*`
     docs each plan depends on, each with its own checkbox. `PRODUCT.md` feature rows carry the
     same flag so unresearched features are visible before scheduling.
P0.4 **`PRODUCT.md`** — feature catalogue (`FEAT-<AREA>-<NN>`), grouped by engine area, each with
     the 5-dimension checklist (DB · Core · Verify · Web · Docs) + Research flag + Spec/Plan
     links; a rollup table. Seeded from `docs/scope-*.md` + this spec's engine stack.
P0.5 **`INFRASTRUCTURE.md`** — §17: environments, DB-naming rule, config & secrets layering,
     per-environment migration policy.
P0.6 **Environment-agnostic data access** —
     `SqlConnectionFactory` takes the connection string from `IConfiguration`
     (`ConnectionStrings:Ikiastrro`), layered `appsettings.json` → `appsettings.{Environment}.json`
     → env var `ConnectionStrings__Ikiastrro`; CLI verify/recompute modes accept `--db <name>`
     (default `ikiastrro`); `db/ikiastrro.sql` header uses `:setvar DbName ikiastrro` /
     `USE [$(DbName)]`; the scratch-DB rebuild switches to `-v DbName=ikiastrro_scratch`.
     `verify-schema` and the Web smoke still green against the default.
P0.7 **Docs** — `STANDARDS.md` (§M.2/§M.3/§D.2), `master_ikiastrro.md`, `../ikiastrro.md`, memory.

### Plan 1 — Engine reorg + terminology + rule portability

1. **Namespace skeleton** — create `Engines/<Name>/` folders; move the pure-move files
   (namespace-only) engine by engine, building green after each.
2. **`ChartAnalyzer` split** — `HouseEngine` + `NakshatraEngine` + `RelationshipEngine` +
   `DignityEngine` + `CombustionEngine`; `Pipeline/ChartAnalyzer` composes them; `verify-schema`
   green.
3. **Avastha rename** — `PlanetaryStates` engine + the 6 table renames + `verify-avastha`
   re-baseline.
4. **`ChartPipeline` / `ChartBundle`** — Core façade; `ChartGenerationService` adopts it;
   `verify-pipeline`.
5. **Terminology tables** — migration 15 (`tbl_Astro_Terminology` + `_Text`); seed script
   (`sa`+`en`); `TerminologyCatalog` + `TerminologyCode`; `verify-terminology`; fold into
   `db/ikiastrro.sql`.
6. **Rule-table portability** — migration 16 (portability tail on existing `tbl_Rule_*`;
   `tbl_Rule_Catalog`; the new empty rule tables); backfill `RuleParametersJson` for
   `tbl_Rule_VargaScheme` from the 17 classes; `verify-rules`; fold into baseline.
7. **New-engine seams** — `IDispositorEngine`, `IStrengthEngine`, `IYogaEngine`,
   `ISthiraKarakaSource`, `INaisargikaKarakaSource` interfaces + `NotImplemented` stubs so
   `ChartBundle` compiles with the P2–P4 fields reserved.
8. **Downstream `using` sweep** — Cli / Data / Web; VS build 0/0.
9. **Docs** — `STANDARDS.md` §D.2 (naming) + §M.2 (doc taxonomy, see companion note),
   `ARCHITECTURE.md` engine section, `master_ikiastrro.md`, `../ikiastrro.md`, memory,
   regenerate `docs/dotnet_engine_map.md`.

## 13. Deferred scope (P2–P4) — reserved, not built here

| Plan | Engines / tables | Notes |
|---|---|---|
| **P2** | `DispositorEngine`; Sthira + Naisargika Karaka; Dīptādi + Lajjitādi Avastha; `tbl_Rule_HouseSignification` (migration 030), `tbl_Rule_Karaka` | Sapta-vs-Ashta Naisargika still an open product decision |
| **P3** | `StrengthEngine` — Ṣaḍbala (6), Iṣṭa/Kaṣṭa, Bhāva Bala, Vimśopaka; `tbl_Rule_ShadbalaComponent`, `tbl_Rule_VimsopakaWeight`, `tbl_Chart_Strength` | needs a cited edition for the lookup tables (BPHS ch. 27) |
| **P4** | `YogaEngine` — `PREDICATE_SET` DSL, `tbl_Rule_Yoga` catalogue, `tbl_Chart_Yoga` | first catalogue slice = Pañcha Mahāpuruṣa + 2–3 Rāja/Dhana; exercises the full chain |

## 14. Risks & mitigations

| Risk | Mitigation |
|---|---|
| Namespace churn breaks the build mid-move | Move one engine per commit, `dotnet build` gate each; the `using` sweep is last and mechanical |
| `ChartAnalyzer` split changes a value | Extraction only — no logic edits; `verify-schema` + `verify-vargas` are the tripwire; diff the pre/post `tbl_Chart_KeyDetails` for `1_Ramakrishnan` |
| Rule params in JSON drift from the C# | `verify-rules` asserts every `RuleParametersJson` round-trips through its interpreter and produces the same sign as the C# class, for all 21 vargas |
| Terminology seed is huge / error-prone | Generate it from the enums + Dim tables in code (`seed-terminology` CLI mode), not hand-authored SQL; `verify-terminology` closes the loop |
| Scope creep into P2–P4 | Non-goal §3 is explicit; P1 adds interfaces + empty tables only |

## 15. Open decisions (resolve before writing the plans)

1. **`Model` vs `Models`** namespace — rename to singular for consistency with `Reference` /
   `Pipeline`, or leave `Models` and accept the odd-one-out? *(recommend: leave `Models`;
   fewer downstream edits.)*
2. **`Dignity` as its own engine** vs. a sub-namespace of `Relationships`? *(recommend: own
   engine — Ṣaḍbala and Avastha both depend on it directly.)*
3. **`RuleParametersJson` column type** — `NVARCHAR(MAX)` now, or a real `JSON` type when the
   DB is on SQL Server 2025+? *(recommend: `NVARCHAR(MAX)` + `ISJSON` CHECK — portable.)*
4. **Terminology `Code` for the 108 padas** — `NAKPADA_ASHWINI_1` vs `NAKPADA_001`?
   *(recommend: `NAKPADA_<NAK>_<n>` — readable in a debugger.)*
5. **Environment / DB naming** — Option A (same catalog name `ikiastrro` on every environment;
   environments are separate servers/instances) vs. Option B (one server, catalog per env
   `ikiastrro_dev` / `_stage` / `_uat` / `_prod`). *(recommend: A as the principle, B as the
   single-server fallback — see §17. Either way: catalog name is config-only, never a literal
   in C# or SQL scripts, and no environment token ever appears in a `tbl_` / `vw_` / `usp_`
   name.)*
6. **Citation registry: table + doc, or doc only?** *(recommend: `tbl_Ref_Source` **and** the
   `.md` mirror — the table keeps portability consistent with the rule tables; the `.md` is the
   human-editable master, regenerated into the table by a seed mode.)*
7. **Does Plan 0 ship as one plan or split** (docs vs. the `SqlConnectionFactory`/`db/`
   config change)? *(recommend: one plan — the config change is ~3 files and belongs with the
   `INFRASTRUCTURE.md` that documents it.)*

## 16. Documentation & citation standard (Plan 0 — `STANDARDS.md §M.2` / §M.3)

### Four buckets + two indexes

| Bucket | Location | Contents | Naming |
|---|---|---|---|
| Research | `docs/research/` | domain research, competitive analysis, coverage gaps, book extracts, **all classical-source attribution** | `research-<slug>.md` |
| Artifacts | `docs/artifacts/{db,ui,diagrams,reference-charts}/` | non-prose inputs/outputs: DB diagrams, engine map, UI mockups shared by the user, JHora exports, D2 source + SVG, screenshots. **No inline "per B.V. Raman / BPHS" — cite `SRC_*` or link the research doc.** | `<kind>/<slug>.*` |
| Specs | `docs/superpowers/specs/` | dated per-feature design; header links Feature IDs + Research status | `YYYY-MM-DD-<topic>-design.md` |
| Plans | `docs/superpowers/plans/` | dated per-feature implementation plan; carries the Research-status block | `YYYY-MM-DD-<topic>.md` |
| *(unchanged)* Reference | `docs/reference-*.md` | stable "how we compute X" — not dated | `reference-<slug>.md` |

- **`master_ikiastrro.md`** — the doc map (every `.md` by category / status). Stays.
- **`PRODUCT.md`** (repo root, new) — the feature map. `FEAT-<AREA>-<NN>` (AREA = `EngineCode`).
  Per feature: Status ladder (`Planned → Designed → DB → Core → Verified → Web → Done`),
  a 5-box checklist (**DB · Core · Verify · Web · Docs**), a **Research** flag
  (`complete | partial | not started` + linked `docs/research/*`), Spec + Plan links, and the
  `verify-*` name. A rollup table gives "% done per area" and — by reading one checklist column —
  "how much DB / how much Web is left."

### Citation rule (`§M.3`)

- Every classical / third-party source is one row in `tbl_Ref_Source` (`Code`, `Title`,
  `Author`, `Edition`, `Tradition`, `Notes`) mirrored in `docs/research/reference-sources.md`.
- Anything that needs to cite a source — a `tbl_Rule_*` row (`SourceRefCode`), a
  `tbl_Astro_TerminologyText` row (`SourceRefCode`), a spec, a plan, a code comment — names the
  `SRC_*` code. Author / text / page strings live **only** in the registry.
- Artifacts (`docs/artifacts/*`) never carry attribution text; they link the relevant
  `docs/research/*` doc.

### Research-status block (spec & plan template)

```
## Research
Status: [ ] complete   [x] partial   [ ] not started
| Research doc | Needed for | Status |
|---|---|---|
| docs/research-topic-coverage.md | avastha slices 3–5 | partial — Dīptādi bands need a cited edition |
| docs/research-<new>.md | Ṣaḍbala lookup tables | not started |
```

A plan whose research is `partial` or `not started` is scheduled only after the gap is either
closed or explicitly accepted as in-scope for that plan.

## 17. Infrastructure & environments (Plan 0 — `INFRASTRUCTURE.md`)

### Environments

| Env | Purpose | Data | Deploy | Migrations |
|---|---|---|---|---|
| **dev** | local build + `verify-*` | 5 seed people | manual (`dotnet run`, `scripts/iis-setup.ps1`) | free — direct `sqlcmd`, baseline drop/rebuild allowed |
| **stage** | integration / pre-UAT | anonymised sample | CI or scripted publish | numbered `db/NN_*.sql` only, ledgered in `SchemaMigrations`; **no baseline drop** |
| **uat** | user acceptance | UAT dataset, refreshed from stage | scripted publish | as stage |
| **prod** | live | real | gated scripted publish + backup first | as stage; `SchemaMigrations` is the source of truth |

### Database-naming rule

- **Principle (Option A):** the catalog is always `ikiastrro`. Environments differ by
  **server / instance**, supplied entirely by configuration:
  `dev = localhost` · `stage = <stage-sql>` · `uat = <uat-sql>` · `prod = <prod-sql>`.
  `db/ikiastrro.sql` applies **identically** to every environment.
- **Single-server fallback (Option B):** catalog per env — `ikiastrro`, `ikiastrro_stage`,
  `ikiastrro_uat`, `ikiastrro_prod` — the name coming from `ConnectionStrings:Ikiastrro`'s
  `Initial Catalog`, never a literal.
- **Hard rule, both options:** an environment token **never** appears in a `tbl_` / `vw_` /
  `usp_` / `tvf_` / constraint / index name. The environment boundary is the catalog (or the
  server), never a schema-object suffix — so switching environments is zero code and zero schema
  change.

### Config & secrets

| Layer | Holds | Committed? |
|---|---|---|
| `appsettings.json` | dev defaults — `Server=localhost;Database=ikiastrro;Trusted_Connection=True` | yes |
| `appsettings.{Environment}.json` | non-secret per-env overrides (server name) | yes (no secrets) |
| env var `ConnectionStrings__Ikiastrro` / user-secrets / Key Vault | stage/uat/prod full connection string incl. credentials | **no** |

`ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT` selects the layer. The CLI honours the same
`IConfiguration` chain plus a `--db` override for one-off targeting.

### Migration application policy

- **dev:** `sqlcmd -v DbName=ikiastrro -i db/ikiastrro.sql` (fresh) or the numbered scripts;
  scratch-DB rebuild for a from-empty check.
- **stage / uat / prod:** apply the numbered `db/NN_*.sql` chain in order; each self-records in
  `dbo.SchemaMigrations`; never run `db/ikiastrro.sql` (the baseline) against a populated
  higher environment. A release = the set of `NN_*.sql` since the last deployed number.
- Baseline `db/ikiastrro.sql` stays the "fresh install / dev" artifact and the folding target;
  the numbered chain is the "promote a change" artifact.

### Version control & repository hygiene (Plan 0 addendum, 2026-09-02)

`INFRASTRUCTURE.md` also carries a **Version control & repository hygiene** section: the
tracked-vs-`.gitignore` rule (build output / `_research/` / `.superpowers/` / `/scratch/` /
CLI stdout redirects / `db/*.ipynb` ignored; `src/`, `db/` baseline+numbered+`_archive/`,
`docs/**` incl. `docs/artifacts/**` tracked), how `docs/artifacts/**` reaches `master` (ordinary
tracked content, binaries committed inline, git-lfs only above a few MB), the FF-only
`feat/* → master` promotion runbook for the two remotes (`origin`, `ikijunar`), a pre-public-push
secret/clean-tree checklist, and the still-pending `db/` history scrub.

Also landed in Plan 0's cleanup: the `vedic_horo_gen` / `VedicHoroGen` project name (renamed
2026-08-30) was retired from the last live code spots (CLI banner, Nominatim User-Agent, a Web
comment) and 9 living docs; the JHora golden-record exports moved `scratch/` →
`docs/artifacts/reference-charts/` and are now tracked; the 6 `docs/reference-chart-*.md`
guides (indexed as living, never committed) were added; `db/15_create_dim_source.sql`'s
`PRINT` (an inline scalar subquery, rejected by SQL Server) was fixed.
