using System.Globalization;
using VedicHoroGen.Core.Astro;
using VedicHoroGen.Core.Calculators;
using VedicHoroGen.Core.Dasha;
using VedicHoroGen.Core.Geocoding;
using VedicHoroGen.Core.Models;
using VedicHoroGen.Core.Transits;
using VedicHoroGen.Data;

void PrintDashaTree(IReadOnlyList<DashaPeriodRecord> roots, int maxLevel = 2)
{
    void PrintLevel(IEnumerable<DashaPeriodRecord> periods, int depth)
    {
        foreach (var period in periods.OrderBy(p => p.StartDayOffset))
        {
            var label = period.LevelNumber switch { 1 => "Mahadasha", 2 => "Antardasha", _ => "Pratyantardasha" };
            Console.WriteLine($"{new string(' ', depth * 2)}{period.Lord,-8} {label,-16} {period.StartDate:yyyy-MM-dd} -> {period.EndDate:yyyy-MM-dd}");
            if (period.LevelNumber < maxLevel)
                PrintLevel(period.Children, depth + 1);
        }
    }
    PrintLevel(roots, 0);
}

void PrintDashaTreeFromComputed(IReadOnlyList<DashaPeriod> roots, int maxLevel = 2)
{
    void PrintLevel(IEnumerable<DashaPeriod> periods, int depth)
    {
        foreach (var period in periods.OrderBy(p => p.StartDayOffset))
        {
            var label = period.LevelNumber switch { 1 => "Mahadasha", 2 => "Antardasha", _ => "Pratyantardasha" };
            Console.WriteLine($"{new string(' ', depth * 2)}{period.Lord,-8} {label,-16} {period.StartDate:yyyy-MM-dd} -> {period.EndDate:yyyy-MM-dd}");
            if (period.LevelNumber < maxLevel)
                PrintLevel(period.Children, depth + 1);
        }
    }
    PrintLevel(roots, 0);
}

Console.WriteLine("=== vedic_horo_gen ===");
Console.WriteLine("Enter birth details to generate and store D1 (Rasi) + D9 (Navamsa) charts.\n");

string ReadRequired(string prompt)
{
    while (true)
    {
        Console.Write($"{prompt}: ");
        var value = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        Console.WriteLine("  This field is required.");
    }
}

