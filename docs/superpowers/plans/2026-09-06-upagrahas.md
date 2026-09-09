---
last_updated: 2026-09-06
---

# Upagraha implementation

Objective: calculate all 11 upagrahas from migration 27's PVR rules, including Gulika at Saturn's midpoint and Maandi at its start. Preserve existing ayanamsa and UI edits.

Branch: `chore/github-pm-templates`, shared checkout. Existing dirty files inspected; no reset or staging of unrelated work.

Plan: add validated Core rule models and calculator; load rules in Data and inject into CLI/Web orchestration; update point rendering and verification; run build/tests; reconcile current-state docs.

Compatibility: stored charts retain their previous results until explicitly regenerated. Do not mutate existing versioned rule rows. All points use the existing special-point varga projection path.

Status: implementation and verification complete; no database writes, commits, or publishing performed.

Changed files: new `SubPlanetRuleSet`, `SubPlanetCalculator`, `SubPlanetRuleRepository`; integration in `SpecialPointCalculator`, `ChartCalculationOrchestrator`, CLI/Web `Program.cs`; PVR pair compatibility in `UpagrahaCalculator`; template grouping in Workspace, SouthIndianTemplate and TemplateChartCard; explanatory comments/terminology seed; seven new cases in `SubPlanetCalculatorTests`; architecture/product/calculation/coverage docs and master index.

Verification:
- `dotnet build Ikiastrro.slnx --no-restore -v quiet`: passed, zero warnings/errors.
- `dotnet test tests/Ikiastrro.Web.Tests/Ikiastrro.Web.Tests.csproj --no-restore -v quiet`: 13 passed (7 new calculation cases, 6 existing UI cases).
- `dotnet run --project src/Ikiastrro.Cli --no-restore -- verify-upagrahas`: passed against live rule rows; all 11 points in all 21 charts, Sun identity, PVR pair agreement, pre-dawn sample.
- `git diff --check`: passed (existing line-ending notices only).
- Initial CLI fixture used an unsupported leading plus in UtcOffset; corrected to `05:30` before the successful verification.

Operational boundary: existing saved charts have not been regenerated, and saved-chart `verify-jaimini` was not run. Its expectations now use PVR names; regenerate stored charts before using it to validate this convention. No browser visual inspection was performed. Existing Core callers that omit the optional rule set retain the two-point API; CLI/Web inject all rules. New result EngineVersion stamps include the PVR rule-set identifier. Original migration notes remain historical snapshots.

Next step, if adopting the new convention in saved data: regenerate charts through the normal generation workflow, then run saved-chart verification. No implementation blocker.
