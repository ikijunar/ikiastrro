# Vedic Astrology Reference Books

**Status:** Living research note  
**Updated:** 2026-09-06  
**Purpose:** Identify modern authors and books that can extend the project's P.V.R. Narasimha Rao and B. V. Raman reference base without merging distinct traditions into one calculation rule set.

## Local collection reviewed

The collection at `D:\Vedic Astrology\Vedic Astology Books` already includes material by:

- **K. N. Rao** — predictive techniques, Vimshottari and Jaimini dashas, marriage, profession, and applied case studies.
- **Sanjay Rath** — *Crux of Vedic Astrology*, *Varga Chakra*, *Vimsottari and Udu Dashas*, and *Collected Papers in Vedic Astrology*.
- **V. K. Choudhry** — *How to Study Divisional Charts* and Systems' Approach material.
- **H. R. Seshadri Iyer**, **R. G. Rao**, **P. Trivedi**, **Hart de Fouw**, and **Robert Svoboda**.

The folder also contains the principal classical and modern sources already used by the project, including P.V.R. Narasimha Rao, B. V. Raman, BPHS translations, Jataka Parijata, Saravali, Jaimini texts, and several works on vargas, dashas, nakshatras, and predictive techniques.

## Top three reference choices

| Rank | Author | In local collection? | Recommended role |
|---|---|---:|---|
| 1 | **Sanjay Rath** | Yes | Secondary reference for Jaimini, vargas, special lagnas, Narayana and conditional dashas, Rudramsa, nakshatras, and practical case studies. |
| 2 | **K. N. Rao** | Yes | Independent predictive cross-check for Parashari and Jaimini timing, Vimshottari, Chara/Sthira/Navamsha dashas, marriage, profession, and empirical chart analysis. |
| 3 | **Ernst Wilhelm** | No | Recommended new acquisition for systematic and computationally explicit Parashara–Jaimini rules, with an unusually direct connection to astrology software. |

### 1. Sanjay Rath

Rath is the closest additional reference to the current PVR-first direction. His published bibliography includes divisional charts, dashas, nakshatras, Rudramsa, Hora Lagna, Sarvatobhadra Chakra, and case-based teaching. His stated foundations include BPHS, Jaimini Upadesa Sutras, Brihat Jataka, and Saravali.

Source: [Sanjay Rath bibliography](https://srath.com/misc/books/) and [background](https://srath.com/about/about/).

### 2. K. N. Rao

Rao is valuable as an independent predictive and timing corpus. His catalogue covers Vimshottari, Chara, Sthira, Navamsha, Mandook, marriage, profession, and other applied Jaimini topics. His material is often case-study driven, so it is best used for validation and interpretation review rather than copied directly into executable rules.

Source: [Vani Publications K. N. Rao catalogue](https://www.vanipublications.com/author_books.php?author_name=k+n+rao).

### 3. Ernst Wilhelm

Wilhelm is the strongest new author to add for this software project. His published approach explicitly presents Parashara and Jaimini astrology as systematic and mathematically precise, and his work includes books, courses, and software. This makes him useful for checking whether a rule can be expressed clearly enough to calculate, test, and explain.

Source: [Ernst Wilhelm's official site](https://www.vedic-astrology.net/).

## Important comparison source

**V. K. Choudhry** should remain a comparison source rather than a core authority. His Systems' Approach is structured and useful for testing house-lord, profession, and divisional-chart reasoning, but it is a distinct methodology. Any implementation based on it should carry its own method and source tags.

The local folder includes *How to Study Divisional Charts*. A publisher copy describes Choudhry as the propounder of the Systems' Approach for interpreting horoscopes.

Source: [How to Study Divisional Charts](https://storage.yandexcloud.net/j108/library/su8aqi22/V.K._Choudhry_-_How_To_Study_Divisional_Charts.pdf).

## Recommended source hierarchy for ikiastrro

1. **P.V.R. Narasimha Rao** — primary implementation baseline.
2. **B. V. Raman** — classical and interpretive cross-check.
3. **Sanjay Rath** — Jaimini, varga, special-point, and advanced timing comparison.
4. **K. N. Rao** — empirical predictive validation and case-study review.
5. **Ernst Wilhelm** — systematic and computational secondary model.
6. **V. K. Choudhry** — explicitly tagged alternative system.

These sources should not be merged into one undifferentiated rule set. Each rule should retain a source and method identity so that differences can be compared later with JHora and corrected without changing the PVR baseline silently.

## Suggested use in the project

- Use Rath for Jaimini, divisional-chart, special-lagna, Rudramsa, and advanced dasha research.
- Use Rao for independent chart examples, timing validation, and predictive interpretation review.
- Add Wilhelm as a new research source when a formal, systematic treatment is needed or when a rule must be made computationally explicit.
- Keep Choudhry rules isolated under a Systems' Approach method code.
- Record disagreements as source-specific deviations rather than treating one author's wording as a universal correction.

