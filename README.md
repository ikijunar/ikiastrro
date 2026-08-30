# ikiastrro

> **An open-source astrology computation engine for researchers and developers.**

ikiastrro is a research-oriented software project for computational astrology, developed by [ikijunar](https://github.com/ikijunar).

The project begins with **Vedic astrology (Jyotisha)** and focuses on the computational foundations behind charts, planetary positions, house systems, timing techniques, and related astrological calculations.

Rather than treating astrology software as a black box that simply produces a horoscope, ikiastrro aims to make the underlying calculations **transparent, reproducible, testable, and extensible**.

> 📄 **Current implementation status & known limitations:** see [`ARCHITECTURE.md`](ARCHITECTURE.md).

## Why ikiastrro?

Astrological software sits at the intersection of astronomy, mathematics, calendrical systems, traditional rules, and software engineering.

For researchers and developers, it is important to be able to answer questions such as:

- What astronomical data and conventions were used?
- Which ayanamsa or reference system was selected?
- How was a planetary position calculated?
- How were houses and divisional charts derived?
- Which rules produced a particular result?
- Can the calculation be reproduced independently?
- Can the result be tested against another implementation?

ikiastrro is being designed around these questions.

## Core principles

### 🔬 Reproducibility

Calculations should be deterministic and reproducible when the same inputs, configuration, astronomical data, and calculation conventions are used.

### 🎯 Accuracy

The project aims to use clearly defined astronomical and mathematical methods, with explicit documentation of assumptions and configuration choices.

### 🔎 Transparency

Important calculations should not disappear behind opaque results. Where practical, the software should expose the inputs, conventions, intermediate values, and methods that lead to an output.

### 🧪 Verification

Results should be testable against known references, independent implementations, and carefully constructed test cases.

### 🧩 Extensibility

The architecture should allow new calculation methods, astrological techniques, chart types, and traditions to be added without making the core system unnecessarily rigid.

### 🌐 Open source

The source code is available so developers and researchers can inspect the implementation, reproduce results, contribute improvements, and build applications on top of the computational foundation.

## Vedic astrology / Jyotisha

Vedic astrology is the project's initial domain of focus.

The long-term objective is not to restrict ikiastrro to a single application or user interface, but to develop a reusable computational foundation for astrology.

Potential areas include:

- Birth chart generation
- Planetary and celestial calculations
- Signs and nakshatras
- House calculations
- Divisional charts (Varga)
- Dashas and timing systems
- Planetary relationships and dignities
- Transits
- Ayanamsa and reference-system handling
- Multiple calculation conventions
- Comparative and verification tools

Features will be documented as they are implemented and validated.

## Architecture

The project is intended to separate the computational engine from presentation and application layers wherever practical.

This makes it possible to use the same underlying calculations in:

- Desktop applications
- Web applications
- APIs
- Research tools
- Data-analysis workflows
- Automated testing
- Future user interfaces

The current implementation may evolve as the architecture is refined. The repository documentation should therefore be considered the authoritative description of the currently supported capabilities.

## Accuracy and verification

Astrology software can produce different results when implementations use different:

- Ephemeris data
- Astronomical models
- Ayanamsa definitions
- House-system definitions
- Time-zone or historical time data
- Rounding conventions
- Traditional calculation rules

ikiastrro intends to make these choices explicit rather than silently hiding them.

**A result is more useful when its computational provenance can be understood.**

The project therefore values documented assumptions, automated tests, reference cases, and comparison with independent implementations.

## Project status

ikiastrro is an evolving open-source project.

Some areas may be experimental or under active development. Until a calculation or feature is documented and covered by appropriate validation, it should not be assumed to be production-ready.

## Roadmap

The roadmap will evolve with the project, but the broader direction includes:

- Establishing a reliable astronomical calculation foundation
- Defining a clean computational domain model
- Implementing core Vedic astrology calculations
- Building comprehensive automated test suites
- Supporting configurable calculation conventions
- Adding reference and comparison tooling
- Improving documentation and reproducibility
- Providing developer-friendly APIs
- Expanding support for additional astrological systems where appropriate

## Contributing

Contributions, technical discussion, validation cases, bug reports, documentation improvements, and research-oriented collaboration are welcome.

When contributing calculation logic, please provide enough information for the result to be independently understood and tested.

In particular, contributions involving astronomical or astrological calculations should document:

1. The source or tradition being implemented
2. The mathematical or computational method
3. Relevant configuration or conventions
4. Reference results where available
5. Tests covering the implementation

## License

ikiastrro is open source. See the repository's license file for the terms under which the software may be used, modified, and distributed.

## About ikijunar

ikiastrro is developed under **ikijunar**, an independent software company inspired by the idea of building software around meaningful pursuits.

**IKI** is inspired by *Ikigai* — purpose and meaningful work.

**ikiastrro** is the first expression of that idea: building an open computational foundation for astrology.

---

**ikijunar**  
*Purpose. Curiosity. Software.*