DateOnly ReadDate(string prompt)
{
    while (true)
    {
        var input = ReadRequired($"{prompt} (yyyy-MM-dd)");
        if (DateOnly.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        Console.WriteLine("  Invalid date format. Example: 1981-04-22");
    }
}

TimeOnly ReadTime(string prompt)
{
    while (true)
    {
        var input = ReadRequired($"{prompt} (HH:mm, 24-hour)");
        if (TimeOnly.TryParseExact(input, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return time;
        Console.WriteLine("  Invalid time format. Example: 05:30");
    }
}

// --- Standard input ---
var connectionFactory = SqlConnectionFactory.CreateDefault();
var birthDetailsRepo = new BirthDetailsRepository(connectionFactory);

// --- Shared compute-and-store pipeline (Task 6/7): one instance, reused by every one-off mode
//     below and by the interactive add flow near the bottom of this file. ---
var orchestrator = ChartCalculationOrchestrator.CreateDefault();
var chartResultsRepo = new ChartResultsRepository(connectionFactory);
var vimshottariDashaService = new VimshottariDashaService(
    chartResultsRepo, new DashaPeriodsRepository(connectionFactory));
var chartGenerationService = new ChartGenerationService(
    orchestrator, vimshottariDashaService,
    chartResultsRepo,
    new ChartKeyDetailsRepository(connectionFactory),
    new ChartHouseLordsRepository(connectionFactory),
    new ChartConjunctionsRepository(connectionFactory),
    new ChartAspectsRepository(connectionFactory));

// --- One-off backfill mode: `dotnet run -- backfill-analytics` ---
// Recomputes KeyDetails/HouseLords/Conjunctions/Aspects for every stored ChartResult that doesn't
// have any yet (e.g. D9 rows for people saved before D9 got the same analytical tables as D1,
// 2026-08-24). Recomputes from Swiss Ephemeris (via the same calculator that originally produced the
// chart) rather than round-tripping the stored ResultJson — old D9 rows predate NirayanaLongitudeDegrees
// being in their JSON at all, and recomputing is deterministic (same birth data -> same signs/houses)
// so it's no less correct. Safe to re-run (skips ChartResults that already have KeyDetails rows).
if (args.Length > 0 && args[0] == "backfill-analytics")
{
    Console.WriteLine("Recomputing analytics (KeyDetails/HouseLords/Conjunctions/Aspects) for every saved person...");
    foreach (var person in birthDetailsRepo.GetAll())
    {
        var personReport = chartGenerationService.RecomputeAnalytics(person, chartTypeFilter: null);
        Console.WriteLine($"  {person.Name}: recomputed analytics for [{string.Join(", ", personReport.ChartTypesWritten)}]");
    }
    return;
}

// --- One-off check: `dotnet run -- verify-vargas` ---
// Repeatable worked-example assertions for the pure divisional-chart + nakshatra helpers in
// AstroMath (this solution has no unit-test project). Longitudes are absolute sidereal degrees.
if (args.Length > 0 && args[0] == "verify-vargas")
{
    var failures = 0;
    void Check(string label, object actual, object expected)
    {
        var ok = actual.ToString() == expected.ToString();
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    // D2 Hora — classical two-sign (Sun's hora = Leo, Moon's hora = Cancer)
    Check("D2 Aries 10",  AstroMath.GetHoraSign(10),  ZodiacName.Leo);
    Check("D2 Aries 20",  AstroMath.GetHoraSign(20),  ZodiacName.Cancer);
    Check("D2 Taurus 10", AstroMath.GetHoraSign(40),  ZodiacName.Cancer);
    Check("D2 Taurus 20", AstroMath.GetHoraSign(50),  ZodiacName.Leo);

    // D6 Shashtamsa — odd: Aries..Virgo; even: Libra..Pisces (source sign ignored)
    Check("D6 Aries 2",   AstroMath.GetShashtamsaSign(2),   ZodiacName.Aries);
    Check("D6 Aries 27",  AstroMath.GetShashtamsaSign(27),  ZodiacName.Virgo);
    Check("D6 Taurus 2",  AstroMath.GetShashtamsaSign(32),  ZodiacName.Libra);
    Check("D6 Taurus 27", AstroMath.GetShashtamsaSign(57),  ZodiacName.Pisces);

    // D10 Dasamsa — odd: from sign; even: from 9th sign
    Check("D10 Aries 1",   AstroMath.GetDasamsaSign(1),   ZodiacName.Aries);
    Check("D10 Aries 28",  AstroMath.GetDasamsaSign(28),  ZodiacName.Capricornus);
    Check("D10 Taurus 1",  AstroMath.GetDasamsaSign(31),  ZodiacName.Capricornus);
    Check("D10 Taurus 28", AstroMath.GetDasamsaSign(58),  ZodiacName.Libra);

    // D11 Rudramsa — (12 - signIndex + part) % 12, part width 30/11
    Check("D11 Aries 1",   AstroMath.GetRudramsaSign(1),   ZodiacName.Aries);
    Check("D11 Aries 29",  AstroMath.GetRudramsaSign(29),  ZodiacName.Aquarius);
    Check("D11 Taurus 1",  AstroMath.GetRudramsaSign(31),  ZodiacName.Pisces);
    Check("D11 Taurus 29", AstroMath.GetRudramsaSign(59),  ZodiacName.Capricornus);

    // Nakshatra name canon — must match tbl_Nakshatras.NakshatraName exactly
    Check("Name idx0",  AstroMath.GetNakshatraName(ConstellationName.Aswini),   "Ashwini");
    Check("Name idx7",  AstroMath.GetNakshatraName(ConstellationName.Pushyami), "Pushya");
    Check("Name idx13", AstroMath.GetNakshatraName(ConstellationName.Chitta),   "Chitra");
    Check("Name idx21", AstroMath.GetNakshatraName(ConstellationName.Sravana),  "Shravana");
    Check("Name idx11", AstroMath.GetNakshatraName(ConstellationName.Uttara),   "Uttara Phalguni");

    // Overall pada index (0..107): 3°20' each
    Check("Pada@0",     AstroMath.GetOverallPadaIndex(0),      0);
    Check("Pada@10",    AstroMath.GetOverallPadaIndex(10),     3);   // Ashwini pada 4
    Check("Pada@218.7", AstroMath.GetOverallPadaIndex(218.72), 65); // Anuradha pada 2 -> slot 16*4+1 = 65

    // Sub-lord — Anuradha (idx 16, lord Saturn), 5.389° into nakshatra -> Venus slot
    Check("Sub@218.72", AstroMath.GetNakshatraSubLord(218.72), PlanetName.Venus);
    Check("Sub@0",      AstroMath.GetNakshatraSubLord(0),      PlanetName.Ketu);   // Ashwini's own lord first

    Console.WriteLine(failures == 0 ? "\nverify-vargas: ALL PASS" : $"\nverify-vargas: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-functional-nature` ---
// Worked-example assertions for LagnaFunctionalNature (Parashari functional benefic/malefic
// heuristic + yogakaraka detection). Solution has no unit-test project.
if (args.Length > 0 && args[0] == "verify-functional-nature")
{
    var failures = 0;
    void Check(string label, object actual, object expected)
    {
        var ok = actual.ToString() == expected.ToString();
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }
    string Houses(FunctionalNatureResult r) => "{" + string.Join(",", r.RuledHouses) + "}";

    var taSa = LagnaFunctionalNature.For(ZodiacName.Taurus, PlanetName.Saturn);
    Check("Taurus/Saturn nature", taSa.Nature, FunctionalNature.Yogakaraka);
    Check("Taurus/Saturn houses", Houses(taSa), "{9,10}");

    var arMe = LagnaFunctionalNature.For(ZodiacName.Aries, PlanetName.Mercury);
    Check("Aries/Mercury nature", arMe.Nature, FunctionalNature.Malefic);
    Check("Aries/Mercury houses", Houses(arMe), "{3,6}");

    var cnMo = LagnaFunctionalNature.For(ZodiacName.Cancer, PlanetName.Moon);
    Check("Cancer/Moon nature", cnMo.Nature, FunctionalNature.Neutral);   // Moon as Lagna lord
    Check("Cancer/Moon houses", Houses(cnMo), "{1}");

    Check("Libra/Saturn",     LagnaFunctionalNature.For(ZodiacName.Libra, PlanetName.Saturn).Nature,      FunctionalNature.Yogakaraka);
    Check("Cancer/Mars",      LagnaFunctionalNature.For(ZodiacName.Cancer, PlanetName.Mars).Nature,       FunctionalNature.Yogakaraka);
    Check("Leo/Mars",         LagnaFunctionalNature.For(ZodiacName.Leo, PlanetName.Mars).Nature,          FunctionalNature.Yogakaraka);
    Check("Capricornus/Venus",LagnaFunctionalNature.For(ZodiacName.Capricornus, PlanetName.Venus).Nature, FunctionalNature.Yogakaraka);
    Check("Aquarius/Venus",   LagnaFunctionalNature.For(ZodiacName.Aquarius, PlanetName.Venus).Nature,    FunctionalNature.Yogakaraka);

    var arJu = LagnaFunctionalNature.For(ZodiacName.Aries, PlanetName.Jupiter);
    Check("Aries/Jupiter nature", arJu.Nature, FunctionalNature.Benefic);   // rules 9th (trikona) + 12th
    Check("Aries/Jupiter houses", Houses(arJu), "{9,12}");

    var arMo = LagnaFunctionalNature.For(ZodiacName.Aries, PlanetName.Moon);
    Check("Aries/Moon nature", arMo.Nature, FunctionalNature.Malefic);      // natural benefic owning only the 4th
    Check("Aries/Moon kendradhipati", arMo.KendradhipatiDosha, true);

    var arVe = LagnaFunctionalNature.For(ZodiacName.Aries, PlanetName.Venus);
    Check("Aries/Venus maraka", arVe.IsMaraka, true);                       // rules 2nd (Taurus) + 7th (Libra)
    Check("Aries/Venus nature", arVe.Nature, FunctionalNature.Malefic);     // falls through to the catch-all

    Console.WriteLine(failures == 0 ? "\nverify-functional-nature: ALL PASS" : $"\nverify-functional-nature: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off backfill mode: `dotnet run -- recompute-keydetails` ---
// Re-derives tbl_Chart_KeyDetails rows for every stored ChartResult that has a registered
// IChartCalculator (D1, D2, D6, D9, D10, D11 — everything except VimshottariDasha), even ones that
// already have rows — unlike backfill-analytics (which only fills in what's missing), this one
// always deletes + reinserts, so it picks up new columns added to an existing row shape
// (IsRetrograde/IsCombust/NakshatraLordPlanet, 2026-08-28; NakshatraId/NakshatraPadaId/
// NakshatraSubLordPlanet + canonical nakshatra names, 2026-08-30). HouseLords/Conjunctions/Aspects
// are untouched — none of the new columns live there. Safe to re-run any time.
if (args.Length > 0 && args[0] == "recompute-keydetails")
{
    Console.WriteLine("Re-deriving analytics for every saved person.");
    Console.WriteLine("NOTE: as of Task 7 this recomputes ALL 4 analytics tables (KeyDetails/HouseLords/Conjunctions/");
    Console.WriteLine("Aspects), not KeyDetails only — an intentional, idempotent no-op widening now that the write");
    Console.WriteLine("path is single-sourced through ChartGenerationService.RecomputeAnalytics.");
    foreach (var person in birthDetailsRepo.GetAll())
    {
        var personReport = chartGenerationService.RecomputeAnalytics(person, chartTypeFilter: null);
        Console.WriteLine($"  {person.Name}: re-derived [{string.Join(", ", personReport.ChartTypesWritten)}]");
    }
    return;
}

// --- One-off backfill mode: `dotnet run -- backfill-charts` ---
// Creates every registered chart type that a saved person is missing a tbl_ChartResults row for,
// plus its full analytics (KeyDetails/HouseLords/Conjunctions/Aspects) via ChartAnalyzer. Sibling
// of backfill-dasha: idempotent (skips (person, ChartType) pairs that already exist), and generic —
// when D3/D7/D12/... are registered later this picks them up with no change. Use after adding a new
// IChartCalculator (e.g. D2/D6/D10/D11, 2026-08-30).
if (args.Length > 0 && args[0] == "backfill-charts")
{
    Console.WriteLine("Creating any missing chart types (+ Vimshottari Dasha) for every saved person...");
    foreach (var person in birthDetailsRepo.GetAll())
    {
        var personReport = chartGenerationService.GenerateMissing(person);
        Console.WriteLine(personReport.ChartTypesWritten.Count == 0 && !personReport.DashaWritten
            ? $"  {person.Name}: nothing missing"
            : $"  {person.Name}: created [{string.Join(", ", personReport.ChartTypesWritten)}]{(personReport.DashaWritten ? " + Dasha" : "")}");
    }
    return;
}

// --- One-off mode: `dotnet run -- list-rule-sets` ---
// Lists every tbl_Rule_Sets row -- which named classical schemes exist, which is active.
if (args.Length > 0 && args[0] == "list-rule-sets")
{
    var ruleSetRepoForList = new RuleSetRepository(connectionFactory);
    var ruleSets = ruleSetRepoForList.GetAll();
    var active = ruleSetRepoForList.GetActive();
    foreach (var rs in ruleSets)
    {
        var marker = rs.Id == active.Id ? " [ACTIVE]" : "";
        Console.WriteLine($"{rs.Id}: {rs.RuleSetName}{marker}");
        if (rs.Description is not null) Console.WriteLine($"   {rs.Description}");
    }
    return;
}

// --- One-off mode: `dotnet run -- show-rules <rule-set-id>` ---
// Prints every aspect offset / combustion orb / natural relationship for one rule set -- lets
// you eyeball the full ruleset without opening SSMS, and cross-check a new ruleset's rows
// against the classical text they're meant to encode before ever wiring it into ChartAnalyzer.
if (args.Length > 1 && args[0] == "show-rules" && int.TryParse(args[1], out var ruleSetIdArg))
{
    Console.WriteLine($"--- Aspect offsets (RuleSetId={ruleSetIdArg}) ---");
    foreach (var (planet, offsets) in new AspectRuleRepository(connectionFactory).GetOffsets(ruleSetIdArg))
        Console.WriteLine($"  {planet,-8} {string.Join(", ", offsets.Select(o => $"{o}th"))}");

    Console.WriteLine($"\n--- Combustion orbs (RuleSetId={ruleSetIdArg}) ---");
    foreach (var (planet, orbs) in new CombustionRuleRepository(connectionFactory).GetOrbs(ruleSetIdArg))
        Console.WriteLine($"  {planet,-8} direct={orbs.Direct}°  retrograde={(orbs.Retrograde is { } r ? $"{r}°" : "(same as direct)")}");

    Console.WriteLine($"\n--- Natural relationships (RuleSetId={ruleSetIdArg}) ---");
    foreach (var (planet, rel) in new NaturalRelationshipRuleRepository(connectionFactory).GetRelationships(ruleSetIdArg))
        Console.WriteLine($"  {planet,-8} Friends=[{string.Join(",", rel.Friends)}] Neutrals=[{string.Join(",", rel.Neutrals)}] Enemies=[{string.Join(",", rel.Enemies)}]");
    return;
}

// --- One-off mode: `dotnet run -- precheck-planet-transits` ---
// Runs the sign-boundary-crossing walk (PlanetTransitEventFinder) over a recent ~10-year window
// only, printing results without touching the database -- for spot-checking against a published
// Vedic (sidereal, Lahiri) source BEFORE trusting the full 1930-2060 backfill. Deliberately does
// NOT auto-compare against hardcoded "known good" dates: Vedic sidereal ingress dates run ~23-24
// days behind the tropical dates most Western news sources publish (current ayanamsha ~24 degrees),
// so a hardcoded reference risks comparing against the wrong system entirely. Print + manual/
// external cross-check is the safer verification step here.
if (args.Length > 0 && args[0] == "precheck-planet-transits")
{
    var start = new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var end = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
    Console.WriteLine($"Precheck: walking {start:yyyy-MM-dd} to {end:yyyy-MM-dd} for {string.Join(", ", PlanetTransitEventFinder.TrackedPlanets)} (sidereal/Lahiri) -- no database writes.\n");

    var allEvents = PlanetTransitEventFinder.FindCrossings(start, end);
    foreach (var planet in PlanetTransitEventFinder.TrackedPlanets)
    {
        var planetEvents = allEvents.Where(e => e.Planet == planet).OrderBy(e => e.EventDateTimeUtc).ToList();
        var marked = PlanetTransitEventFinder.MarkReentries(planetEvents);
        Console.WriteLine($"--- {planet} ({planetEvents.Count} crossing(s)) ---");
        foreach (var (evt, isReentry) in marked)
        {
            var reentryFlag = isReentry ? "  [re-entry]" : "";
            var direction = evt.IsRetrograde ? "R" : "D";
            Console.WriteLine($"  {evt.EventDateTimeUtc:yyyy-MM-dd HH:mm} UTC -> enters {evt.Sign} ({direction}){reentryFlag}");
        }
        Console.WriteLine();
    }
    Console.WriteLine("Cross-check these against a Vedic sidereal (Lahiri) Gochar source (e.g. DrikPanchang/Prokerala's Vedic transit calendar, not tropical/Western sun-sign dates) before running backfill-planet-transits.");
    return;
}

// --- One-off backfill mode: `dotnet run -- backfill-planet-transits` ---
// Full 1930-2060 sign-boundary-crossing backfill for Saturn/Jupiter/Rahu into
// tbl_PlanetSignTransitEvents (point 2, docs/vedic-reference-tables.md). Ketu is never
// stored -- always derived from Rahu via vw_KetuSignTransitEvents. Idempotent per planet: skips
// any tracked planet that already has rows, so it's safe to re-run (e.g. after adding Mars later).
if (args.Length > 0 && args[0] == "backfill-planet-transits")
{
    var transitRepo = new PlanetSignTransitEventsRepository(connectionFactory);
    var alreadyPopulated = PlanetTransitEventFinder.TrackedPlanets.Where(p => transitRepo.CountByPlanet(p) > 0).ToList();
    if (alreadyPopulated.Count == PlanetTransitEventFinder.TrackedPlanets.Count)
    {
        Console.WriteLine("All tracked planets already have transit events stored -- nothing to do.");
        return;
    }
    if (alreadyPopulated.Count > 0)
    {
        Console.WriteLine($"Note: {string.Join(", ", alreadyPopulated)} already has rows and will be re-walked and re-added unless you clear it first -- backfill-planet-transits does not currently de-duplicate.");
    }

    var backfillStart = new DateTime(1930, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var backfillEnd = new DateTime(2060, 12, 31, 0, 0, 0, DateTimeKind.Utc);
    Console.WriteLine($"Walking {backfillStart:yyyy-MM-dd} to {backfillEnd:yyyy-MM-dd} for {string.Join(", ", PlanetTransitEventFinder.TrackedPlanets)} -- this scans ~{(backfillEnd - backfillStart).Days:N0} days, may take a few minutes.\n");

    var lastReportedYear = 0;
    var allEvents = PlanetTransitEventFinder.FindCrossings(backfillStart, backfillEnd, day =>
    {
        if (day.Year != lastReportedYear && day.Month == 1)
        {
            lastReportedYear = day.Year;
            Console.WriteLine($"  ...at {day:yyyy-MM-dd}");
        }
    });

    var totalInserted = 0;
    foreach (var planet in PlanetTransitEventFinder.TrackedPlanets)
    {
        var planetEvents = allEvents.Where(e => e.Planet == planet).OrderBy(e => e.EventDateTimeUtc).ToList();
        var marked = PlanetTransitEventFinder.MarkReentries(planetEvents);
        transitRepo.InsertAll(marked);
        Console.WriteLine($"{planet}: inserted {marked.Count} event(s) ({marked.Count(m => m.IsReentry)} re-entries).");
        totalInserted += marked.Count;
    }
    Console.WriteLine($"\nDone -- inserted {totalInserted} total transit event(s) across {PlanetTransitEventFinder.TrackedPlanets.Count} planet(s).");
    return;
}

// --- One-off backfill mode: `dotnet run -- backfill-dasha` ---
// Computes and stores Vimshottari Dasha for every saved person who doesn't have it yet (e.g.
// everyone saved before this feature existed, 2026-08-27). Safe to re-run — skips anyone who
// already has a VimshottariDasha ChartResults row.
if (args.Length > 0 && args[0] == "backfill-dasha")
{
    var chartResultsRepoForDashaBackfill = new ChartResultsRepository(connectionFactory);
    var dashaPeriodsRepoForBackfill = new DashaPeriodsRepository(connectionFactory);
    var dashaServiceForBackfill = new VimshottariDashaService(chartResultsRepoForDashaBackfill, dashaPeriodsRepoForBackfill);

    var people = birthDetailsRepo.GetAll();
    var backfilled = 0;
    foreach (var person in people)
    {
        var alreadyHasDasha = chartResultsRepoForDashaBackfill.GetByBirthDetailId(person.Id)
            .Any(r => r.ChartType == VimshottariDashaCalculator.ChartType);
        if (alreadyHasDasha) continue;

        var (result, _) = dashaServiceForBackfill.ComputeAndStore(person);
        Console.WriteLine($"Backfilled Dasha for {person.Name} (ChartResultId={result.Id}).");
        backfilled++;
    }
    Console.WriteLine($"\nDone — backfilled Dasha for {backfilled} person/people.");
    return;
}

// --- Standalone mode: `dotnet run -- compute-dasha <name>` ---
// (Re)computes and prints Mahadasha/Antardasha for one already-saved person by exact name —
// e.g. after a birth-time correction, without re-entering their whole record. Replaces any
// existing Dasha for them (VimshottariDashaService.ComputeAndStore), leaving D1/D9 untouched.
if (args.Length > 1 && args[0] == "compute-dasha")
{
    var targetName = string.Join(' ', args.Skip(1));
    var person = birthDetailsRepo.GetAll().FirstOrDefault(p => p.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
    if (person is null)
    {
        Console.WriteLine($"No saved person named \"{targetName}\".");
        return;
    }

    var dashaService = new VimshottariDashaService(new ChartResultsRepository(connectionFactory), new DashaPeriodsRepository(connectionFactory));
    var (result, tree) = dashaService.ComputeAndStore(person);
    Console.WriteLine($"Computed and stored Vimshottari Dasha for {person.Name} (ChartResultId={result.Id}):\n");
    PrintDashaTreeFromComputed(tree);
    return;
}

// --- Standalone mode: `dotnet run -- show-dasha <name>` ---
// Prints an already-stored Dasha (no recompute) by reading tbl_Chart_DashaPeriods back — the
// read-side counterpart to compute-dasha, and how PrintDashaTree (DashaPeriodRecord-based) gets used.
if (args.Length > 1 && args[0] == "show-dasha")
{
    var targetName = string.Join(' ', args.Skip(1));
    var person = birthDetailsRepo.GetAll().FirstOrDefault(p => p.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
    if (person is null)
    {
        Console.WriteLine($"No saved person named \"{targetName}\".");
        return;
    }

    var tree = new DashaPeriodsRepository(connectionFactory).GetTreeByBirthDetailId(person.Id);
    if (tree.Count == 0)
    {
        Console.WriteLine($"No Dasha stored yet for {person.Name} — run `compute-dasha {person.Name}` first.");
        return;
    }

    Console.WriteLine($"Vimshottari Dasha for {person.Name}:\n");
    PrintDashaTree(tree);
    return;
}

// --- Standalone mode: `dotnet run -- compute-all <name>` ---
// Regenerates every registered chart type + Vimshottari Dasha for one already-saved person by exact
// name — one command in place of running backfill-charts + backfill-dasha separately, e.g. after a
// birth-time correction. Delete-first-then-regenerate via ChartGenerationService.GenerateAll, so
// it's idempotent and safe to re-run.
if (args.Length > 1 && args[0] == "compute-all")
{
    var targetName = string.Join(' ', args.Skip(1));
    var person = birthDetailsRepo.GetAll().FirstOrDefault(p => p.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
    if (person is null)
    {
        Console.WriteLine($"No saved person named \"{targetName}\".");
        return;
    }

    var genReport = chartGenerationService.GenerateAll(person);
    Console.WriteLine($"{person.Name}: regenerated [{string.Join(", ", genReport.ChartTypesWritten)}]"
                      + (genReport.DashaWritten ? " + Vimshottari Dasha" : ""));
    return;
}

string name;
while (true)
{
    name = ReadRequired("Name");
    if (!birthDetailsRepo.ExistsByName(name)) break;
    Console.WriteLine($"  A record named \"{name}\" already exists. Please enter a different name.");
}

var dateOfBirth = ReadDate("Date of Birth");
var timeOfBirth = ReadTime("Time of Birth (as recorded)");
var placeCity = ReadRequired("Place of Birth - City");
var placeCountry = ReadRequired("Place of Birth - Country");

// --- Resolve lat/long/UTC-offset from place ---
double latitude, longitude;
string utcOffset, ianaTimeZoneId;

Console.WriteLine($"\nResolving location \"{placeCity}, {placeCountry}\"...");
try
{
    IPlaceResolver resolver = new NominatimPlaceResolver();
    var resolved = await resolver.ResolveAsync(placeCity, placeCountry, dateOfBirth);
    latitude = resolved.Latitude;
    longitude = resolved.Longitude;
    utcOffset = resolved.UtcOffset.ToString(@"hh\:mm\:ss");
    if (resolved.UtcOffset < TimeSpan.Zero) utcOffset = "-" + utcOffset;
    ianaTimeZoneId = resolved.IanaTimeZoneId;
    Console.WriteLine($"  Resolved: lat={latitude}, long={longitude}, UTC offset={utcOffset} ({ianaTimeZoneId})");
}
catch (Exception ex)
{
    Console.WriteLine($"  Automatic resolution failed: {ex.Message}");
    Console.WriteLine("  Enter location details manually.");
    latitude = double.Parse(ReadRequired("  Latitude (decimal degrees, e.g. 13.0827)"), CultureInfo.InvariantCulture);
    longitude = double.Parse(ReadRequired("  Longitude (decimal degrees, e.g. 80.2707)"), CultureInfo.InvariantCulture);
    utcOffset = ReadRequired("  UTC offset (e.g. 05:30 or -08:00)");
    if (TimeSpan.TryParse(utcOffset, out var parsedOffset))
        utcOffset = (parsedOffset < TimeSpan.Zero ? "-" : "") + parsedOffset.ToString(@"hh\:mm\:ss");
    ianaTimeZoneId = "Manual";
}

var birthDetails = new BirthDetails
{
    Name = name,
    DateOfBirth = dateOfBirth,
    TimeOfBirth = timeOfBirth,
    PlaceCity = placeCity,
    PlaceCountry = placeCountry,
    Latitude = latitude,
    Longitude = longitude,
    UtcOffset = utcOffset,
    IanaTimeZoneId = ianaTimeZoneId
};

// --- Store input, then compute + store every chart type + Vimshottari Dasha (one pipeline: Task 7) ---
birthDetailsRepo.Insert(birthDetails);
Console.WriteLine($"\nStored BirthDetails (Id={birthDetails.Id}).");

var report = chartGenerationService.GenerateAll(birthDetails);
Console.WriteLine($"Stored: [{string.Join(", ", report.ChartTypesWritten)}]{(report.DashaWritten ? " + Vimshottari Dasha" : "")}\n");

// --- Print summary (read back from the store) ---
foreach (var result in chartResultsRepo.GetByBirthDetailId(birthDetails.Id))
{
    Console.WriteLine($"--- {result.ChartType} ({result.Ayanamsha}, {result.HouseSystem}) ---");
    Console.WriteLine(result.ResultJson);
    Console.WriteLine();
}
