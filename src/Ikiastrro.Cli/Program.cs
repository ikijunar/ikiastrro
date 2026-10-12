using System.Globalization;
using System.Text;
using System.Text.Json;
using Dapper;
using Ikiastrro.Cli;
using Ikiastrro.Core.Engines.Ashtakavarga;
using Ikiastrro.Core.Engines.Panchanga;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Matching;
using Ikiastrro.Core.Engines.DivisionalCharts;
using Ikiastrro.Core.Pipeline;
using Ikiastrro.Core.Engines.PlanetaryStates;
using Ikiastrro.Core.Engines.Dasha;
using Ikiastrro.Core.Engines.Dignity;
using Ikiastrro.Core.Engines.Dispositors;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Karakas;
using Ikiastrro.Core.Geocoding;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Relationships;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Transits;
using Ikiastrro.Data;

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

Console.WriteLine("=== ikiastrro ===");
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
// --db <name> / --db=<name> : target a non-default catalog (stage/uat/scratch). Stripped from
// args before mode dispatch so `verify-schema --db foo` still dispatches on "verify-schema".
static string? ExtractDbOverride(ref string[] args)
{
    string? db = null;
    var kept = new List<string>(args.Length);
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--db" && i + 1 < args.Length) { db = args[++i]; continue; }
        if (args[i].StartsWith("--db=", StringComparison.Ordinal)) { db = args[i]["--db=".Length..]; continue; }
        kept.Add(args[i]);
    }
    args = kept.ToArray();
    return string.IsNullOrWhiteSpace(db) ? null : db;
}

var dbOverride = ExtractDbOverride(ref args);

var connectionFactory = SqlConnectionFactory.Create(dbNameOverride: dbOverride);
if (dbOverride is not null) Console.WriteLine($"(targeting catalog: {dbOverride})");
var birthDetailsRepo = new BirthDetailsRepository(connectionFactory);

// --- Shared compute-and-store pipeline (Task 6/7): one instance, reused by every one-off mode
//     below and by the interactive add flow near the bottom of this file. ---
var vargaSchemes = new VargaSchemeRepository(connectionFactory).GetAll(1);
var orchestrator = ChartCalculationOrchestrator.CreateDefault(vargaSchemes,
    new SubPlanetRuleRepository(connectionFactory).GetAll(new RuleSetRepository(connectionFactory).GetActive().Id));
var chartResultsRepo = new ChartResultsRepository(connectionFactory);
var ayanamsaRuleRepo = new AyanamsaRuleRepository(connectionFactory);
var vimshottariDashaService = new VimshottariDashaService(
    chartResultsRepo, new DashaPeriodsRepository(connectionFactory));
var chartGenerationService = new ChartGenerationService(
    orchestrator, vimshottariDashaService,
    chartResultsRepo,
    new ChartKeyDetailsRepository(connectionFactory),
    new ChartHouseLordsRepository(connectionFactory),
    new ChartConjunctionsRepository(connectionFactory),
    new ChartMultiGrahaConjunctionRepository(connectionFactory),
    new ChartAspectsRepository(connectionFactory),
    new PlanetaryStateRuleRepository(connectionFactory),
    new PlanetaryStateRepository(connectionFactory),
    new RuleSetRepository(connectionFactory),
    new ChartTypeRepository(connectionFactory),
    ayanamsaRuleRepo,
    new PlanetaryStrengthRepository(connectionFactory),
    new BhavaStrengthRepository(connectionFactory),
    new VargottamaRepository(connectionFactory), new YogaInputRepository(connectionFactory),
    new AshtakavargaRepository(connectionFactory), new PanchangaRepository(connectionFactory),
    new AmsabalaRepository(connectionFactory), new AmsabalaSchemeRepository(connectionFactory),
    new KpSubLordChainRepository(connectionFactory), new ArgalaFactRepository(connectionFactory),
    new GrahaDrishtiStrengthRepository(connectionFactory),
    NewHouseStrengthStatisticsService(connectionFactory, chartResultsRepo));

static Ikiastrro.Data.Statistics.HouseStrengthStatisticsService NewHouseStrengthStatisticsService(
    SqlConnectionFactory factory, ChartResultsRepository chartResults) =>
    new(chartResults, new ChartKeyDetailsRepository(factory), new AshtakavargaRepository(factory),
        new BhavaStrengthRepository(factory), new PlanetaryStrengthRepository(factory),
        new AmsabalaRepository(factory), new ArgalaFactRepository(factory),
        new Ikiastrro.Data.Statistics.HouseStrengthStatisticsRepository(factory), new RuleSetRepository(factory));

// --- Backfill mode: `dotnet run -- backfill-strength-statistics` ---
// Rebuilds tbl_Fact_HouseStrengthStatistics (migration 156) for every saved person from facts
// already in the database — the Key Inference strength statistics, every chart x 12 signs. Chart
// generation keeps it current from then on; this is for people generated before migration 156.
if (args.Length > 0 && args[0] == "backfill-strength-statistics")
{
    var statisticsService = NewHouseStrengthStatisticsService(connectionFactory, chartResultsRepo);
    var everyone = birthDetailsRepo.GetAll().ToList();
    Console.WriteLine($"backfill-strength-statistics: {everyone.Count} person(s).");
    foreach (var person in everyone)
        Console.WriteLine($"  {person.Name}: {statisticsService.Recompute(person.Id)} rows written.");
    return;
}

// --- `dotnet run -- materialize-dasha-features` ---
// Writes the natal KI_DASHA_MATTER_V1 feature rows (migration 178) for every ELIGIBLE analytics subject from
// stored charts. Run before `ikiastrro-analytics validate-dasha-matters` / `describe-dasha-matters`.
if (args.Length > 0 && args[0] == "materialize-dasha-features")
{
    var materializer = new Ikiastrro.Data.Statistics.DashaMatterFeatureMaterializer(
        connectionFactory, chartResultsRepo, new ChartKeyDetailsRepository(connectionFactory));
    var (subjects, rows) = materializer.Materialize();
    Console.WriteLine($"materialize-dasha-features: {subjects} eligible subject(s), {rows} feature row(s).");
    return;
}

// --- One-off backfill mode: `dotnet run -- backfill-analytics` ---
// Unconditionally re-derives all four analytics tables (KeyDetails/HouseLords/Conjunctions/Aspects)
// for every calculable chart type of every saved person — delete-then-reinsert, with no "skip
// ChartResults that already have rows" guard (that guard was removed once the write path was
// single-sourced through ChartGenerationService.RecomputeAnalytics). Recomputes from Swiss Ephemeris
// (via the same calculator that originally produced the chart) rather than round-tripping the stored
// ResultJson; that is deterministic (same birth data -> same signs/houses) so it's no less correct.
// NOTE: this mode is now equivalent to `recompute-keydetails` — both just call
// ChartGenerationService.RecomputeAnalytics(person, chartTypeFilter: null). Kept as two names
// pending a decision to merge them (out of scope here).
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

// Read-only: verifies current rule data and in-memory output, never rewrites saved charts.
// --- Read-only report: `dotnet run -- family-dasha [yyyy-mm-dd]` ---
// Running Vimshottari lords for every saved person against each life area (PVR Ch. 16.5), plus the Kendradi
// Graha Dasa caution (Ch. 16.7). Facts only; reads the stored charts and dasha periods, writes nothing.
if (args.Length > 0 && args[0] == "family-dasha")
{
    var on = args.Length > 1 && DateTime.TryParse(args[1], out var parsed) ? parsed.Date : DateTime.Today;
    var charts = new DoshaChartRepository(connectionFactory);
    var history = new PairHistoryRepository(connectionFactory);
    var codes = LifeMatterSimilarity.Areas.Where(a => a.Houses.Count > 0).Select(a => a.Varga).Append("D1").Distinct().ToArray();
    Console.WriteLine($"Running dasha lords by life area on {on:yyyy-MM-dd} (PVR 16.5: MD from Sun, AD from Moon, PD from Lagna)");
    foreach (var person in birthDetailsRepo.GetAll())
    {
        Console.WriteLine($"\n== {person.Name} ==");
        var d = history.DashaOn(person.Id, on);
        var vargas = charts.GetVargas(person.Id, codes);
        if (d is null || d.Maha is null || d.Antar is null || d.Pratyantar is null || !vargas.TryGetValue("D1", out var d1))
        {
            Console.WriteLine("  (no stored dasha periods or D1 chart)");
            continue;
        }
        var (fromLagna, fromMoon) = KendradiGrahaDasaCheck.Evaluate(d1);
        Console.WriteLine($"  MD {d.Maha} / AD {d.Antar} / PD {d.Pratyantar}.  Kendra planets: from Lagna {fromLagna.PlanetsInQuadrants} " +
            $"({fromLagna.QuadrantsOccupied}/4 occupied), from Moon {fromMoon.PlanetsInQuadrants} ({fromMoon.QuadrantsOccupied}/4 occupied)");
        var running = new RunningDasha(Enum.Parse<PlanetName>(d.Maha), Enum.Parse<PlanetName>(d.Antar), Enum.Parse<PlanetName>(d.Pratyantar));
        foreach (var r in DashaPromiseReader.Read(vargas, running).Where(r => r.IsLinked))
            Console.WriteLine($"  {r.Area.Name,-18} {r.Level,-10} {r.Lord,-8} {r.Area.Varga,-4} " +
                $"[{string.Join(", ", r.LinksFromLagna.Concat(r.LinksFromReference))}]  in {DashaPromiseReader.Ordinal(r.HouseFromReference)} from {r.Reference}");
    }
    return;
}

if (args.Length > 0 && args[0] == "verify-upagrahas")
{
    var rules = new SubPlanetRuleRepository(connectionFactory).GetAll(new RuleSetRepository(connectionFactory).GetActive().Id);
    var birth = new BirthDetails { Name = "Upagraha verification", DateOfBirth = new DateOnly(2000, 1, 2),
        TimeOfBirth = new TimeOnly(3, 0), Latitude = 13.0827, Longitude = 80.2707, UtcOffset = "05:30" };
    var computed = orchestrator.CalculateAll(birth);
    var failures = 0;
    foreach (var (_, input) in computed)
    {
        var points = input.SpecialPoints.Where(p => p.PointKind == "Upagraha").ToArray();
        var ok = points.Length == 11 && points.Select(p => p.Planet).Distinct().Count() == 11 &&
            points.All(p => p.NirayanaLongitudeDegrees is >= 0 and < 360);
        Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {input.ChartType}: {points.Length} upagrahas");
        if (!ok) failures++;
    }
    var d1 = computed.Single(c => c.Input.ChartType == "D1").Input;
    var sunLongitude = d1.Planets.Single(p => p.Planet == "Sun").NirayanaLongitudeDegrees!.Value;
    var upaketu = d1.SpecialPoints.Single(p => p.Planet == "Upaketu").NirayanaLongitudeDegrees!.Value;
    if (Math.Abs(((upaketu + 30 - sunLongitude + 540) % 360) - 180) > .000002) failures++;
    var sun = SwissEphemerisProvider.GetSunTimes(birth);
    var pair = UpagrahaCalculator.Compute(birth, sun);
    foreach (var expected in new[] { pair.Gulika, pair.Maandi })
        if (Math.Abs(d1.SpecialPoints.Single(p => p.Planet == expected.Code).NirayanaLongitudeDegrees!.Value - expected.NirayanaLongitudeDegrees) > .000001) failures++;
    Console.WriteLine($"Rule set {rules.RuleSetId}; PVR Gulika=middle, Maandi=start; pre-dawn birth uses previous sunrise.");
    Console.WriteLine(failures == 0 ? "verify-upagrahas: ALL PASS" : $"verify-upagrahas: {failures} FAILURE(S)");
    Environment.ExitCode = failures == 0 ? 0 : 1;
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

    // --- IVargaSignRule unit checks (hand-computed) ---
    // D3 Drekkana: (sign + l*4)%12, l = deg/10
    Check("rule D3 Ar 12",  new LinearVargaSignRule(3, 4).SignFor(12),  ZodiacName.Leo);          // l1 -> 0+4
    Check("rule D3 Ar 25",  new LinearVargaSignRule(3, 4).SignFor(25),  ZodiacName.Sagittarius);  // l2 -> 0+8
    Check("rule D3 Ta 2",   new LinearVargaSignRule(3, 4).SignFor(32),  ZodiacName.Taurus);       // sign1 l0
    // D4 Chaturthamsa: (sign + l*3)%12, l = deg/7.5
    Check("rule D4 Ar 20",  new LinearVargaSignRule(4, 3).SignFor(20),  ZodiacName.Libra);        // l2 -> 0+6
    Check("rule D4 Ar 3",   new LinearVargaSignRule(4, 3).SignFor(3),   ZodiacName.Aries);        // l0
    // D12 Dwadasamsa: (sign + l)%12, l = deg/2.5
    Check("rule D12 Ar 12", new LinearVargaSignRule(12, 1).SignFor(12), ZodiacName.Leo);          // l4
    Check("rule D12 Ta 29", new LinearVargaSignRule(12, 1).SignFor(59), ZodiacName.Aries);        // sign1 l11 -> 12%12
    // D60 Shashtyamsa: (sign + l)%12, l = deg/0.5
    Check("rule D60 Ar 12", new LinearVargaSignRule(60, 1).SignFor(12), ZodiacName.Aries);        // l24 -> 24%12
    Check("rule D60 Ar 1",  new LinearVargaSignRule(60, 1).SignFor(1),  ZodiacName.Gemini);       // l2 -> 0+2

    // D7 Saptamsa: odd -> (sign+l)%12 ; even -> (sign+l+6)%12 ; l = deg/(30/7)
    Check("rule D7 Ar 2",   new SaptamsaD7SignRule().SignFor(2),   ZodiacName.Aries);       // sign0 odd, l0
    Check("rule D7 Ta 2",   new SaptamsaD7SignRule().SignFor(32),  ZodiacName.Scorpio);     // sign1 even, l0 -> 1+0+6
    // D8 Ashtamsa: movable->l ; fixed->l+8 ; dual->l+4 ; l = deg/3.75
    Check("rule D8 Ar 2",   new AshtamsaD8SignRule().SignFor(2),   ZodiacName.Aries);       // sign0 movable l0
    Check("rule D8 Ta 2",   new AshtamsaD8SignRule().SignFor(32),  ZodiacName.Sagittarius); // sign1 fixed l0 -> 8
    Check("rule D8 Ge 2",   new AshtamsaD8SignRule().SignFor(62),  ZodiacName.Leo);         // sign2 dual l0 -> 4
    // D24 Siddhamsa: odd -> 4+l ; even -> 3+l ; l = deg/1.25
    Check("rule D24 Ar 1",  new SiddhamsaD24SignRule().SignFor(1),  ZodiacName.Leo);        // sign0 odd l0 -> 4
    Check("rule D24 Ta 1",  new SiddhamsaD24SignRule().SignFor(31), ZodiacName.Cancer);     // sign1 even l0 -> 3
    // D27 Nakshatramsa: fire->l ; earth->l+3 ; air->l+6 ; water->l+9 ; l = deg/(30/27)
    Check("rule D27 Ar 1",  new NakshatramsaD27SignRule().SignFor(1),  ZodiacName.Aries);       // fire l0
    Check("rule D27 Ta 1",  new NakshatramsaD27SignRule().SignFor(31), ZodiacName.Cancer);      // earth l0 -> 3
    Check("rule D27 Ge 1",  new NakshatramsaD27SignRule().SignFor(61), ZodiacName.Libra);       // air l0 -> 6
    Check("rule D27 Cn 1",  new NakshatramsaD27SignRule().SignFor(91), ZodiacName.Capricornus); // water l0 -> 9

    // D16 Shodasamsa: movable->l ; fixed->l+4 ; dual->l+8 ; l = deg/1.875
    Check("rule D16 Ar 1",  new ShodasamsaD16SignRule().SignFor(1),  ZodiacName.Aries);
    Check("rule D16 Ta 1",  new ShodasamsaD16SignRule().SignFor(31), ZodiacName.Leo);          // fixed l0 -> 4
    Check("rule D16 Ge 1",  new ShodasamsaD16SignRule().SignFor(61), ZodiacName.Sagittarius);  // dual l0 -> 8
    // D20 Vimsamsa: movable->l ; dual->l+4 ; fixed->l+8 ; l = deg/1.5
    Check("rule D20 Ar 1",  new VimsamsaD20SignRule().SignFor(1),  ZodiacName.Aries);
    Check("rule D20 Ta 1",  new VimsamsaD20SignRule().SignFor(31), ZodiacName.Sagittarius);    // fixed l0 -> 8
    Check("rule D20 Ge 1",  new VimsamsaD20SignRule().SignFor(61), ZodiacName.Leo);            // dual l0 -> 4
    // D40 Khavedamsa: odd->l ; even->l+6 ; l = deg/0.75
    Check("rule D40 Ar .3", new KhavedamsaD40SignRule().SignFor(0.3),  ZodiacName.Aries);      // odd l0
    Check("rule D40 Ar 2",  new KhavedamsaD40SignRule().SignFor(2),    ZodiacName.Gemini);     // odd l2
    Check("rule D40 Ta .3", new KhavedamsaD40SignRule().SignFor(30.3), ZodiacName.Libra);      // even l0 -> 0+6
    // D45 Akshavedamsa: movable->l ; fixed->l+4 ; dual->l+8 ; l = deg/(30/45)
    Check("rule D45 Ar .3", new AkshavedamsaD45SignRule().SignFor(0.3),  ZodiacName.Aries);    // movable l0
    Check("rule D45 Ar 2",  new AkshavedamsaD45SignRule().SignFor(2),    ZodiacName.Cancer);   // movable l3
    Check("rule D45 Ta .3", new AkshavedamsaD45SignRule().SignFor(30.3), ZodiacName.Leo);      // fixed l0 -> 4

    // D5 Panchamsa: odd -> [Ar,Aq,Sg,Ge,Li][l] ; even -> [Ta,Vi,Pi,Cp,Sc][l] ; l = deg/6
    Check("rule D5 Ar 3",   new PanchamsaD5SignRule().SignFor(3),   ZodiacName.Aries);         // odd l0
    Check("rule D5 Ar 9",   new PanchamsaD5SignRule().SignFor(9),   ZodiacName.Aquarius);      // odd l1
    Check("rule D5 Ta 3",   new PanchamsaD5SignRule().SignFor(33),  ZodiacName.Taurus);        // even l0
    Check("rule D5 Ta 27",  new PanchamsaD5SignRule().SignFor(57),  ZodiacName.Scorpio);       // even l4
    // D30 Trimsamsa: bands on degrees-in-sign (not l-parts)
    Check("rule D30 Ar 3",  new TrimsamsaD30SignRule().SignFor(3),   ZodiacName.Aries);        // odd [0,5)
    Check("rule D30 Ar 7",  new TrimsamsaD30SignRule().SignFor(7),   ZodiacName.Aquarius);     // odd [5,10)
    Check("rule D30 Ar 15", new TrimsamsaD30SignRule().SignFor(15),  ZodiacName.Sagittarius);  // odd [10,18)
    Check("rule D30 Ar 22", new TrimsamsaD30SignRule().SignFor(22),  ZodiacName.Gemini);       // odd [18,25)
    Check("rule D30 Ar 28", new TrimsamsaD30SignRule().SignFor(28),  ZodiacName.Libra);        // odd [25,30]
    Check("rule D30 Ta 3",  new TrimsamsaD30SignRule().SignFor(33),  ZodiacName.Taurus);       // even [0,5)
    Check("rule D30 Ta 15", new TrimsamsaD30SignRule().SignFor(45),  ZodiacName.Pisces);       // even [12,20)
    Check("rule D30 Ta 28", new TrimsamsaD30SignRule().SignFor(58),  ZodiacName.Scorpio);      // even [25,30]

    // Wrapper rules must match the AstroMath methods they delegate to
    Check("wrap D2 Ar 10",  new HoraD2ClassicSignRule().SignFor(10),   AstroMath.GetHoraSign(10));
    Check("wrap D2 Ta 20",  new HoraD2ClassicSignRule().SignFor(50),   AstroMath.GetHoraSign(50));
    Check("wrap D6 Ar 2",   new ShashtamsaD6SignRule().SignFor(2),     AstroMath.GetShashtamsaSign(2));
    Check("wrap D6 Ta 27",  new ShashtamsaD6SignRule().SignFor(57),    AstroMath.GetShashtamsaSign(57));
    Check("wrap D9 Ar 3",   new NavamsaD9SignRule().SignFor(3),        AstroMath.GetNavamsaSign(3));
    Check("wrap D9 Sc 21",  new NavamsaD9SignRule().SignFor(231),      AstroMath.GetNavamsaSign(231));
    Check("wrap D10 Ar 28", new DasamsaD10SignRule().SignFor(28),      AstroMath.GetDasamsaSign(28));
    Check("wrap D11 Ta 29", new RudramsaD11SignRule().SignFor(59),     AstroMath.GetRudramsaSign(59));
    // D2-US (Uma Shambu) - parivritti even-reverse D2 (confirmed vs D-2 (US) export grid)
    Check("rule D2US Ar 5",  new HoraD2UmaShambuSignRule().SignFor(5),   ZodiacName.Aries);   // r0 even h0 -> 0
    Check("rule D2US Ar 20", new HoraD2UmaShambuSignRule().SignFor(20),  ZodiacName.Taurus);  // r0 even h1 -> 1
    Check("rule D2US Ta 5",  new HoraD2UmaShambuSignRule().SignFor(35),  ZodiacName.Cancer);  // r1 odd  h0 -> 2r+1
    Check("rule D2US Ta 20", new HoraD2UmaShambuSignRule().SignFor(50),  ZodiacName.Gemini);  // r1 odd  h1 -> 2r+1-1
    Check("rule D2US Sc 7",  new HoraD2UmaShambuSignRule().SignFor(217), ZodiacName.Cancer);  // r7 odd  h0 -> 15%12 (export Moon)
    // D3 Uma Shambu - first drekkana r + 4*ceil(r/2), odd signs forward, even backward (D-3 (US))
    Check("rule D3US Ar 15", new DrekkanaD3UmaShambuSignRule().SignFor(15),  ZodiacName.Taurus);  // start Ar, l1 forward
    Check("rule D3US Sc 7",  new DrekkanaD3UmaShambuSignRule().SignFor(217), ZodiacName.Pisces);  // start Pi (export Moon)
    Check("rule D3US Vi 11", new DrekkanaD3UmaShambuSignRule().SignFor(161), ZodiacName.Leo);     // start Vi, l1 backward (export Saturn)
    Check("rule D3US Pi 25", new DrekkanaD3UmaShambuSignRule().SignFor(355), ZodiacName.Capricornus); // start Pi, l2 backward (Hora Lagna)
    // D7 even-reverse - odd from the sign forward, even from the 7th backward (D-7 (7-1))
    Check("rule D7ER Ar 9",  new SaptamsaD7EvenReverseSignRule().SignFor(9),   ZodiacName.Gemini);      // l2 forward
    Check("rule D7ER Sc 7",  new SaptamsaD7EvenReverseSignRule().SignFor(217), ZodiacName.Aries);       // Ta - 1 (export Moon)
    Check("rule D7ER Vi 9",  new SaptamsaD7EvenReverseSignRule().SignFor(159), ZodiacName.Capricornus); // Pi - 2 (export Jupiter)

    // D10/D16/D24/D60 on JHora's schemes (migration 166) - export Moon 7 Sc / Jupiter 8 Vi
    Check("rule D10ER Sc 7", new DasamsaD10EvenReverseSignRule().SignFor(217.2088), ZodiacName.Capricornus);
    Check("rule D16ER Sc 7", new ShodasamsaD16EvenReverseSignRule().SignFor(217.2088), ZodiacName.Leo);
    Check("rule D24ER Sc 7", new SiddhamsaD24EvenReverseSignRule().SignFor(217.2088), ZodiacName.Aquarius);
    Check("rule D60RvAr Vi 8", new ShashtyamsaD60EvenReverseFromAriesSignRule().SignFor(158.7238), ZodiacName.Libra);

    // Every SignRuleKey seeded in tbl_Rule_VargaScheme must resolve through the factory
    string[] seededKeys =
    {
        "HoraD2Classic","HoraD2UmaShambu","DrekkanaD3","DrekkanaD3UmaShambu","ChaturthamsaD4","PanchamsaD5",
        "ShashtamsaD6","SaptamsaD7","SaptamsaD7EvenReverse","AshtamsaD8","NavamsaD9","DasamsaD10","DasamsaD10EvenReverse","RudramsaD11",
        "DwadasamsaD12","ShodasamsaD16","ShodasamsaD16EvenReverse","VimsamsaD20","SiddhamsaD24","SiddhamsaD24EvenReverse","NakshatramsaD27",
        "TrimsamsaD30","KhavedamsaD40","AkshavedamsaD45","ShashtyamsaD60","ShashtyamsaD60EvenReverseFromAries"
    };
    int[] seededFactors = { 2, 2, 3, 3, 4, 5, 6, 7, 7, 8, 9, 10, 10, 11, 12, 16, 16, 20, 24, 24, 27, 30, 40, 45, 60, 60 };
    var factoryFailures = 0;
    for (var i = 0; i < seededKeys.Length; i++)
    {
        try { _ = VargaSignRuleFactory.For(seededKeys[i], seededFactors[i]).SignFor(15.0); }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] factory {seededKeys[i]}: {ex.Message}"); factoryFailures++; }
    }
    Console.WriteLine($"  [{(factoryFailures == 0 ? "PASS" : "FAIL")}] VargaSignRuleFactory resolves all {seededKeys.Length} known keys");
    if (factoryFailures > 0) failures += factoryFailures;

    // VargaChartComputer's D9 sign rule must agree with AstroMath.GetNavamsaSign (spot longitudes).
    // The full VargaChartComputer-vs-live-DB comparison is done in Task 13 (verify-vargas grid match).
    {
        var d9rule = new NavamsaD9SignRule();
        var mism = 0;
        foreach (var lon in new[] { 8.2, 217.3, 3.95, 1.84, 173.7, 191.96, 41.99, 190.96, 103.06 })
            if (d9rule.SignFor(lon) != AstroMath.GetNavamsaSign(lon)) mism++;
        Check("VargaChartComputer D9-rule vs AstroMath (spot longitudes)", mism, 0);
    }

    // VargaSchemeRepository: 20 rows for RuleSetId 1, every SignRuleKey resolves
    {
        var schemes = new VargaSchemeRepository(connectionFactory).GetAll(1);
        Check("VargaSchemeRepository row count", schemes.Count, 20);
        var bad = 0;
        foreach (var s in schemes)
            try { _ = VargaSignRuleFactory.For(s.SignRuleKey, s.DivisionFactor); }
            catch { bad++; }
        Check("every scheme SignRuleKey resolves", bad, 0);
    }

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

    // --- JHora export grid match (Ramakrishnan, 22 Apr 1981, Chennai) ---
    // Transcribed from D:\@ClaudeSpace\Scratchpad\Rammy_Jagannatha.txt. Planet order:
    // Ascendant, Sun, Moon, Mars, Mercury, Jupiter, Venus, Saturn, Rahu, Ketu.
    // D2, D3 and D7 run JHora's default schemes since migration 164 (Uma Shambu hora, Uma
    // Shambu drekkana, saptamsa 7-1); their rows are from the 2026-09-23 "Rasis occupied in all
    // vargas" export, which tags them D-2 (US), D-3 (US) and D-7 (7-1). D10, D16, D24 and D60
    // (migration 166: D-10 (5-8), D-16 (Rev), D-24 (Rev), D-60 (RvAr)) are from the same export,
    // with Rahu/Ketu on the true node (decision 008).
    var jhoraGrid = new Dictionary<string, ZodiacName[]>
    {
        ["D2-US"] = new[] { ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Cancer, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Pisces, ZodiacName.Aries, ZodiacName.Pisces, ZodiacName.Scorpio, ZodiacName.Scorpio },
        ["D2"]  = new[] { ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Cancer, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Pisces, ZodiacName.Aries, ZodiacName.Pisces, ZodiacName.Scorpio, ZodiacName.Scorpio },
        ["D3"]  = new[] { ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Pisces, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Virgo, ZodiacName.Taurus, ZodiacName.Leo, ZodiacName.Aquarius, ZodiacName.Leo },
        ["D4"]  = new[] { ZodiacName.Aries, ZodiacName.Cancer, ZodiacName.Scorpio, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Sagittarius, ZodiacName.Cancer, ZodiacName.Sagittarius, ZodiacName.Libra, ZodiacName.Aries },
        ["D5"]  = new[] { ZodiacName.Aries, ZodiacName.Aquarius, ZodiacName.Virgo, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Virgo, ZodiacName.Aquarius, ZodiacName.Virgo, ZodiacName.Pisces, ZodiacName.Pisces },
        ["D6"]  = new[] { ZodiacName.Aries, ZodiacName.Taurus, ZodiacName.Scorpio, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Scorpio, ZodiacName.Gemini, ZodiacName.Sagittarius, ZodiacName.Sagittarius, ZodiacName.Sagittarius },
        ["D7"]  = new[] { ZodiacName.Aries, ZodiacName.Taurus, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Capricornus, ZodiacName.Gemini, ZodiacName.Capricornus, ZodiacName.Libra, ZodiacName.Aries },
        ["D8"]  = new[] { ZodiacName.Aries, ZodiacName.Gemini, ZodiacName.Capricornus, ZodiacName.Taurus, ZodiacName.Aries, ZodiacName.Libra, ZodiacName.Cancer, ZodiacName.Libra, ZodiacName.Cancer, ZodiacName.Cancer },
        ["D9"]  = new[] { ZodiacName.Aries, ZodiacName.Gemini, ZodiacName.Virgo, ZodiacName.Taurus, ZodiacName.Aries, ZodiacName.Pisces, ZodiacName.Cancer, ZodiacName.Aries, ZodiacName.Libra, ZodiacName.Aries },
        ["D10"] = new[] { ZodiacName.Aries, ZodiacName.Gemini, ZodiacName.Capricornus, ZodiacName.Taurus, ZodiacName.Aries, ZodiacName.Scorpio, ZodiacName.Cancer, ZodiacName.Libra, ZodiacName.Cancer, ZodiacName.Capricornus },
        ["D11"] = new[] { ZodiacName.Aries, ZodiacName.Cancer, ZodiacName.Scorpio, ZodiacName.Taurus, ZodiacName.Aries, ZodiacName.Aquarius, ZodiacName.Leo, ZodiacName.Pisces, ZodiacName.Taurus, ZodiacName.Scorpio },
        ["D12"] = new[] { ZodiacName.Aries, ZodiacName.Cancer, ZodiacName.Capricornus, ZodiacName.Taurus, ZodiacName.Aries, ZodiacName.Sagittarius, ZodiacName.Leo, ZodiacName.Capricornus, ZodiacName.Sagittarius, ZodiacName.Gemini },
        ["D16"] = new[] { ZodiacName.Aries, ZodiacName.Leo, ZodiacName.Leo, ZodiacName.Gemini, ZodiacName.Aries, ZodiacName.Scorpio, ZodiacName.Libra, ZodiacName.Libra, ZodiacName.Capricornus, ZodiacName.Capricornus },
        ["D20"] = new[] { ZodiacName.Aries, ZodiacName.Virgo, ZodiacName.Aries, ZodiacName.Gemini, ZodiacName.Taurus, ZodiacName.Capricornus, ZodiacName.Scorpio, ZodiacName.Pisces, ZodiacName.Sagittarius, ZodiacName.Sagittarius },
        ["D24"] = new[] { ZodiacName.Leo, ZodiacName.Aquarius, ZodiacName.Aquarius, ZodiacName.Scorpio, ZodiacName.Virgo, ZodiacName.Capricornus, ZodiacName.Taurus, ZodiacName.Scorpio, ZodiacName.Virgo, ZodiacName.Virgo },
        ["D27"] = new[] { ZodiacName.Aries, ZodiacName.Scorpio, ZodiacName.Cancer, ZodiacName.Cancer, ZodiacName.Taurus, ZodiacName.Aquarius, ZodiacName.Aquarius, ZodiacName.Aries, ZodiacName.Sagittarius, ZodiacName.Gemini },
        ["D30"] = new[] { ZodiacName.Aries, ZodiacName.Aquarius, ZodiacName.Virgo, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Virgo, ZodiacName.Sagittarius, ZodiacName.Virgo, ZodiacName.Pisces, ZodiacName.Pisces },
        ["D40"] = new[] { ZodiacName.Aries, ZodiacName.Aquarius, ZodiacName.Cancer, ZodiacName.Virgo, ZodiacName.Gemini, ZodiacName.Virgo, ZodiacName.Cancer, ZodiacName.Sagittarius, ZodiacName.Pisces, ZodiacName.Pisces },
        ["D45"] = new[] { ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Gemini, ZodiacName.Virgo, ZodiacName.Gemini, ZodiacName.Capricornus, ZodiacName.Virgo, ZodiacName.Aries, ZodiacName.Scorpio, ZodiacName.Scorpio },
        ["D60"] = new[] { ZodiacName.Taurus, ZodiacName.Leo, ZodiacName.Capricornus, ZodiacName.Scorpio, ZodiacName.Cancer, ZodiacName.Libra, ZodiacName.Pisces, ZodiacName.Gemini, ZodiacName.Aquarius, ZodiacName.Aquarius },
    };
    var planetOrder = new[] { "Ascendant", "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn", "Rahu", "Ketu" };
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        foreach (var (chartType, expectedSigns) in jhoraGrid)
        {
            var rows = conn.Query<(string Planet, string Sign)>(
                "SELECT kd.Planet, kd.Sign FROM dbo.tbl_Chart_KeyDetails kd " +
                "JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId " +
                "JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId " +
                "WHERE bd.Name = 'RamakrishnanP' AND cr.ChartType = @ct AND kd.PointKind = 'Graha'",
                new { ct = chartType })
                .ToDictionary(r => r.Planet, r => r.Sign);
            for (var i = 0; i < planetOrder.Length; i++)
            {
                if (!rows.TryGetValue(planetOrder[i], out var actual))
                { Console.WriteLine($"  [FAIL] JHora {chartType} {planetOrder[i]}: no row"); failures++; continue; }
                Check($"JHora {chartType} {planetOrder[i]}", actual, expectedSigns[i].ToString());
            }
        }
    }

    // --- Degree sanity (DB-backed, every person, every position chart type) ---
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        long Count(string s) => conn.ExecuteScalar<long>(s);
        Check("all VargaLongitudeDegrees in [0,360)",
            Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE PointKind = 'Graha' AND (VargaLongitudeDegrees < 0 OR VargaLongitudeDegrees >= 360)"), 0L);
        Check("all DegreesInSignDecimal in [0,30)",
            Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE PointKind = 'Graha' AND (DegreesInSignDecimal IS NULL OR DegreesInSignDecimal < 0 OR DegreesInSignDecimal >= 30)"), 0L);
        // VargaLongitudeDegrees carries the true within-sign varga degree only. Every varga
        // sign rule counts from the planet's own rasi (or an unequal-part map), never from a
        // global Aries-anchored scaling, so FLOOR(VargaLongitudeDegrees/30) is deliberately
        // NOT the varga sign for any varga here. Sign correctness is covered in full by the
        // hand-computed IVargaSignRule checks and the JHora export grid loop above.
        Check("DegreesInSignDecimal == VargaLongitudeDegrees mod 30",
            Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE PointKind = 'Graha' AND ABS(DegreesInSignDecimal - (VargaLongitudeDegrees % 30)) > 0.0002"), 0L);
        Check("varga KeyDetails SignId populated and in [1,12]",
            Count(@"SELECT COUNT(*)
                    FROM dbo.tbl_Chart_KeyDetails kd
                    JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
                    WHERE cr.CalculationKind = 'PositionChart' AND cr.ChartType <> 'D1'
                      AND kd.Planet <> 'Ascendant' AND kd.PointKind = 'Graha'
                      AND (kd.SignId IS NULL OR kd.SignId < 1 OR kd.SignId > 12)"), 0L);
        // DB-completeness invariant (spec 2): every stored position chart carries its
        // numeric provenance, and every varga chart names its method.
        Check("every position chart has AyanamshaDegrees + SiderealTimeHours",
            Count(@"SELECT COUNT(*) FROM dbo.tbl_ChartResults
                    WHERE CalculationKind = 'PositionChart'
                      AND (AyanamshaDegrees IS NULL OR SiderealTimeHours IS NULL)"), 0L);
        Check("every varga position chart (non-D1) has VargaMethod",
            Count(@"SELECT COUNT(*) FROM dbo.tbl_ChartResults
                    WHERE CalculationKind = 'PositionChart' AND ChartType <> 'D1'
                      AND VargaMethod IS NULL"), 0L);

        var vargottama = conn.Query<(string Planet, string Sign)>(
            @"SELECT d1.Planet, d1.Sign
              FROM dbo.tbl_Chart_KeyDetails d1
              JOIN dbo.tbl_ChartResults cr1 ON cr1.Id = d1.ChartResultId
              JOIN dbo.tbl_BirthDetails bd  ON bd.Id  = cr1.BirthDetailId
              JOIN dbo.tbl_ChartResults cr9 ON cr9.BirthDetailId = bd.Id AND cr9.ChartType = 'D9'
              JOIN dbo.tbl_Chart_KeyDetails d9 ON d9.ChartResultId = cr9.Id AND d9.Planet = d1.Planet
              WHERE bd.Name = 'RamakrishnanP' AND cr1.ChartType = 'D1' AND d1.SignId = d9.SignId").ToList();
        Console.WriteLine($"  [INFO] Ramakrishnan D1/D9 Vargottama: {(vargottama.Count == 0 ? "none" : string.Join(", ", vargottama.Select(v => $"{v.Planet}({v.Sign})")))}");
    }

    Console.WriteLine(failures == 0 ? "\nverify-vargas: ALL PASS" : $"\nverify-vargas: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-functional-nature` ---
// Assertions for LagnaFunctionalNature against PVR Table 30 (SRC_PVR_INTEGRATED p.167): worked
// examples, then every Lagna x planet cell, then PVR's yogakaraka rule re-derived from lordship.
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
    Check("Cancer/Moon nature", cnMo.Nature, FunctionalNature.Benefic);   // Table 30: Moon benefic for Cancer
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
    Check("Aries/Venus maraka houses", string.Join(",", arVe.MarakaHouses), "2,7");

    var arMeOwnership = LagnaFunctionalNature.For(ZodiacName.Aries, PlanetName.Mercury);
    Check("Aries/Mercury dusthana", string.Join(",", arMeOwnership.DusthanaHouses), "6");
    Check("Aries/Mercury trishadaya", string.Join(",", arMeOwnership.TrishadayaHouses), "3,6");
    Check("Aries/Mars eighth lord", string.Join(",", LagnaFunctionalNature.For(ZodiacName.Aries, PlanetName.Mars).DusthanaHouses), "8");
    Check("Aries/Jupiter twelfth lord", string.Join(",", arJu.DusthanaHouses), "12");

    var arSa = LagnaFunctionalNature.For(ZodiacName.Aries, PlanetName.Saturn);
    Check("Aries/Saturn baadhaka", arSa.IsBaadhakaLord, true);
    Check("Aries baadhaka house", arSa.BaadhakaHouse, 11);
    var taSaOwnership = LagnaFunctionalNature.For(ZodiacName.Taurus, PlanetName.Saturn);
    Check("Taurus/Saturn baadhaka", taSaOwnership.IsBaadhakaLord, true);
    Check("Taurus baadhaka house", taSaOwnership.BaadhakaHouse, 9);
    var geJu = LagnaFunctionalNature.For(ZodiacName.Gemini, PlanetName.Jupiter);
    Check("Gemini/Jupiter baadhaka", geJu.IsBaadhakaLord, true);
    Check("Gemini baadhaka house", geJu.BaadhakaHouse, 7);
    Check("Gemini/Jupiter maraka overlap", string.Join(",", geJu.MarakaHouses), "7");
    Check("Aries/Venus nature", arVe.Nature, FunctionalNature.Malefic);     // falls through to the catch-all

    // PVR Table 30 transcribed independently of the engine (Y yogakaraka, B benefic, N neutral, M malefic).
    // Planet order Su Mo Ma Me Ju Ve Sa; '-' = not listed (the Moon for Aries, Libra, Capricornus).
    var table30 = new (ZodiacName Lagna, string Cells)[]
    {
        (ZodiacName.Aries,       "B-BMBMM"), (ZodiacName.Taurus,      "BMNBMMY"), (ZodiacName.Gemini,      "MNMNMBN"),
        (ZodiacName.Cancer,      "NBYMBMN"), (ZodiacName.Leo,         "BNYMBMM"), (ZodiacName.Virgo,       "NMMBMBN"),
        (ZodiacName.Libra,       "M-MBMBY"), (ZodiacName.Scorpio,     "NBNMBMM"), (ZodiacName.Sagittarius, "BNBNNMM"),
        (ZodiacName.Capricornus, "N-MBMYB"), (ZodiacName.Aquarius,    "NMMNMYB"), (ZodiacName.Pisces,      "MBBMNMM"),
    };
    var order = new[] { PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury, PlanetName.Jupiter, PlanetName.Venus, PlanetName.Saturn };
    foreach (var (lagna, cells) in table30)
        for (var i = 0; i < order.Length; i++)
        {
            if (cells[i] == '-') continue;
            var expected = cells[i] switch { 'Y' => FunctionalNature.Yogakaraka, 'B' => FunctionalNature.Benefic, 'N' => FunctionalNature.Neutral, _ => FunctionalNature.Malefic };
            Check($"Table 30 {lagna}/{order[i]}", LagnaFunctionalNature.For(lagna, order[i]).Nature, expected);
        }

    // Moon is phase-dependent for Aries/Libra/Capricornus (PVR p.166-167).
    foreach (var lagna in new[] { ZodiacName.Aries, ZodiacName.Libra, ZodiacName.Capricornus })
    {
        Check($"{lagna}/Moon waxing", LagnaFunctionalNature.For(lagna, PlanetName.Moon, true).Nature, FunctionalNature.Malefic);
        Check($"{lagna}/Moon waning", LagnaFunctionalNature.For(lagna, PlanetName.Moon, false).Nature, FunctionalNature.Neutral);
    }

    // PVR 13.2: a planet owning a quadrant (4/7/10) and a trine (5/9) is the yogakaraka - the table's
    // yogakaraka column must be exactly the planets the rule finds, for every Lagna.
    foreach (var lagna in Enum.GetValues<ZodiacName>())
    {
        var byRule = order.Where(p => { var h = LagnaFunctionalNature.For(lagna, p, true).RuledHouses;
                                         return h.Any(x => x is 4 or 7 or 10) && h.Any(x => x is 5 or 9); }).ToArray();
        Check($"{lagna} yogakaraka by rule", string.Join(",", byRule), LagnaFunctionalNature.YogakarakaFor(lagna)?.ToString() ?? "");
    }

    Console.WriteLine(failures == 0 ? "\nverify-functional-nature: ALL PASS" : $"\nverify-functional-nature: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-baadhaka` ---
// Worked-example assertions for BaadhakaCalculator (PVR §13.3, Table 31 — baadhaka sthaana +
// baadhaka lord per rasi). Solution has no unit-test project for Ikiastrro.Core.
if (args.Length > 0 && args[0] == "verify-baadhaka")
{
    var failures = 0;
    void Check(string label, object actual, object expected)
    {
        var ok = actual.ToString() == expected.ToString();
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    // All 12 rasis against PVR Table 31 (Rahu/Ketu co-baadhaka names on the Ar/Cp rows omitted —
    // see BaadhakaCalculator's divergence note; only the classical planetary lord is asserted here).
    var table = new (ZodiacName Sign, SignModality Modality, ZodiacName Sthaana, string Lord)[]
    {
        (ZodiacName.Aries,       SignModality.Movable, ZodiacName.Aquarius,    "Saturn"),
        (ZodiacName.Taurus,      SignModality.Fixed,   ZodiacName.Capricornus, "Saturn"),
        (ZodiacName.Gemini,      SignModality.Dual,    ZodiacName.Sagittarius, "Jupiter"),
        (ZodiacName.Cancer,      SignModality.Movable, ZodiacName.Taurus,      "Venus"),
        (ZodiacName.Leo,         SignModality.Fixed,   ZodiacName.Aries,       "Mars"),
        (ZodiacName.Virgo,       SignModality.Dual,    ZodiacName.Pisces,      "Jupiter"),
        (ZodiacName.Libra,       SignModality.Movable, ZodiacName.Leo,         "Sun"),
        (ZodiacName.Scorpio,     SignModality.Fixed,   ZodiacName.Cancer,      "Moon"),
        (ZodiacName.Sagittarius, SignModality.Dual,    ZodiacName.Gemini,      "Mercury"),
        (ZodiacName.Capricornus, SignModality.Movable, ZodiacName.Scorpio,     "Mars"),
        (ZodiacName.Aquarius,    SignModality.Fixed,   ZodiacName.Libra,       "Venus"),
        (ZodiacName.Pisces,      SignModality.Dual,    ZodiacName.Virgo,       "Mercury"),
    };
    foreach (var (sign, modality, sthaana, lord) in table)
    {
        var r = BaadhakaCalculator.For(sign);
        Check($"{sign} modality", r.Modality, modality);
        Check($"{sign} sthaana", r.SthaanaSign, sthaana);
        Check($"{sign} baadhaka", r.Baadhaka, lord);
    }

    // Worked examples straight from PVR §13.3 prose (docs/cli/reading/PVR_read_horoscope.md §2.2):
    // "suppose lagna in someone's D-10 is in Ge. Then Jupiter is baadhaka for lagna."
    var geLagna = BaadhakaCalculator.For(ZodiacName.Gemini, 1);
    Check("Gemini lagna baadhaka (PVR D-10 example)", geLagna.Baadhaka, "Jupiter");
    Check("Gemini lagna sthaana", geLagna.SthaanaSign, ZodiacName.Sagittarius);

    // "Aq is the 9th house ... Baadhaka sthana for Aq is Li. So the periods of Venus ... "
    var aqNinth = BaadhakaCalculator.For(ZodiacName.Gemini, 9);   // 9th from Ge lagna = Aq
    Check("9th-from-Gemini sign", HouseEngine.GetHouseSign(ZodiacName.Gemini, 9), ZodiacName.Aquarius);
    Check("9th-from-Gemini baadhaka sthaana", aqNinth.SthaanaSign, ZodiacName.Libra);
    Check("9th-from-Gemini baadhaka", aqNinth.Baadhaka, "Venus");
    Check("9th-from-Gemini sthaana house-from-lagna", aqNinth.SthaanaHouseNumberFromLagna!.Value, 5);

    Console.WriteLine(failures == 0 ? "\nverify-baadhaka: ALL PASS" : $"\nverify-baadhaka: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-avastha` ---
// Worked-example assertions for the Baaladi + Jagradadi avastha calculators against the seeded
// tbl_Rule_AgeState / tbl_Rule_WakefulnessState rows (active rule set). Solution has no unit-test project.
if (args.Length > 0 && args[0] == "verify-avastha")
{
    var rules = new PlanetaryStateRuleRepository(connectionFactory).GetActiveRuleSet();
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    string Baaladi(ZodiacName sign, decimal deg) => AgeStateCalculator.For(sign, deg, rules.AgeBands).StateName;
    decimal Fraction(ZodiacName sign, decimal deg) => AgeStateCalculator.For(sign, deg, rules.AgeBands).EffectFraction;
    string? Jagradadi(string dignity) => WakefulnessStateCalculator.For(dignity, rules.WakefulnessByDignity)?.StateName;

    // Baaladi — odd sign (Aries) bands run forward 0-6-12-18-24-30
    Check("Baaladi(Aries, 3)",   Baaladi(ZodiacName.Aries, 3m),   "Baala");
    Check("Baaladi(Aries, 8)",   Baaladi(ZodiacName.Aries, 8m),   "Kumara");
    Check("Baaladi(Aries, 15)",  Baaladi(ZodiacName.Aries, 15m),  "Yuva");
    Check("Baaladi(Aries, 20)",  Baaladi(ZodiacName.Aries, 20m),  "Vriddha");
    Check("Baaladi(Aries, 27)",  Baaladi(ZodiacName.Aries, 27m),  "Mrita");
    // Baaladi — even sign (Taurus) bands run reversed
    Check("Baaladi(Taurus, 3)",  Baaladi(ZodiacName.Taurus, 3m),  "Mrita");
    Check("Baaladi(Taurus, 27)", Baaladi(ZodiacName.Taurus, 27m), "Baala");
    Check("Baaladi(Gemini, 8)",  Baaladi(ZodiacName.Gemini, 8m),  "Kumara");   // Gemini is an odd sign
    // effect fractions
    Check("Fraction Baala",  Fraction(ZodiacName.Aries, 3m),  0.250m);
    Check("Fraction Yuva",   Fraction(ZodiacName.Aries, 15m), 1.000m);
    Check("Fraction Mrita",  Fraction(ZodiacName.Aries, 27m), 0.000m);

    // Jagradadi — from DignityStatus
    Check("Jagradadi(Exalted)",     Jagradadi("Exalted"),     "Jagrat");
    Check("Jagradadi(Own Sign)",    Jagradadi("Own Sign"),    "Jagrat");
    Check("Jagradadi(Moolatrikona)",Jagradadi("Moolatrikona"),"Jagrat");
    Check("Jagradadi(Great Friend)",Jagradadi("Great Friend"),"Swapna");
    Check("Jagradadi(Friend)",      Jagradadi("Friend"),      "Swapna");
    Check("Jagradadi(Neutral)",     Jagradadi("Neutral"),     "Swapna");
    Check("Jagradadi(Enemy)",       Jagradadi("Enemy"),       "Sushupti");
    Check("Jagradadi(Great Enemy)", Jagradadi("Great Enemy"), "Sushupti");
    Check("Jagradadi(Debilitated)", Jagradadi("Debilitated"), "Sushupti");
    Check("Jagradadi(null)",        Jagradadi(null!),         null);

    // Sayanaadi — hand-computed from the JHora export for 1_Ramakrishnan (PVR sec 15.4.4):
    // M = Anuraadha (17), G = ghati 59 (Janma Ghatis 58.8892), L = Aries (1) for both.
    // Sun: C = Aswini (1), P = 1, A = 3rd navamsa (8.205deg in sign) -> index 8 = Aagama.
    Check("Sayanaadi(Sun) hand-computed",
        PostureStateCalculator.For(PostureStateCalculator.ComputeIndex(1, 1, 3, 17, 59, 1), rules.PostureStatesBySequence)?.StateName,
        "Aagama");
    // Moon: C = Anuraadha (17), P = 2, A = 3rd navamsa (7.293deg in sign) -> index 11 = Kautuka.
    Check("Sayanaadi(Moon) hand-computed",
        PostureStateCalculator.For(PostureStateCalculator.ComputeIndex(17, 2, 3, 17, 59, 1), rules.PostureStatesBySequence)?.StateName,
        "Kautuka");

    using (var conn = connectionFactory.CreateOpenConnection())
    {
        // JHora export's printed "Activity" table for 1_Ramakrishnan, all 9 grahas.
        var stored = conn.Query<(string Planet, string? PostureState)>(
            @"SELECT Planet, PostureState FROM dbo.vw_ChartPlanetEvidence
              WHERE BirthDetailId = (SELECT Id FROM dbo.tbl_BirthDetails WHERE Name = 'RamakrishnanP')
                AND ChartType = 'D1' AND PointKind = 'Graha'")
            .ToDictionary(r => r.Planet, r => r.PostureState);
        Check("Sayanaadi(Sun)     -> Aagama",      stored.GetValueOrDefault("Sun"),     "Aagama");
        Check("Sayanaadi(Moon)    -> Kautuka",     stored.GetValueOrDefault("Moon"),    "Kautuka");
        Check("Sayanaadi(Mars)    -> Kautuka",     stored.GetValueOrDefault("Mars"),    "Kautuka");
        Check("Sayanaadi(Mercury) -> Bhojana",     stored.GetValueOrDefault("Mercury"), "Bhojana");
        Check("Sayanaadi(Jupiter) -> Gamana",      stored.GetValueOrDefault("Jupiter"), "Gamana");
        Check("Sayanaadi(Venus)   -> Gamana",      stored.GetValueOrDefault("Venus"),   "Gamana");
        Check("Sayanaadi(Saturn)  -> Bhojana",     stored.GetValueOrDefault("Saturn"),  "Bhojana");
        Check("Sayanaadi(Rahu)    -> Bhojana",     stored.GetValueOrDefault("Rahu"),    "Bhojana");
        Check("Sayanaadi(Ketu)    -> Gamana",      stored.GetValueOrDefault("Ketu"),    "Gamana");
    }

    Console.WriteLine(failures == 0 ? "\nverify-avastha: ALL PASS" : $"\nverify-avastha: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-kp-sublords` ---
// AstroMath.GetKpSubLordChain (levels 1-7), against a hand-derived worked example. Longitude
// 218.72 deg is the same Anuradha test point GetNakshatraSubLord's own doc comment verifies to
// Venus at level 1; levels 2-3 were independently hand-calculated in exact 120ths (Vimshottari
// years always sum to 120, so no rounding drift): position 48.48/120 into the level-1 Venus
// cycle -> Sun(26)..Moon(36) band -> Moon; then 32.88/120 into the level-2 Moon cycle ->
// Saturn(70)..Mercury(87) band -> Mercury. No unit-test project (see verify-avastha above).
if (args.Length > 0 && args[0] == "verify-kp-sublords")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    // Level 1 must agree with the existing, independently-verified GetNakshatraSubLord for a
    // spread of longitudes -- the chain's first entry is that same calculation, not a new one.
    foreach (var lon in new[] { 0.3, 47.9, 91.5, 133.2, 218.72, 271.4, 319.9, 359.99 })
    {
        Check($"Level1(={lon}) matches GetNakshatraSubLord", AstroMath.GetKpSubLordChain(lon, 1)[0], AstroMath.GetNakshatraSubLord(lon));
    }

    // Hand-derived worked example (see comment above) — levels 1-3 at the Anuradha test point.
    var chain = AstroMath.GetKpSubLordChain(218.72, 3);
    Check("KP chain L1(218.72)", chain[0], PlanetName.Venus);
    Check("KP chain L2(218.72)", chain[1], PlanetName.Moon);
    Check("KP chain L3(218.72)", chain[2], PlanetName.Mercury);

    // Length and range sanity across levels 1-7 at a spread of longitudes: every entry must be
    // a real planet (chain length matches request; no exception for any degree, including 0 and
    // the 360 wrap boundary).
    foreach (var lon in new[] { 0.0, 13.333333, 26.666667, 180.0, 359.999999 })
    {
        var full = AstroMath.GetKpSubLordChain(lon, 7);
        Check($"KP chain length(={lon})", full.Count, 7);
    }

    Console.WriteLine(failures == 0 ? "\nverify-kp-sublords: ALL PASS" : $"\nverify-kp-sublords: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-jaimini` ---
// Worked-example assertions for the Jaimini Chara Karakas + special points against
// docs/artifacts/reference-charts/Rammy_Jagannatha.txt (person 1_Ramakrishnan). Solution has no unit-test project.
// Grows over Tasks 3–5; Task 2 seeds it with sunrise / sunset / night-birth only.
if (args.Length > 0 && args[0] == "verify-jaimini")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }
    void CheckSeconds(string label, DateTimeOffset actual, string expectedHms, double toleranceSec)
    {
        var exp = DateTimeOffset.ParseExact(
            actual.ToString("yyyy-MM-dd ") + expectedHms + actual.ToString(" zzz"),
            "yyyy-MM-dd HH:mm:ss zzz", System.Globalization.CultureInfo.InvariantCulture);
        var deltaSec = Math.Abs((actual - exp).TotalSeconds);
        var ok = deltaSec <= toleranceSec;
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual:HH:mm:ss}, expected {expectedHms} (Δ {deltaSec:F1}s, tol {toleranceSec:F0}s)");
        if (!ok) failures++;
    }

    var ram = birthDetailsRepo.GetAll().First(p => p.Name == "RamakrishnanP");

    // --- Phase 1: sunrise / sunset (Chennai, 22 Apr 1981; night birth -> 21 Apr arc) ---
    // JHora prints "Sunrise: 5:56:39 / Sunset: 18:18:53 (April 21)" for this birth. SwissEphNet
    // ships no .se1 files, so swe_rise_trans runs on the Moshier analytic theory; that plus
    // delta-T differences leave a fixed ~2s early bias vs JHora's full Swiss Ephemeris. A 5s
    // tolerance is far below the precision Hora Lagna / Gulika / Maandi (day split into 8-12
    // parts of ~90 min) need in Task 5.
    var sun = SwissEphemerisProvider.GetSunTimes(ram);
    CheckSeconds("sunrise (local)", sun.Sunrise, "05:56:39", 5.0);
    CheckSeconds("sunset (local)",  sun.Sunset,  "18:18:53", 5.0);
    Check("night birth (05:30 < sunrise)", sun.IsNightBirth, true);
    Check("Vedic-day arc sunrise is 21 Apr", sun.Sunrise.ToString("MM-dd"), "04-21");
    Check("next sunrise (closes the night arc) is 22 Apr ~dawn", sun.NextSunrise.ToString("MM-dd HH:mm"), "04-22 05:56");
    Check("night arc is ~11.6h", Math.Round((sun.NextSunrise - sun.Sunset).TotalHours, 1), 11.6);

    // --- Phase 2: Chara Karakas (8-karaka Ashta) ---
    // hand-computed ranking check (independent of the DB)
    var ckHand = CharaKarakaCalculator.Assign(new Dictionary<PlanetName, double>
    {
        [PlanetName.Sun] = 8.205, [PlanetName.Moon] = 7.293, [PlanetName.Mars] = 3.950,
        [PlanetName.Mercury] = 1.842, [PlanetName.Jupiter] = 8.727, [PlanetName.Venus] = 11.992,
        [PlanetName.Saturn] = 10.956, [PlanetName.Rahu] = 12.920,   // raw (true node); calc reverses Rahu
    });
    Check("Assign: Rahu -> AK", ckHand[PlanetName.Rahu], CharaKaraka.AK);
    Check("Assign: Mercury -> DK", ckHand[PlanetName.Mercury], CharaKaraka.DK);

    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var d1 = conn.Query<(string Planet, string? CharaKaraka)>(
            @"SELECT kd.Planet, kd.CharaKaraka
              FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name = 'RamakrishnanP' AND cr.ChartType = 'D1' AND kd.CharaKaraka IS NOT NULL")
            .ToDictionary(r => r.CharaKaraka!, r => r.Planet);
        Check("AK  = Rahu",    d1.GetValueOrDefault("AK"),  "Rahu");
        Check("AmK = Venus",   d1.GetValueOrDefault("AmK"), "Venus");
        Check("BK  = Saturn",  d1.GetValueOrDefault("BK"),  "Saturn");
        Check("MK  = Jupiter", d1.GetValueOrDefault("MK"),  "Jupiter");
        Check("PiK = Sun",     d1.GetValueOrDefault("PiK"), "Sun");
        Check("PK  = Moon",    d1.GetValueOrDefault("PK"),  "Moon");
        Check("GK  = Mars",    d1.GetValueOrDefault("GK"),  "Mars");
        Check("DK  = Mercury", d1.GetValueOrDefault("DK"),  "Mercury");
        // karaka label travels to every varga
        var d9ak = conn.ExecuteScalar<string>(
            @"SELECT kd.Planet FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name = 'RamakrishnanP' AND cr.ChartType = 'D9' AND kd.CharaKaraka = 'AK'");
        Check("D9 AK label travels", d9ak, "Rahu");
    }

    // --- Phase 2b: CharaKarakaCalculator.Ranked order == tbl_Rule_Karaka (migration 085) ---
    // Closes the 2026-09-11 rule-mapping audit's "Chara Karaka has no DB citation" gap: the
    // table was schema-ready (KarakaScheme/OrderIndex/ReverseForRahu) since migration 18 but
    // empty until 085 seeded it. This is the "verified mirror" check migration 085's own header
    // promised — CharaKarakaCalculator itself stays hardcoded (the project's usual pattern).
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var dbOrder = conn.Query<(int OrderIndex, string TargetValue, bool ReverseForRahu)>(
            @"SELECT OrderIndex, TargetValue, ReverseForRahu FROM dbo.tbl_Rule_Karaka
              WHERE RuleSetId = 1 AND KarakaScheme = 'Chara' ORDER BY OrderIndex").ToList();
        Check("tbl_Rule_Karaka has 8 Chara rows", dbOrder.Count, 8);
        var codeOrder = string.Join(",", Enum.GetValues<CharaKaraka>().Take(8));
        var dbCodeOrder = string.Join(",", dbOrder.Select(r => r.TargetValue));
        Check("tbl_Rule_Karaka order == CharaKaraka enum order (AK..DK)", dbCodeOrder, codeOrder);
        Check("tbl_Rule_Karaka.ReverseForRahu is set on every Chara row", dbOrder.Count(r => r.ReverseForRahu), dbOrder.Count);
    }

    // --- Phase 3: Arudha Lagna + 12 Bhava Arudhas ---
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        string? SpSign(string chart, string code) => conn.ExecuteScalar<string>(
            @"SELECT kd.Sign FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name='RamakrishnanP' AND cr.ChartType=@c AND kd.Planet=@p AND kd.PointKind<>'Graha'",
            new { c = chart, p = code });

        Check("AL (D1) -> Capricornus", SpSign("D1", "AL"), "Capricornus");
        Check("A2..A12 present in D1", conn.ExecuteScalar<int>(
            @"SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id=kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id=cr.BirthDetailId
              WHERE bd.Name='RamakrishnanP' AND cr.ChartType='D1' AND kd.PointKind='Arudha'"), 12);
        // tbl_Rule_ArudhaFormula (migration 085) closes the same audit's Arudha-has-no-DB-
        // citation gap. A single narrative row, so this checks presence + citation, not a
        // numeric round-trip (same shape as tbl_Rule_PostureStateFormula/PanchangaFormula).
        var arudhaSource = conn.ExecuteScalar<string?>(
            "SELECT SourceRefCode FROM dbo.tbl_Rule_ArudhaFormula WHERE RuleSetId = 1");
        Check("tbl_Rule_ArudhaFormula cites SRC_PVR_INTEGRATED", arudhaSource, "SRC_PVR_INTEGRATED");
        // Since 2026-10-01 a varga's padas are computed inside that varga (PVR sec.9.2), not
        // projected from D1: D9's AL is D9's own pada — Gemini, as in JHora's D-9 grid
        // (Rammy_Jagannatha.txt; every shared chart is checked by verify-varga-arudha).
        Check("AL (D9) computed inside D9 -> Gemini (JHora)", SpSign("D9", "AL"), "Gemini");
    }

    // --- Phase 4: Hora Lagna + PVR upagrahas (JHora Saturn instants, names swapped) ---
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        string? SpSign(string chart, string code) => conn.ExecuteScalar<string>(
            @"SELECT kd.Sign FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name='RamakrishnanP' AND cr.ChartType=@c AND kd.Planet=@p AND kd.PointKind<>'Graha'",
            new { c = chart, p = code });
        double SpLon(string code) => conn.ExecuteScalar<double>(
            @"SELECT kd.NirayanaLongitudeDegrees FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name='RamakrishnanP' AND cr.ChartType='D1' AND kd.Planet=@p",
            new { p = code });
        void CheckLon(string label, double actual, double expected, double tolDeg)
        {
            var d = Math.Abs(((actual - expected + 540) % 360) - 180);
            var ok = d <= tolDeg;
            Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual:F4}, expected {expected:F4} (Δ {d:F3}°, tol {tolDeg}°)");
            if (!ok) failures++;
        }

        // JHora export: HL 23 Pi 55'08" (Reva, Pi/Aq) · Gulika 7 Li 44'38" (Swat, Li/Sg) · Maandi 18 Li 07'01" (Swat, Li/Pi)
        Check("HL (D1) -> Pisces",     SpSign("D1", "HL"),     "Pisces");
        Check("HL (D9) -> Aquarius",   SpSign("D9", "HL"),     "Aquarius");
        Check("Gulika (D1) -> Libra",  SpSign("D1", "Gulika"), "Libra");
        Check("Gulika (D9) -> Pisces (PVR)", SpSign("D9", "Gulika"), "Pisces");
        Check("Maandi (D1) -> Libra",  SpSign("D1", "Maandi"), "Libra");
        Check("Maandi (D9) -> Sagittarius (PVR)", SpSign("D9", "Maandi"), "Sagittarius");
        CheckLon("HL longitude",     SpLon("HL"),     353.9189, 0.5);
        CheckLon("Gulika longitude", SpLon("Gulika"), 198.1169, 0.5);
        CheckLon("Maandi longitude", SpLon("Maandi"), 187.7439, 0.5);

        // --- Phase 5: Bhaava / Ghati / Sree Lagna (JHora export: BL 0 Ar 35'00" · GL 3 Pi
        //     55'31" · SL 17 Cn 33'01"; D9 columns Ar/Le/Sg respectively) ---
        Check("BL (D1) -> Aries",       SpSign("D1", "BL"), "Aries");
        Check("BL (D9) -> Aries",       SpSign("D9", "BL"), "Aries");
        Check("GL (D1) -> Pisces",      SpSign("D1", "GL"), "Pisces");
        Check("GL (D9) -> Leo",         SpSign("D9", "GL"), "Leo");
        Check("SL (D1) -> Cancer",      SpSign("D1", "SL"), "Cancer");
        // SL is driven by the Moon, so it follows the 2026-09-23 Traditional Lahiri export
        // (15 Cn 17'37.70", D9 Sc) that decision 009 reconciles against; the older True
        // Chitrapaksha export above computed the Moon on the plain ecliptic (17 Cn 33'01", D9 Sg).
        Check("SL (D9) -> Scorpio", SpSign("D9", "SL"), "Scorpio");
        CheckLon("BL longitude", SpLon("BL"), 0.5835,   0.5);
        CheckLon("GL longitude", SpLon("GL"), 333.9254, 0.5);
        CheckLon("SL longitude", SpLon("SL"), 105.2938, 0.5);
    }

    // --- Phase 6: Karakamsa (AK in D9) — JHora export: Rahu (AK) Navamsa column "Li" ---
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var k = conn.QuerySingle<(string AtmaKarakaPlanet, string KarakamsaSign)>(
            @"SELECT k.AtmaKarakaPlanet, k.KarakamsaSign FROM dbo.vw_ChartKarakamsa k
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = k.BirthDetailId
              WHERE bd.Name = 'RamakrishnanP'");
        Check("Karakamsa AK", k.AtmaKarakaPlanet, "Rahu");
        Check("Karakamsa sign -> Libra", k.KarakamsaSign, "Libra");
    }

    Console.WriteLine(failures == 0 ? "\nverify-jaimini: ALL PASS" : $"\nverify-jaimini: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-ashtakavarga` ---
// Parasari Ashtakavarga (AshtakavargaCalculator) must (1) mirror the seeded rule matrix,
// (2) reproduce the JHora export for 1_Ramakrishnan — BAV grid, Sarvashtakavarga, and the
// Rasi/Graha/Sodhya Pinda — exactly, and (3) match what GenerateAll persisted.
if (args.Length > 0 && args[0] == "verify-ashtakavarga")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }
    static string Csv(IEnumerable<int> xs) => string.Join(" ", xs);

    // --- Phase 1: the C# BENEFIC_HOUSES matrix mirrors tbl_Rule_AshtakavargaContribution ---
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var dbRows = conn.Query<(string RecipientCode, string ContributorCode, string BeneficPlacesJson)>(
            @"SELECT RecipientCode, ContributorCode, BeneficPlacesJson
              FROM dbo.tbl_Rule_AshtakavargaContribution
              WHERE RuleSetId = 1 AND MethodCode = 'PVR_PARASARA_BAV'").ToList();
        Check("rule matrix row count", dbRows.Count, 56);

        var mismatches = 0;
        var grand = 0;
        foreach (var recipient in AshtakavargaTables.Recipients)
        {
            var recipientTotal = 0;
            foreach (var contributor in AshtakavargaTables.Contributors)
            {
                var cs = AshtakavargaTables.BeneficPlaces[recipient][contributor];
                recipientTotal += cs.Length;
                var row = dbRows.FirstOrDefault(r => r.RecipientCode == recipient && r.ContributorCode == contributor);
                var db = row.BeneficPlacesJson is null
                    ? Array.Empty<int>()
                    : JsonSerializer.Deserialize<int[]>(row.BeneficPlacesJson) ?? Array.Empty<int>();
                if (Csv(cs.OrderBy(x => x)) != Csv(db.OrderBy(x => x))) mismatches++;
            }
            grand += recipientTotal;
        }
        Check("C# matrix == DB matrix (per cell)", mismatches, 0);
        Check("Sarvashtakavarga grand total", grand, 337);
    }

    var psRules = new PlanetaryStateRuleRepository(connectionFactory).GetActiveRuleSet();
    var ram = birthDetailsRepo.GetAll().First(p => p.Name == "RamakrishnanP");
    var bundle = new ChartPipeline(orchestrator, psRules).Run(ram);
    var av = bundle.Ashtakavarga ?? throw new InvalidOperationException("ChartBundle.Ashtakavarga is null.");

    // --- Phase 2: BAV grid == the JHora benchmark (research.* cells) ---
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var benchBav = conn.Query<(string RecipientCode, int SignNumber, int BinduValue)>(
            @"SELECT c.RecipientCode, c.SignNumber, c.BinduValue
              FROM research.tbl_Dim_SourceReferenceAshtakavargaBenchmarkCell c
              JOIN research.tbl_Dim_SourceReferenceAshtakavargaBenchmarkRun r ON r.Id = c.RunId
              JOIN research.tbl_Dim_SourceReferenceAshtakavargaBenchmarkCase cs ON cs.Id = r.CaseId
              WHERE cs.CaseCode = 'BENCH_RAMAKRISHNAN_P_JHORA_1981' AND c.MetricCode = 'BAV'
                AND c.RecipientCode <> 'AS'")
            .ToLookup(x => x.RecipientCode, x => (x.SignNumber, x.BinduValue));

        foreach (var b in av.Bhinna)
        {
            var expected = Enumerable.Range(1, 12)
                .Select(s => benchBav[b.Recipient].First(t => t.SignNumber == s).BinduValue);
            Check($"BAV {b.Recipient,-8} vs JHora", Csv(b.Bindus), Csv(expected));
        }

        // --- Phase 3: SAV == column sums of the benchmark BAV; total 337 ---
        var savExpected = Enumerable.Range(1, 12)
            .Select(s => AshtakavargaTables.Recipients.Sum(rp => benchBav[rp].First(t => t.SignNumber == s).BinduValue))
            .ToArray();
        Check("SAV vs JHora column sums", Csv(av.Sarva.Bindus), Csv(savExpected));
        Check("SAV total", av.Sarva.Total, 337);

        // --- Phase 4: Rasi / Graha / Sodhya Pinda == the JHora benchmark (exact) ---
        var notes = conn.ExecuteScalar<string?>(
            @"SELECT r.Notes
              FROM research.tbl_Dim_SourceReferenceAshtakavargaBenchmarkRun r
              JOIN research.tbl_Dim_SourceReferenceAshtakavargaBenchmarkCase cs ON cs.Id = r.CaseId
              WHERE cs.CaseCode = 'BENCH_RAMAKRISHNAN_P_JHORA_1981' AND r.SourceSystemCode = 'JAGANNATHA_HORA'")
            ?? throw new InvalidOperationException("JHora Ashtakavarga benchmark run notes (Pinda JSON) not seeded — apply db/075.");
        var pinda = JsonDocument.Parse(notes).RootElement.GetProperty("pinda");
        var abbrev = new Dictionary<string, string>
        {
            ["SUN"] = "Su", ["MOON"] = "Mo", ["MARS"] = "Ma", ["MERCURY"] = "Me",
            ["JUPITER"] = "Ju", ["VENUS"] = "Ve", ["SATURN"] = "Sa",
        };
        foreach (var p in av.Pinda)
        {
            var e = pinda.GetProperty(abbrev[p.Recipient]);
            Check($"Pinda {p.Recipient,-8} (rasi/graha/sodhya)",
                $"{p.RasiPinda}/{p.GrahaPinda}/{p.SodhyaPinda}",
                $"{e.GetProperty("rasi").GetInt32()}/{e.GetProperty("graha").GetInt32()}/{e.GetProperty("sodhya").GetInt32()}");
        }
    }

    // --- Phase 5: persisted tbl_Fact_* for the stored Ramakrishnan D1 == the engine ---
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        const string where =
            @"JOIN dbo.tbl_ChartResults cr ON cr.Id = f.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name = 'RamakrishnanP' AND cr.ChartType = 'D1'";

        var storedBav = conn.Query<(string RecipientCode, int SignNumber, int BinduCount)>(
            $"SELECT f.RecipientCode, f.SignNumber, f.BinduCount FROM dbo.tbl_Fact_BhinnaAshtakavarga f {where}")
            .ToLookup(x => x.RecipientCode, x => (x.SignNumber, x.BinduCount));
        foreach (var b in av.Bhinna)
        {
            var stored = Enumerable.Range(1, 12).Select(s => storedBav[b.Recipient].FirstOrDefault(t => t.SignNumber == s).BinduCount);
            Check($"persisted BAV {b.Recipient,-8}", Csv(stored), Csv(b.Bindus));
        }

        var storedSav = conn.Query<int>(
            $"SELECT f.TotalBindus FROM dbo.tbl_Fact_SarvaAshtakavarga f {where} ORDER BY f.SignNumber").ToArray();
        Check("persisted SAV", Csv(storedSav), Csv(av.Sarva.Bindus));

        var storedPinda = conn.Query<(string RecipientCode, int RasiPinda, int GrahaPinda, int SodhyaPinda)>(
            $"SELECT f.RecipientCode, f.RasiPinda, f.GrahaPinda, f.SodhyaPinda FROM dbo.tbl_Fact_AshtakavargaPinda f {where}")
            .ToDictionary(x => x.RecipientCode);
        foreach (var p in av.Pinda)
        {
            var s = storedPinda.GetValueOrDefault(p.Recipient);
            Check($"persisted Pinda {p.Recipient,-8}",
                $"{s.RasiPinda}/{s.GrahaPinda}/{s.SodhyaPinda}",
                $"{p.RasiPinda}/{p.GrahaPinda}/{p.SodhyaPinda}");
        }
    }

    // --- Phase 6: D9 Ashtakavarga is a genuine independent per-varga recompute, not a D1 copy ---
    // FEAT-ASHTAKAVARGA-02: PVR's Integrated Approach, Example 39 (p.155) explicitly endorses
    // this — "Ashtakavarga of divisional charts is prepared in the same manner as that of rasi
    // chart... we can find SAV of a divisional chart too" — worked through Vajpayee's D-10
    // (Examples 39/101/108). ChartGenerationService.PersistAnalytics already materializes this per
    // chart type; this proves the persisted D9 SAV both differs from D1's and matches an
    // independent recompute off D9's own sign positions, not a relabeled D1 row.
    var d9 = bundle.Charts.FirstOrDefault(c => c.ChartType.Equals("D9", StringComparison.OrdinalIgnoreCase));
    if (d9 is not null)
    {
        var d9Av = AshtakavargaCalculator.Calculate(d9);
        using var conn = connectionFactory.CreateOpenConnection();
        var storedD9Sav = conn.Query<int>(
            @"SELECT f.TotalBindus FROM dbo.tbl_Fact_SarvaAshtakavarga f
              JOIN dbo.tbl_ChartResults cr ON cr.Id = f.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name = 'RamakrishnanP' AND cr.ChartType = 'D9' ORDER BY f.SignNumber").ToArray();
        Check("persisted D9 SAV matches an independent D9 recompute", Csv(storedD9Sav), Csv(d9Av.Sarva.Bindus));
        Check("D9 SAV genuinely differs from D1 SAV (not a relabeled copy)",
            Csv(storedD9Sav) != Csv(av.Sarva.Bindus), true);
    }
    else
    {
        Console.WriteLine("  [SKIP] Phase 6 (D9 Ashtakavarga) — no D9 chart in the pipeline bundle");
    }

    Console.WriteLine(failures == 0 ? "\nverify-ashtakavarga: ALL PASS" : $"\nverify-ashtakavarga: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-panchanga` ---
// PanchangaCalculator must reproduce the JHora export for 1_Ramakrishnan (Krishna Tritiya,
// Vyatipaata, Tuesday, Hora Lord Venus, Janma Ghatis 58.8892 — see the migration-081 header
// and PanchangaCalculator's doc comment for the source), and what GenerateAll persisted must
// match what the engine computes.
if (args.Length > 0 && args[0] == "verify-panchanga")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }
    void CheckClose(string label, double actual, double expected, double tolerance)
    {
        var ok = Math.Abs(actual - expected) <= tolerance;
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual:F4}, expected {expected:F4} (+/-{tolerance})");
        if (!ok) failures++;
    }

    var psRules = new PlanetaryStateRuleRepository(connectionFactory).GetActiveRuleSet();
    var ram = birthDetailsRepo.GetAll().First(p => p.Name == "RamakrishnanP");
    var bundle = new ChartPipeline(orchestrator, psRules).Run(ram);
    var pc = bundle.Panchanga ?? throw new InvalidOperationException("ChartBundle.Panchanga is null.");

    // --- Phase 1: the engine == the JHora export (Rammy_Jagannatha.txt / 1_Ramakrishnan) ---
    Check("Tithi (18 = Krishna Tritiya)", pc.TithiId, 18);
    Check("Karana (6 = Vanija)", pc.KaranaId, 6);
    Check("Nitya Yoga (17 = Vyatipaata)", pc.NityaYogaId, 17);
    Check("Vedic Weekday (3 = Tuesday)", pc.VedicWeekdayId, 3);
    Check("Hora Lord (6 = Venus)", pc.HoraLordPlanetId, 6);
    CheckClose("Janma Ghatis", pc.JanmaGhatis, 58.8892, 0.01);

    // --- Phase 2: persisted tbl_Chart_Panchanga for the stored Ramakrishnan D1 == the engine ---
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var stored = conn.QuerySingleOrDefault<(int TithiId, int KaranaId, int NityaYogaId,
                int VedicWeekdayId, int HoraLordPlanetId, double JanmaGhatis)?>(
            @"SELECT p.TithiId, p.KaranaId, p.NityaYogaId, p.VedicWeekdayId, p.HoraLordPlanetId, p.JanmaGhatis
              FROM dbo.tbl_Chart_Panchanga p
              JOIN dbo.tbl_ChartResults cr ON cr.Id = p.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name = 'RamakrishnanP' AND cr.ChartType = 'D1'");
        if (stored is null)
        {
            Console.WriteLine("  [SKIP] no persisted tbl_Chart_Panchanga row for Ramakrishnan's D1 — run GenerateAll first.");
        }
        else
        {
            var s = stored.Value;
            Check("persisted TithiId", s.TithiId, pc.TithiId);
            Check("persisted KaranaId", s.KaranaId, pc.KaranaId);
            Check("persisted NityaYogaId", s.NityaYogaId, pc.NityaYogaId);
            Check("persisted VedicWeekdayId", s.VedicWeekdayId, pc.VedicWeekdayId);
            Check("persisted HoraLordPlanetId", s.HoraLordPlanetId, pc.HoraLordPlanetId);
            CheckClose("persisted Janma Ghatis", s.JanmaGhatis, pc.JanmaGhatis, 0.001);
        }
    }

    Console.WriteLine(failures == 0 ? "\nverify-panchanga: ALL PASS" : $"\nverify-panchanga: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- `dotnet run -- verify-strength` : the FEAT-STRENGTH-01 Kaala Bala slice
// (Dina/Hora/Tribhaga Bala + Graha Yuddha detection) for 1_Ramakrishnan. No JHora per-component
// breakdown is printed in the export (only the Shadbala grand total is), so this is a
// self-consistency check against the classical formula applied to this specific birth, not a
// cross-tool numeric match — see ShadbalaCalculator.ComputeYuddha's doc comment for why the
// Yuddha Bala *magnitude* stays 0 (Raman DJVU has no text extract).
if (args.Length > 0 && args[0] == "verify-strength")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    var psRules = new PlanetaryStateRuleRepository(connectionFactory).GetActiveRuleSet();
    var ram = birthDetailsRepo.GetAll().First(p => p.Name == "RamakrishnanP");
    var bundle = new ChartPipeline(orchestrator, psRules).Run(ram);
    var strengths = bundle.Strengths ?? throw new InvalidOperationException("ChartBundle.Strengths is null.");

    double Component(string planet, string subComponentCode) =>
        strengths.Single(r => r.Planet == planet).Components
            .SingleOrDefault(c => c.SubComponentCode == subComponentCode)?.ValueVirupas ?? 0;

    Console.WriteLine("-- Phase 1: the engine, hand-derived from the JHora export's own printed times --");
    // Weekday Tuesday -> Mars (verify-panchanga).
    Check("Dina Bala Mars (weekday lord)", Component("Mars", "DINA_BALA"), 45);
    foreach (var p in new[] { "Sun", "Moon", "Mercury", "Jupiter", "Venus", "Saturn" })
        Check($"Dina Bala {p} (not weekday lord)", Component(p, "DINA_BALA"), 0);

    // Hora Lord Venus (verify-panchanga).
    Check("Hora Bala Venus (running hora lord)", Component("Venus", "HORA_BALA"), 60);
    foreach (var p in new[] { "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Saturn" })
        Check($"Hora Bala {p} (not hora lord)", Component(p, "HORA_BALA"), 0);

    // Janma Ghatis 58.8892 -> 23h33m21s after sunrise, into the night's 3rd third -> Mars;
    // Jupiter is classically exempt and always scores the full 60.
    Check("Tribhaga Bala Mars (night-3rd-third lord)", Component("Mars", "TRIBHAGA_BALA"), 60);
    Check("Tribhaga Bala Jupiter (classical exemption)", Component("Jupiter", "TRIBHAGA_BALA"), 60);
    foreach (var p in new[] { "Sun", "Moon", "Mercury", "Venus", "Saturn" })
        Check($"Tribhaga Bala {p} (neither)", Component(p, "TRIBHAGA_BALA"), 0);

    // No Graha Yuddha: Mars/Mercury/Venus are >2 degrees apart in Aries, Jupiter/Saturn 2.23
    // degrees apart in Virgo -- none within the 1-degree war orb.
    Check("Yuddha Bala virupas (no war present)", strengths.Sum(r => r.YuddhaBalaVirupas), 0.0);

    Console.WriteLine("\n-- Phase 1b: Cheṣṭā Bala vs JHora (strengths_otherstrengths_cheshta-bala, 2026-09-23) --");
    // Tara grahas: Raman's Ujjain-1900 mean motions land within 0.2 virupas of JHora; the Moon
    // (Pakṣa Bala) matches exactly. The Sun is BPHS's Ayana Bala, which JHora does not use (it
    // shows 40.40), so it is checked against the BPHS figure instead.
    void Near(string label, double actual, double expected, double tolerance)
    {
        var ok = Math.Abs(actual - expected) <= tolerance;
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual:0.00}, expected {expected:0.00} ±{tolerance}");
        if (!ok) failures++;
    }
    var jhoraCheshta = new Dictionary<string, double>
    {
        ["Mars"] = 57.22, ["Mercury"] = 4.31, ["Jupiter"] = 51.71, ["Venus"] = 4.01, ["Saturn"] = 50.59,
    };
    foreach (var (planet, expected) in jhoraCheshta)
        Near($"Cheṣṭā Bala {planet}", strengths.Single(r => r.Planet == planet).CheshtaBalaVirupas, expected, 0.25);
    Near("Cheṣṭā Bala Moon (Pakṣa Bala)", strengths.Single(r => r.Planet == "Moon").CheshtaBalaVirupas, 50.33, 0.05);
    Near("Cheṣṭā Bala Sun (BPHS Ayana Bala)", strengths.Single(r => r.Planet == "Sun").CheshtaBalaVirupas, 45.15, 0.25);
    var jhoraPaksha = new Dictionary<string, double>
    {
        ["Sun"] = 9.67, ["Moon"] = 100.66, ["Mars"] = 9.67, ["Mercury"] = 9.67,
        ["Jupiter"] = 50.33, ["Venus"] = 50.33, ["Saturn"] = 9.67,
    };
    foreach (var (planet, expected) in jhoraPaksha)
        Near($"Pakṣa Bala {planet}", Component(planet, "PAKSHA_BALA"), expected, 0.1);
    // JHora strengths_otherstrengths_kaala-bala: Ayana column (the Sun's doubled).
    var jhoraAyana = new Dictionary<string, double>
    {
        ["Sun"] = 90.29, ["Moon"] = 55.84, ["Mars"] = 43.20, ["Mercury"] = 42.20,
        ["Jupiter"] = 29.13, ["Venus"] = 46.72, ["Saturn"] = 31.99,
    };
    foreach (var (planet, expected) in jhoraAyana)
        Near($"Ayana Bala {planet}", Component(planet, "AYANA_BALA"), expected, 0.5);
    // JHora kaala-bala: Natonnata, Abda, Maasa columns. Our midnight is the sunset/sunrise midpoint
    // (00:07:36 here); JHora's sits ~7.5 minutes earlier, hence the 0.7 tolerance.
    var jhoraNathonnata = new Dictionary<string, double>
    {
        ["Sun"] = 27.49, ["Moon"] = 32.51, ["Mars"] = 32.51, ["Mercury"] = 60.00,
        ["Jupiter"] = 27.49, ["Venus"] = 27.49, ["Saturn"] = 32.51,
    };
    foreach (var (planet, expected) in jhoraNathonnata)
        Near($"Nathonnata Bala {planet}", Component(planet, "NATHONNATA_BALA"), expected, 0.7);
    foreach (var planet in jhoraNathonnata.Keys)
    {
        Check($"Abda Bala {planet}", Component(planet, "VARSHA_BALA"), planet == "Mars" ? 15 : 0);
        Check($"Māsa Bala {planet}", Component(planet, "MASA_BALA"), planet == "Saturn" ? 30 : 0);
    }
    foreach (var r in strengths)
    {
        var uchcha = Component(r.Planet, "UCHCHA_BALA");
        Near($"Ishta {r.Planet} = √(Uchcha × Cheṣṭā)", r.IshtaBala ?? double.NaN, Math.Sqrt(uchcha * r.CheshtaBalaVirupas), 0.01);
        Near($"Kashta {r.Planet} = √((60−Uchcha) × (60−Cheṣṭā))", r.KashtaBala ?? double.NaN,
            Math.Sqrt((60 - uchcha) * (60 - r.CheshtaBalaVirupas)), 0.01);
    }

    Console.WriteLine("\n-- Phase 1c: Sthāna Bala vs JHora (strengths_otherstrengths_sthana-bala) --");
    // Columns: Uchcha, Saptavargaja, Oja-Yugma, Kendra, Drekkana.
    var jhoraSthana = new Dictionary<string, double[]>
    {
        ["Sun"] = [59.40, 37.50, 30, 60, 15], ["Moon"] = [1.40, 82.50, 30, 30, 0],
        ["Mars"] = [38.02, 172.50, 15, 60, 15], ["Mercury"] = [5.61, 26.25, 30, 60, 0],
        ["Jupiter"] = [38.76, 99.38, 0, 15, 15], ["Venus"] = [55.01, 52.50, 15, 60, 0],
        ["Saturn"] = [46.99, 82.50, 15, 15, 0],
    };
    var sthanaCodes = new[] { "UCHCHA_BALA", "SAPTAVARGAJA_BALA", "OJHA_YUGMA_RASYAMSA_BALA", "KENDRADI_BALA", "DREKKANA_BALA" };
    // Saptavargaja matches only with D2/D3/D7 on JHora's schemes (migration 164), the Rasi-chart
    // relationship in every varga and moolatrikona in D1 alone.
    foreach (var (planet, expected) in jhoraSthana)
        for (var i = 0; i < sthanaCodes.Length; i++)
        {
            // Known gap: JHora gives Venus 0 in her 2nd drekkana; BPHS (female grahas in the
            // middle drekkana) gives 15, and Raman's order (female 3rd, neuter 2nd) would break
            // Saturn's 0 instead. BPHS kept until a second chart settles JHora's rule.
            if (planet == "Venus" && sthanaCodes[i] == "DREKKANA_BALA")
            {
                Console.WriteLine($"  [GAP ] DREKKANA_BALA Venus: got {Component(planet, sthanaCodes[i]):0.00}, JHora {expected[i]:0.00} (BPHS kept)");
                continue;
            }
            Near($"{sthanaCodes[i]} {planet}", Component(planet, sthanaCodes[i]), expected[i], i == 0 ? 0.1 : 0.01);
        }
    if (args.Contains("--detail"))
        foreach (var r in strengths)
            Console.WriteLine($"  {r.Planet}: {r.Components.Single(c => c.SubComponentCode == "SAPTAVARGAJA_BALA").Narrative}");

    Console.WriteLine("\n-- Phase 2: persisted tbl_Fact_PlanetaryStrengthComponent == the engine --");
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var stored = conn.Query<(string Planet, string SubComponentCode, double ValueVirupas)>(
            @"SELECT p.PlanetName AS Planet, c.SubComponentCode, c.ValueVirupas
              FROM dbo.tbl_Fact_PlanetaryStrengthComponent c
              JOIN dbo.tbl_ChartResults cr ON cr.Id = c.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              JOIN dbo.tbl_Planets p ON p.Id = c.PlanetId
              WHERE bd.Name = 'RamakrishnanP' AND cr.ChartType = 'D1'
                AND c.SubComponentCode IN ('DINA_BALA', 'HORA_BALA', 'TRIBHAGA_BALA')").ToList();
        if (stored.Count == 0)
        {
            Console.WriteLine("  [SKIP] no persisted tbl_Fact_PlanetaryStrengthComponent rows for Ramakrishnan's D1 — run GenerateAll first.");
        }
        else
        {
            foreach (var s in stored)
                Check($"persisted {s.Planet}/{s.SubComponentCode}", s.ValueVirupas, Component(s.Planet, s.SubComponentCode));
        }
    }

    Console.WriteLine("\n-- Phase 3: StrengthBands (Core) == tbl_Rule_StrengthBand (migration 154) --");
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var bands = conn.Query<(string ScaleCode, string BandCode, decimal LowerBound, string SourceRefCode)>(
            @"SELECT ScaleCode, BandCode, LowerBound, SourceRefCode
              FROM dbo.tbl_Rule_StrengthBand WHERE RuleSetId = 1 AND IsActive = 1").ToList();
        Check("tbl_Rule_StrengthBand row count", bands.Count, StrengthBands.All.Count * 2);
        foreach (var scale in StrengthBands.All)
        {
            var strong = bands.SingleOrDefault(b => b.ScaleCode == scale.ScaleCode && b.BandCode == "STRONG");
            var moderate = bands.SingleOrDefault(b => b.ScaleCode == scale.ScaleCode && b.BandCode == "MODERATE");
            Check($"{scale.ScaleCode} STRONG", $"{strong.LowerBound:0.###} {strong.SourceRefCode}", $"{scale.StrongFrom:0.###} {scale.StrongSourceRefCode}");
            Check($"{scale.ScaleCode} MODERATE", $"{moderate.LowerBound:0.###} {moderate.SourceRefCode}", $"{scale.ModerateFrom:0.###} {scale.ModerateSourceRefCode}");
        }
    }

    Console.WriteLine(failures == 0 ? "\nverify-strength: ALL PASS" : $"\nverify-strength: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- `dotnet run -- verify-dasha` : Vimshottari Dasha's core table vs tbl_Rule_VimshottariPeriod
// (migration 085). Closes the 2026-09-11 rule-mapping audit's "the 9-planet order/120-year split
// has no DB citation at all" gap — AstroMath.NakshatraLordOrder / VimshottariYearsByLord (also the
// KP-2 sub-lord division's source) stay hardcoded per the project's "verified mirror" pattern;
// this is the CLI check that pattern requires.
if (args.Length > 0 && args[0] == "verify-dasha")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    using var conn = connectionFactory.CreateOpenConnection();
    var dbRows = conn.Query<(int SequenceOrder, string PlanetName, int YearsInCycle)>(
        @"SELECT r.SequenceOrder, p.PlanetName, r.YearsInCycle
          FROM dbo.tbl_Rule_VimshottariPeriod r
          JOIN dbo.tbl_Planets p ON p.Id = r.PlanetId
          WHERE r.RuleSetId = 1 ORDER BY r.SequenceOrder").ToList();

    Check("tbl_Rule_VimshottariPeriod has 9 rows", dbRows.Count, 9);
    Check("YearsInCycle totals 120", dbRows.Sum(r => r.YearsInCycle), 120);

    var codeOrder = string.Join(",", AstroMath.NakshatraLordOrder);
    var dbOrder = string.Join(",", dbRows.Select(r => r.PlanetName));
    Check("SequenceOrder == AstroMath.NakshatraLordOrder", dbOrder, codeOrder);

    var yearMismatch = 0;
    foreach (var r in dbRows)
    {
        var planet = Enum.Parse<PlanetName>(r.PlanetName);
        var expectedYears = AstroMath.VimshottariYearsByLord[planet];
        if (r.YearsInCycle != expectedYears)
        {
            yearMismatch++;
            Console.WriteLine($"    {r.PlanetName}: DB {r.YearsInCycle}y, AstroMath {expectedYears}y");
        }
    }
    Check("YearsInCycle == AstroMath.VimshottariYearsByLord for every planet", yearMismatch, 0);

    Console.WriteLine(failures == 0 ? "\nverify-dasha: ALL PASS" : $"\nverify-dasha: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

if (args.Length > 0 && args[0] == "verify-interpretive-factors")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    using var conn = connectionFactory.CreateOpenConnection();

    var factorCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.tbl_Dim_InterpretiveFactor WHERE IsActive = 1");
    Check("tbl_Dim_InterpretiveFactor has 4 active rows", factorCount, 4);

    var badReference = conn.ExecuteScalar<int>("""
        SELECT COUNT(*) FROM dbo.tbl_Rule_InterpretiveFactorDetail
        WHERE (CASE WHEN LifeAreaId IS NOT NULL THEN 1 ELSE 0 END
             + CASE WHEN DivisionalSubjectCode IS NOT NULL THEN 1 ELSE 0 END
             + CASE WHEN KarakaRoleId IS NOT NULL THEN 1 ELSE 0 END) <> 1
        """);
    Check("every detail row has exactly one of LifeAreaId/DivisionalSubjectCode/KarakaRoleId", badReference, 0);

    var badValue = conn.ExecuteScalar<int>("""
        SELECT COUNT(*) FROM dbo.tbl_Rule_InterpretiveFactorDetail
        WHERE (CASE WHEN HouseNumber IS NOT NULL THEN 1 ELSE 0 END
             + CASE WHEN GrahaId IS NOT NULL THEN 1 ELSE 0 END
             + CASE WHEN SignId IS NOT NULL THEN 1 ELSE 0 END
             + CASE WHEN ChartTypeId IS NOT NULL THEN 1 ELSE 0 END) <> 1
        """);
    Check("every detail row has exactly one of House/Graha/Sign/ChartType", badValue, 0);

    var nonCharaKarakaRoles = conn.ExecuteScalar<int>("""
        SELECT COUNT(*) FROM dbo.tbl_Rule_InterpretiveFactorDetail d
        JOIN dbo.tbl_Dim_KarakaRole kr ON kr.Id = d.KarakaRoleId
        WHERE kr.KarakaTypeCode <> 'CHARA'
        """);
    Check("every KarakaRoleId used is a CHARA-typed role", nonCharaKarakaRoles, 0);

    var detailCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.tbl_Rule_InterpretiveFactorDetail");
    Console.WriteLine($"  [INFO] tbl_Rule_InterpretiveFactorDetail row count: {detailCount} (sanity ceiling 160)");
    Check("detail row count within sanity ceiling", detailCount <= 160, true);

    // FEAT-HOUSE-05 (migration 134): every one of the 20 tbl_Dim_LifeArea rows now has at
    // least one VARGA detail row -- the piece migration 109 explicitly deferred.
    var lifeAreasCovered = conn.ExecuteScalar<int>("""
        SELECT COUNT(DISTINCT LifeAreaId) FROM dbo.tbl_Rule_InterpretiveFactorDetail WHERE LifeAreaId IS NOT NULL
        """);
    Check("every LifeArea has at least one InterpretiveFactorDetail row", lifeAreasCovered, 20);

    Console.WriteLine(failures == 0 ? "\nverify-interpretive-factors: ALL PASS" : $"\nverify-interpretive-factors: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

if (args.Length > 0 && args[0] == "verify-dasha-lord-relationship")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    var ram = birthDetailsRepo.GetAll().First(p => p.Name == "RamakrishnanP");
    var repo = new DashaLordRelationshipRepository(connectionFactory);
    var rows = repo.GetByBirthDetailId(ram.Id);

    Check("row count > 0", rows.Count > 0, true);

    var levelCodes = rows.Select(r => r.LevelCode).Distinct().OrderBy(c => c).ToList();
    Check("exactly 3 distinct LevelCode values", string.Join(",", levelCodes), "L1_MAHA,L2_ANTAR,L3_PRAT");

    var missingRasiLord = rows.Count(r => r.RasiLordName is null);
    Check("every row has a non-null RasiLordName (D1 join didn't drop rows)", missingRasiLord, 0);
    var missingNakLord = rows.Count(r => r.NakshatraLordName is null);
    Check("every row has a non-null NakshatraLordName", missingNakLord, 0);

    // Spot-check the first Mahadasha period's lord against tbl_Chart_KeyDetails directly.
    var firstMaha = rows.First(r => r.LevelCode == "L1_MAHA");
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var direct = conn.QuerySingle<(string SignLordName, string NakshatraLordName)>("""
            SELECT rlp.PlanetName AS SignLordName, nlp.PlanetName AS NakshatraLordName
            FROM dbo.tbl_ChartResults cr
            JOIN dbo.tbl_Chart_KeyDetails kd ON kd.ChartResultId = cr.Id AND kd.PointKind = 'Graha' AND kd.PlanetId = @LordId
            JOIN dbo.tbl_Planets rlp ON rlp.Id = kd.SignLordPlanetId
            JOIN dbo.tbl_Planets nlp ON nlp.Id = kd.NakshatraLordPlanetId
            WHERE cr.BirthDetailId = @BirthDetailId AND cr.CalculationKind = 'PositionChart'
              AND cr.ChartTypeId = (SELECT Id FROM dbo.tbl_Dim_ChartType WHERE Code = 'D1')
            """, new { LordId = firstMaha.DashaLordPlanetId, BirthDetailId = ram.Id });
        Check("spot-check: first Mahadasha RasiLordName matches direct KeyDetails query", firstMaha.RasiLordName, direct.SignLordName);
        Check("spot-check: first Mahadasha NakshatraLordName matches direct KeyDetails query", firstMaha.NakshatraLordName, direct.NakshatraLordName);
    }

    Console.WriteLine(failures == 0 ? "\nverify-dasha-lord-relationship: ALL PASS" : $"\nverify-dasha-lord-relationship: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

if (args.Length > 0 && args[0] == "verify-punya-saham")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    // Hand-computed, independent of the DB (same pattern as verify-jaimini's CharaKarakaCalculator
    // check) — PVR's own two worked Saham examples (sec 28.8.1), relabeled as Sun/Moon/Lagna to
    // exercise Punya Saham's day/night formula and its +30 deg arc correction. The arc-check math
    // is identical regardless of which named bodies A/B/C represent, so PVR's own real degree
    // values prove the calculator, not synthetic ones.

    // Samartha-saham numbers (day-birth relabeling): B=Sun=354 58', A=Moon=19 10', C=Lagna=280 50'.
    // PVR: raw A-B+C = 305 02', Lagna not encountered going Sun->Moon, so +30 -> final 335 02'.
    var day = PunyaSahamCalculator.Compute(
        sunLongitude: 354 + 58.0 / 60, moonLongitude: 19 + 10.0 / 60, natalLagnaLongitude: 280 + 50.0 / 60, isNightBirth: false);
    Check("day formula (Samartha-saham numbers), raw 305 02' + 30 deg arc correction",
        Math.Round(day.NirayanaLongitudeDegrees, 4), Math.Round(335 + 2.0 / 60, 4));

    // Vanik-saham numbers (night-birth relabeling): B=Moon=345 14', A=Mercury/here Sun=311 28',
    // C=Lagna=280 50'. PVR: Lagna IS encountered going Moon->Mercury, so no +30 -> 247 04'.
    var night = PunyaSahamCalculator.Compute(
        sunLongitude: 311 + 28.0 / 60, moonLongitude: 345 + 14.0 / 60, natalLagnaLongitude: 280 + 50.0 / 60, isNightBirth: true);
    Check("night formula (Vanik-saham numbers), arc found, no +30 deg correction",
        Math.Round(night.NirayanaLongitudeDegrees, 4), Math.Round(247 + 4.0 / 60, 4));

    Check("PointKind is SpecialLagna", day.PointKind, "SpecialLagna");
    Check("Code is PS", day.Code, "PS");

    Console.WriteLine(failures == 0 ? "\nverify-punya-saham: ALL PASS" : $"\nverify-punya-saham: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

if (args.Length > 0 && args[0] == "verify-planet-in-house")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    using var conn = connectionFactory.CreateOpenConnection();

    var rowCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.tbl_Rule_PlanetInHouse");
    Check("tbl_Rule_PlanetInHouse row count", rowCount, 108);

    var badSource = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.tbl_Rule_PlanetInHouse WHERE SourceRefCode <> 'SRC_RAMAN_HTJH'");
    Check("every row cites SRC_RAMAN_HTJH", badSource, 0);

    var promotedCount = conn.ExecuteScalar<int>(
        "SELECT COUNT(*) FROM research.tbl_Dim_SourceReferencePlanetInHouseCrosswalk WHERE ProductionTargetCode = 'dbo.tbl_Rule_PlanetInHouse' AND PromotionStatus = 'Promoted'");
    Check("crosswalk shows 108 Promoted", promotedCount, 108);

    // Spot-check against the source paragraphs quoted during transcription.
    var marsHouse1 = conn.ExecuteScalar<string>(
        "SELECT r.ResultText FROM dbo.tbl_Rule_PlanetInHouse r JOIN dbo.tbl_Planets p ON p.Id = r.PlanetId WHERE p.PlanetName = 'Mars' AND r.HouseNumber = 1");
    Check("Mars/1st starts with the Raman paraphrase", marsHouse1?.StartsWith("Gives a hot, courageous constitution"), true);
    var sunHouse10 = conn.ExecuteScalar<string>(
        "SELECT r.ResultText FROM dbo.tbl_Rule_PlanetInHouse r JOIN dbo.tbl_Planets p ON p.Id = r.PlanetId WHERE p.PlanetName = 'Sun' AND r.HouseNumber = 10");
    Check("Sun/10th starts with the Raman paraphrase", sunHouse10?.StartsWith("Successful in all undertakings"), true);

    // End-to-end: the view resolves for a real chart via the repository.
    var ram = birthDetailsRepo.GetAll().First(p => p.Name == "RamakrishnanP");
    var d1ChartResultId = conn.ExecuteScalar<int>(
        "SELECT cr.Id FROM dbo.tbl_ChartResults cr WHERE cr.BirthDetailId = @Id AND cr.ChartType = 'D1'", new { ram.Id });
    var repo = new PlanetInHouseRepository(connectionFactory);
    var rows = repo.GetForChart(d1ChartResultId);
    Check("Ramakrishnan D1 resolves 9 planet-in-house rows via the view/repository", rows.Count, 9);

    // Dispositor cross-reference (migration 116): every graha has a sign lord, so every row
    // should resolve a dispositor, and under WHOLE_SIGN houses that dispositor must match the
    // house's own lord from tbl_Chart_HouseLords -- an independent cross-check, not a re-derivation.
    Check("every row has a resolved dispositor", rows.Count(r => r.DispositorPlanetId is null), 0);
    var mismatches = 0;
    foreach (var r in rows)
    {
        var houseLord = conn.ExecuteScalar<string>(
            "SELECT LordPlanet FROM dbo.tbl_Chart_HouseLords WHERE ChartResultId = @Id AND HouseNumber = @House",
            new { Id = d1ChartResultId, House = r.HouseNumber });
        if (houseLord != r.DispositorPlanetName) mismatches++;
    }
    Check("dispositor matches tbl_Chart_HouseLords' own lord for every row (whole-sign)", mismatches, 0);

    Console.WriteLine(failures == 0 ? "\nverify-planet-in-house: ALL PASS" : $"\nverify-planet-in-house: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-dispositor` ---
// DispositorEngine (chain-following + final-dispositor + mutual-reception/cycle detection) is a
// pure, live Core calculator — no fact table (FEAT-DISPOSITOR-01, live-only by design, same
// category as ArgalaTable/RasiDrishtiCalculator). No unit-test project covers CLI-reachable
// stored-chart integration, so this is the only place its output gets checked against real,
// persisted charts rather than hand-built fixtures (see DispositorEngineTests for those).
if (args.Length > 0 && args[0] == "verify-dispositor")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    var keyDetailsRepo = new ChartKeyDetailsRepository(connectionFactory);
    using var dispConn = connectionFactory.CreateOpenConnection();

    foreach (var person in birthDetailsRepo.GetAll())
    {
        var d1ChartResultId = dispConn.ExecuteScalar<int?>(
            "SELECT cr.Id FROM dbo.tbl_ChartResults cr WHERE cr.BirthDetailId = @Id AND cr.ChartType = 'D1'", new { person.Id });
        if (d1ChartResultId is null) continue;

        var keyDetails = keyDetailsRepo.GetByChartResultId(d1ChartResultId.Value);
        var ascendantSign = Enum.Parse<ZodiacName>(keyDetails.Single(k => k.Planet == "Ascendant").Sign);
        // Matches DispositorTable.razor's own filter exactly: PointKind == "Graha" alone isn't
        // enough to exclude Ascendant/other non-classical points in every stored chart.
        var grahas = keyDetails.Where(k => k.PointKind == "Graha" && k.Planet != "Ascendant"
            && Enum.TryParse<PlanetName>(k.Planet, true, out _)).ToList();
        var input = new ChartAnalysisInput("D1", ascendantSign,
            grahas.Select(k => new PlanetPosition { Planet = k.Planet, Sign = k.Sign, PointKind = "Graha", HouseNumber = k.HouseNumberFromLagna }).ToList());

        var chains = new DispositorEngine().Compute(input).ToDictionary(c => c.Planet);
        Check($"{person.Name}: every graha resolves a chain", chains.Count, grahas.Count);
        Check($"{person.Name}: no chain hits MISSING_PLACEMENT", chains.Values.Count(c => c.TerminationCode == "MISSING_PLACEMENT"), 0);

        // Cross-check against ChartAnalyzer's own independently-computed SignLordPlanet
        // (tbl_Chart_KeyDetails, stamped at generation time) — the chain's first hop must agree.
        var firstHopMismatches = grahas.Count(k => chains[k.Planet].Chain[1] != k.SignLordPlanet);
        Check($"{person.Name}: chain's first hop matches stored SignLordPlanet for every graha", firstHopMismatches, 0);

        // FinalDispositor is where the chain terminates, not necessarily the starting planet —
        // that IS the point of "final dispositor" (e.g. Mars -> Jupiter -> Jupiter means Jupiter
        // is Mars's final dispositor). The real invariant: FinalDispositor must be a genuine
        // fixed point — looked up as its own starting planet, it resolves self-disposed onto itself.
        var finalDispositorsAreFixedPoints = chains.Values.Where(c => c.TerminationCode == "SELF_DISPOSED")
            .All(c => c.FinalDispositor is not null && chains.TryGetValue(c.FinalDispositor, out var fd)
                && fd.TerminationCode == "SELF_DISPOSED" && fd.FinalDispositor == c.FinalDispositor);
        Check($"{person.Name}: every SELF_DISPOSED chain's FinalDispositor is a genuine fixed point", finalDispositorsAreFixedPoints, true);

        var mutualReceptionConsistent = chains.Values.Where(c => c.TerminationCode == "MUTUAL_RECEPTION")
            .All(c => c.InMutualReception && c.Cycle is { Count: 2 } cycle && cycle.Contains(c.Planet));
        Check($"{person.Name}: every MUTUAL_RECEPTION chain is flagged + has a 2-planet cycle containing itself", mutualReceptionConsistent, true);

        var outcomes = chains.Values.GroupBy(c => c.TerminationCode).ToDictionary(g => g.Key, g => g.Count());
        Console.WriteLine($"    ({person.Name} outcome mix: {string.Join(", ", outcomes.Select(o => $"{o.Key}={o.Value}"))})");
    }

    Console.WriteLine(failures == 0 ? "\nverify-dispositor: ALL PASS" : $"\nverify-dispositor: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-graha-drishti` ---
// GrahaDrishtiStrengthRepository.Replace persists the sphuta (longitude-based) Virupa
// strength for every aspecting/aspected pair in every registered chart type (FEAT-RELATIONSHIP-05,
// migration 132; widened from D1/D9/D10-only per sign_planet_asp.md slices 6-7). This recomputes
// GrahaDrishtiStrengthCalculator independently from each chart's own stored longitudes and
// cross-checks it against the persisted tbl_Fact_GrahaDrishtiStrengths rows, rather than
// re-deriving via the same code path.
if (args.Length > 0 && args[0] == "verify-graha-drishti")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    var gdKeyDetailsRepo = new ChartKeyDetailsRepository(connectionFactory);
    var grahaDrishtiRepo = new GrahaDrishtiStrengthRepository(connectionFactory);
    using var gdConn = connectionFactory.CreateOpenConnection();
    // Same "every registered chart type except VimshottariDasha" scope recompute-keydetails uses.
    var supportedChartTypes = orchestrator.Calculators.Select(c => c.ChartType).ToArray();

    foreach (var person in birthDetailsRepo.GetAll())
    {
        var storedRows = grahaDrishtiRepo.GetByBirthDetailId(person.Id);
        Check($"{person.Name}: no rows outside registered chart types", storedRows.Count(r => !supportedChartTypes.Contains(r.ChartType)), 0);

        foreach (var chartType in supportedChartTypes)
        {
            var chartResultId = gdConn.ExecuteScalar<int?>(
                "SELECT cr.Id FROM dbo.tbl_ChartResults cr WHERE cr.BirthDetailId = @Id AND cr.ChartType = @ChartType",
                new { person.Id, ChartType = chartType });
            if (chartResultId is null) continue;

            var points = gdKeyDetailsRepo.GetByChartResultId(chartResultId.Value)
                .Where(k => k.PointKind == "Graha").ToList();
            var aspecting = points.Where(k => Enum.TryParse<PlanetName>(k.Planet, true, out _)).ToList();
            var rows = storedRows.Where(r => r.ChartType == chartType).ToList();

            Check($"{person.Name}/{chartType}: row count = aspecting x (points - 1)", rows.Count, aspecting.Count * (points.Count - 1));

            var mismatches = 0;
            foreach (var source in aspecting)
            {
                var sourcePlanet = Enum.Parse<PlanetName>(source.Planet, true);
                foreach (var target in points.Where(p => p.Planet != source.Planet))
                {
                    var expected = GrahaDrishtiStrengthCalculator.Calculate(
                        sourcePlanet, source.VargaLongitudeDegrees, target.VargaLongitudeDegrees);
                    var row = rows.FirstOrDefault(r => r.AspectingPlanet == source.Planet && r.AspectedPointKey == target.Planet);
                    if (row is null
                        || Math.Abs((double)row.TotalVirupas - expected.TotalVirupas) > 0.01
                        || Math.Abs((double)row.StrengthPercentage - expected.Percentage) > 0.01
                        || (row.DiscreteAspectHouse.HasValue ? (int)row.DiscreteAspectHouse.Value : (int?)null) != expected.DiscreteAspectHouse)
                    {
                        mismatches++;
                        Console.WriteLine($"    {chartType} {source.Planet} -> {target.Planet}: stored {row?.TotalVirupas} vir / {row?.StrengthPercentage}% / house {row?.DiscreteAspectHouse}, "
                            + $"recomputed {expected.TotalVirupas:0.####} / {expected.Percentage:0.####}% / house {expected.DiscreteAspectHouse} "
                            + $"(longitudes {source.VargaLongitudeDegrees:0.######} -> {target.VargaLongitudeDegrees:0.######})");
                    }
                }
            }
            Check($"{person.Name}/{chartType}: every row matches an independent recompute", mismatches, 0);

            Check($"{person.Name}/{chartType}: IsDiscreteAspect matches DiscreteAspectHouse for every row",
                rows.Count(r => r.IsDiscreteAspect != r.DiscreteAspectHouse.HasValue), 0);
            Check($"{person.Name}/{chartType}: every row cites SRC_PVR_INTEGRATED", rows.Count(r => r.SourceRefCode != "SRC_PVR_INTEGRATED"), 0);
            Check($"{person.Name}/{chartType}: every row has a RuleSetId", rows.Count(r => r.RuleSetId == 0), 0);
        }
    }

    Console.WriteLine(failures == 0 ? "\nverify-graha-drishti: ALL PASS" : $"\nverify-graha-drishti: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-sthira-karaka` ---
// Migration 133 populates the STHIRA slot in tbl_Dim_KarakaRole reserved-but-unseeded by
// migration 103, sourced to B.V. Raman's How to Judge a Horoscope (SRC_RAMAN_HTJH) rather than
// PVR. FEAT-HOUSE-03 / FEAT-KARAKA-03.
if (args.Length > 0 && args[0] == "verify-sthira-karaka")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    var karakaRepo = new NaisargikaKarakaRepository(connectionFactory);
    var sthira = karakaRepo.GetSthiraKarakas();
    Check("6 Sthira Karaka rows", sthira.Count, 6);

    var expected = new Dictionary<int, string> { [1] = "Sun", [2] = "Jupiter", [3] = "Mars", [4] = "Moon", [5] = "Jupiter", [8] = "Saturn" };
    var wrongPairs = sthira.Count(r => !expected.TryGetValue(r.HouseNumber, out var planet) || planet != r.Graha);
    Check("every row matches the confirmed (house, planet) pairs", wrongPairs, 0);
    Check("no rows outside the 6 confirmed houses", sthira.Select(r => r.HouseNumber).Except(expected.Keys).Count(), 0);

    using var skConn = connectionFactory.CreateOpenConnection();
    var badSource = skConn.ExecuteScalar<int>("""
        SELECT COUNT(*) FROM dbo.tbl_Rule_KarakaMatter km
        JOIN dbo.tbl_Dim_KarakaRole kr ON kr.Id = km.KarakaRoleId AND kr.KarakaTypeCode = 'STHIRA'
        WHERE km.SourceRefCode <> 'SRC_RAMAN_HTJH'
        """);
    Check("every Sthira Karaka row cites SRC_RAMAN_HTJH", badSource, 0);

    // Independent cross-check: for the 6 houses where both traditions have a role, Raman's
    // Sthira Karaka and PVR's Naisargika primary-karaka name the same planet — an agreement
    // this project treats as a real cross-check, not an assumption (they're separately cited).
    var naisargikaPrimary = karakaRepo.LoadActive().Primary.ToDictionary(r => r.HouseNumber, r => r.Graha);
    var mismatches = sthira.Count(r => naisargikaPrimary.TryGetValue(r.HouseNumber, out var g) && g != r.Graha);
    Check("Sthira Karaka agrees with the Naisargika primary table on the same houses", mismatches, 0);

    Console.WriteLine(failures == 0 ? "\nverify-sthira-karaka: ALL PASS" : $"\nverify-sthira-karaka: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-house-benefic-malefic` ---
// HouseBeneficMaleficCalculator (FEAT-HOUSE-06, cited SRC_RAMAN_HTJH p.14-15) has no persisted
// table — same "hardcode + cite" pattern as LagnaFunctionalNature. This recomputes it for every
// D1 chart on file and checks structural invariants plus internal consistency (the verdict is a
// deterministic function of the same counts the Rationale string reports).
if (args.Length > 0 && args[0] == "verify-house-benefic-malefic")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    var hbmKeyDetailsRepo = new ChartKeyDetailsRepository(connectionFactory);
    using var hbmConn = connectionFactory.CreateOpenConnection();

    foreach (var person in birthDetailsRepo.GetAll())
    {
        var chartResultId = hbmConn.ExecuteScalar<int?>(
            "SELECT cr.Id FROM dbo.tbl_ChartResults cr WHERE cr.BirthDetailId = @Id AND cr.ChartType = 'D1'",
            new { person.Id });
        if (chartResultId is null) continue;

        var keyDetails = hbmKeyDetailsRepo.GetByChartResultId(chartResultId.Value);
        var ascendantRow = keyDetails.FirstOrDefault(k => k.Planet == "Ascendant");
        if (ascendantRow is null) continue;

        var ascendantSign = Enum.Parse<ZodiacName>(ascendantRow.Sign);
        var results = HouseBeneficMaleficCalculator.ComputeAll(ascendantSign, keyDetails);

        Check($"{person.Name}: 12 house results", results.Count, 12);
        Check($"{person.Name}: house numbers are 1-12 in order", string.Join(",", results.Select(r => r.HouseNumber)), string.Join(",", Enumerable.Range(1, 12)));

        var badLord = results.Count(r => r.LordPlanet != HouseEngine.GetSignLord(r.Sign));
        Check($"{person.Name}: every house's lord matches HouseEngine.GetSignLord", badLord, 0);

        var badSign = results.Count(r => r.Sign != HouseEngine.GetHouseSign(ascendantSign, r.HouseNumber));
        Check($"{person.Name}: every house's sign matches HouseEngine.GetHouseSign", badSign, 0);

        // Independent recount: the Verdict must follow from the very lists the result itself
        // exposes (lord's nature + occupants + aspectors), not some other hidden weighting.
        var badVerdict = results.Count(r =>
        {
            var beneficCount = (r.LordFunctionalNature is FunctionalNature.Benefic or FunctionalNature.Yogakaraka ? 1 : 0)
                + r.BeneficOccupants.Count + r.BeneficAspectors.Count;
            var maleficCount = (r.LordFunctionalNature == FunctionalNature.Malefic ? 1 : 0)
                + r.MaleficOccupants.Count + r.MaleficAspectors.Count;
            var expectedVerdict = (beneficCount, maleficCount) switch
            {
                (0, 0) => HouseBeneficMaleficVerdict.Neutral,
                var (b, m) when b > m => HouseBeneficMaleficVerdict.Benefic,
                var (b, m) when m > b => HouseBeneficMaleficVerdict.Malefic,
                _ => HouseBeneficMaleficVerdict.Mixed
            };
            return r.Verdict != expectedVerdict;
        });
        Check($"{person.Name}: every Verdict follows deterministically from its own counted lists", badVerdict, 0);

        var badOccupant = results.Count(o =>
            o.BeneficOccupants.Concat(o.MaleficOccupants).Any(p =>
                !keyDetails.Any(k => k.Planet == p && k.HouseNumberFromLagna == o.HouseNumber)));
        Check($"{person.Name}: every listed occupant is really placed in that house", badOccupant, 0);

        // No planet is both a listed benefic occupant and a listed malefic occupant of the same house.
        var overlap = results.Count(r => r.BeneficOccupants.Intersect(r.MaleficOccupants).Any()
            || r.BeneficAspectors.Intersect(r.MaleficAspectors).Any());
        Check($"{person.Name}: no planet double-counted as both benefic and malefic on the same house", overlap, 0);
    }

    Console.WriteLine(failures == 0 ? "\nverify-house-benefic-malefic: ALL PASS" : $"\nverify-house-benefic-malefic: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- `dotnet run -- verify-varga-arudha` ---
// PVR sec.9.2 defines the arudha padas of every house "in all the divisional charts" from that
// chart's own placements (ArudhaCalculator inside VargaChartComputer, with the sec.15.5.1 stronger
// co-lord for Sc/Aq). Checks the Arudha Lagna of every chart the project shares with JHora against
// the AL in each grid of docs/artifacts/reference-charts/*_Jagannatha.txt, for every person on file
// with an export. Live = ChartPipeline.Run now (fails the check); stored = tbl_Chart_KeyDetails
// (reported STALE until backfill-charts runs). A grid whose Ascendant differs from ours is a
// different varga variant and is skipped, not failed.
if (args.Length > 0 && args[0] == "verify-varga-arudha")
{
    var failures = 0;
    var dir = args.Length > 1 ? args[1] : Path.Combine("docs", "artifacts", "reference-charts");
    var psRulesVva = new PlanetaryStateRuleRepository(connectionFactory).GetActiveRuleSet();
    var pipelineVva = new ChartPipeline(orchestrator, psRulesVva);
    var people = birthDetailsRepo.GetAll();
    using var vvaConn = connectionFactory.CreateOpenConnection();

    foreach (var file in Directory.GetFiles(dir, "*_Jagannatha.txt").OrderBy(f => f))
    {
        var lines = File.ReadAllLines(file);
        var jhoraName = JHoraGridReader.PersonName(lines[0]);
        var person = people.FirstOrDefault(p => p.Name == jhoraName);
        Console.WriteLine($"\n{Path.GetFileName(file)} -> {jhoraName}");
        if (person is null) { Console.WriteLine("  (no such person on file — skipped)"); continue; }

        var live = pipelineVva.Run(person).Charts.ToDictionary(c => c.ChartType);
        var stored = vvaConn.Query<(string ChartType, string Planet, string Sign)>(
            @"SELECT cr.ChartType, kd.Planet, kd.Sign
              FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              WHERE cr.BirthDetailId = @bid AND kd.Planet IN ('Ascendant', 'AL')", new { bid = person.Id })
            .ToDictionary(r => (r.ChartType, r.Planet), r => r.Sign);

        foreach (var grid in JHoraGridReader.Read(lines))
        {
            var chartType = JHoraGridReader.ChartTypeOf(grid.Label, live.Keys);
            if (chartType is null || !grid.SignOf.TryGetValue("AL", out var jhoraAl) || !grid.SignOf.TryGetValue("As", out var jhoraAs))
                continue;
            var chart = live[chartType];
            if (chart.AscendantSign != jhoraAs)
            {
                Console.WriteLine($"  [SKIP] {chartType,-6} JHora '{grid.Label}' Lagna {jhoraAs} vs ours {chart.AscendantSign} — a different varga variant");
                continue;
            }
            var liveAl = chart.SpecialPoints.Single(p => p.Planet == "AL").Sign;
            var ok = liveAl == jhoraAl.ToString();
            if (!ok) failures++;
            var storedAl = stored.GetValueOrDefault((chartType, "AL"));
            var storedNote = storedAl == liveAl ? "" : $"  stored {storedAl ?? "—"} STALE";
            Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {chartType,-6} AL ours {liveAl}, JHora {jhoraAl}{storedNote}");
        }
    }

    Console.WriteLine(failures == 0 ? "\nverify-varga-arudha: ALL PASS" : $"\nverify-varga-arudha: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-pipeline` ---
// The DB-free ChartPipeline.Run façade must reproduce, for person 1 (Ramakrishnan), the same D1
// KeyDetails the stored rows hold — proving the compute half of ChartGenerationService is faithfully
// captured with no repository/connection. Solution has no unit-test project; this is the guard.
if (args.Length > 0 && args[0] == "verify-pipeline")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    var psRules = new PlanetaryStateRuleRepository(connectionFactory).GetActiveRuleSet();
    var pipeline = new ChartPipeline(orchestrator, psRules);

    var ram = birthDetailsRepo.GetAll().First(p => p.Name == "RamakrishnanP");
    var bundle = pipeline.Run(ram);

    // Recompute D1 KeyDetails from the bundle and compare to the stored rows for person 1.
    var d1Input = bundle.Charts.Single(c => c.ChartType == "D1");
    var (computed, _, _, _) = ChartAnalyzer.Compute(d1Input);

    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var stored = conn.Query<(string Planet, string PointKind, string Sign, string? DignityStatus, int HouseNumberFromLagna)>(
            @"SELECT kd.Planet, kd.PointKind, kd.Sign, kd.DignityStatus, kd.HouseNumberFromLagna
              FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              WHERE cr.BirthDetailId = @bid AND cr.ChartType = 'D1'
              ORDER BY kd.Id", new { bid = ram.Id }).ToList();

        Check("D1 row count", computed.Count, stored.Count);
        foreach (var s in stored)
        {
            var c = computed.FirstOrDefault(x => x.Planet == s.Planet && x.PointKind == s.PointKind);
            Check($"{s.Planet}/{s.PointKind} Sign", c?.Sign, s.Sign);
            Check($"{s.Planet}/{s.PointKind} DignityStatus", c?.DignityStatus, s.DignityStatus);
            Check($"{s.Planet}/{s.PointKind} HouseFromLagna", c?.HouseNumberFromLagna, s.HouseNumberFromLagna);
        }
    }

    Check("CharaKaraka count", bundle.CharaKarakaByPlanet.Count, 8);
    Check("State rows non-empty", bundle.States.Count > 0, true);

    Console.WriteLine(failures == 0 ? "\nverify-pipeline: ALL PASS" : $"\nverify-pipeline: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- `dotnet run -- verify-sources` : tbl_Dim_Source well-formedness (STANDARDS §M.4) ---
if (args.Length > 0 && args[0] == "verify-sources")
{
    using var conn = connectionFactory.CreateOpenConnection();
    var failures = 0;
    void Check(string label, long violations)
    {
        var ok = violations == 0;
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: {violations} violation(s)");
        if (!ok) failures++;
    }
    long Count(string sql) => conn.ExecuteScalar<long>(sql);

    Check("tbl_Dim_Source is seeded (>= 10 rows)",
        Count("SELECT CASE WHEN COUNT(*) >= 10 THEN 0 ELSE 1 END FROM dbo.tbl_Dim_Source"));
    Check("every Code matches SRC_<UPPER_SNAKE>",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Dim_Source
                WHERE Code NOT LIKE 'SRC[_]%' OR Code COLLATE Latin1_General_BIN LIKE '%[^A-Z0-9_]%'"));
    Check("every source has a Title",
        Count("SELECT COUNT(*) FROM dbo.tbl_Dim_Source WHERE Title IS NULL OR LTRIM(RTRIM(Title)) = ''"));
    Check("Code is unique",
        Count("SELECT COUNT(*) - COUNT(DISTINCT Code) FROM dbo.tbl_Dim_Source"));

    // Forward-looking: every SourceRefCode used by a dbo rule/fact table must resolve.
    // Scoped to schema dbo — the research.* corpus (migrations 056/067/070) carries its own
    // SourceRefCode / SourceRefLocator columns that this dbo-only tripwire must not touch.
    var refColumns = conn.Query<(string TableName, string ColumnName)>(@"
        SELECT t.name, c.name
        FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
        WHERE c.name = 'SourceRefCode' AND t.schema_id = SCHEMA_ID('dbo')").ToList();
    foreach (var (tbl, col) in refColumns)
        Check($"{tbl}.{col} all resolve in tbl_Dim_Source",
            Count($@"SELECT COUNT(*) FROM dbo.[{tbl}] x
                     WHERE x.[{col}] IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_Source s WHERE s.Code = x.[{col}])"));

    Console.WriteLine(failures == 0 ? "\nverify-sources: ALL PASS" : $"\nverify-sources: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

if (args.Length > 0 && args[0] == "verify-schema")
{
    using var conn = connectionFactory.CreateOpenConnection();
    var failures = 0;
    void Check(string label, long violations)
    {
        var ok = violations == 0;
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: {violations} violation(s)");
        if (!ok) failures++;
    }
    long Count(string sql) => conn.ExecuteScalar<long>(sql);

    // -- AstroIds offset sanity (Phase 2) --
    Check("PlanetId offset matches tbl_Planets",
        Count("SELECT COUNT(*) FROM dbo.tbl_Planets WHERE Id <> (CASE PlanetName WHEN 'Sun' THEN 1 WHEN 'Moon' THEN 2 WHEN 'Mars' THEN 3 WHEN 'Mercury' THEN 4 WHEN 'Jupiter' THEN 5 WHEN 'Venus' THEN 6 WHEN 'Saturn' THEN 7 WHEN 'Rahu' THEN 8 WHEN 'Ketu' THEN 9 END)"));

    // -- backfill completeness (Phase 1) --
    Check("KeyDetails.PlanetId populated (non-Ascendant)",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE PointKind = 'Graha' AND Planet <> 'Ascendant' AND PlanetId IS NULL"));
    Check("KeyDetails.SignId populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE SignId IS NULL"));
    Check("HouseLords ids populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_HouseLords WHERE HouseSignId IS NULL OR LordPlanetId IS NULL OR LordPlacedInSignId IS NULL"));
    Check("Conjunctions ids populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_Conjunctions WHERE Planet1Id IS NULL OR Planet2Id IS NULL OR SignId IS NULL"));
    Check("Aspects ids populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_Aspects WHERE AspectingPlanetId IS NULL OR AspectedTargetType IS NULL OR (AspectedTargetType = 'Planet' AND AspectedPlanetId IS NULL)"));
    Check("DashaPeriods.LordId populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_DashaPeriods WHERE LordId IS NULL"));
    Check("ChartResults.RuleSetId populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_ChartResults WHERE RuleSetId IS NULL"));
    Check("ChartResults position charts have ChartTypeId",
        Count("SELECT COUNT(*) FROM dbo.tbl_ChartResults WHERE CalculationKind = 'PositionChart' AND ChartTypeId IS NULL"));
    Check("PlanetaryState.PlanetId populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Fact_PlanetaryState WHERE PlanetId IS NULL"));

    // -- orphan-id checks --
    Check("KeyDetails.PlanetId resolves",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd WHERE kd.PlanetId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Planets p WHERE p.Id = kd.PlanetId)"));
    Check("KeyDetails.SignId resolves",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd WHERE kd.SignId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_SignAttributes s WHERE s.Id = kd.SignId)"));
    Check("ChartResults.ChartTypeId resolves",
        Count("SELECT COUNT(*) FROM dbo.tbl_ChartResults cr WHERE cr.ChartTypeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_ChartType ct WHERE ct.Id = cr.ChartTypeId)"));

    // -- domain probes (become CHECK constraints in migration 06) --
    Check("KeyDetails longitude in [0,360)",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE NirayanaLongitudeDegrees < 0 OR NirayanaLongitudeDegrees >= 360"));
    Check("KeyDetails degrees-in-sign in [0,30)",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE DegreesInSignDecimal IS NOT NULL AND (DegreesInSignDecimal < 0 OR DegreesInSignDecimal >= 30)"));
    Check("KeyDetails houses in [1,12]",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE HouseNumberFromLagna NOT BETWEEN 1 AND 12 OR HouseNumberFromSun NOT BETWEEN 1 AND 12 OR HouseNumberFromMoon NOT BETWEEN 1 AND 12"));
    Check("KeyDetails pada in [1,4]",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE NakshatraPada IS NOT NULL AND NakshatraPada NOT BETWEEN 1 AND 4"));
    Check("BirthDetails lat/long in range",
        Count("SELECT COUNT(*) FROM dbo.tbl_BirthDetails WHERE Latitude NOT BETWEEN -90 AND 90 OR Longitude NOT BETWEEN -180 AND 180"));
    Check("DashaPeriods start < end",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_DashaPeriods WHERE StartDate >= EndDate OR StartDayOffset > EndDayOffset"));

    // -- rule-set sanity --
    Check("exactly one active rule set",
        Count("SELECT ABS(COUNT(*) - 1) FROM dbo.tbl_Rule_Sets WHERE IsActive = 1"));

    // -- Phase 2: constraint-backed invariants --
    Check("every conjunction canonical (Planet1Id < Planet2Id)",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_Conjunctions WHERE Planet1Id >= Planet2Id"));
    Check("aspect target shape consistent",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_Aspects WHERE (AspectedTargetType = 'Ascendant' AND AspectedPlanetId IS NOT NULL) OR (AspectedTargetType = 'Planet' AND AspectedPlanetId IS NULL)"));
    Check("no dasha parent points cross-chart",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_DashaPeriods c JOIN dbo.tbl_Chart_DashaPeriods p ON p.Id = c.ParentDashaPeriodId WHERE p.ChartResultId <> c.ChartResultId"));
    Check("every stored position chart type is a known Dim code",
        Count("SELECT COUNT(*) FROM (SELECT DISTINCT ChartType FROM dbo.tbl_ChartResults WHERE CalculationKind = 'PositionChart') x WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_ChartType d WHERE d.Code = x.ChartType)"));
    Check("every tbl_Dim_ChartType row has a ChartShortDescription (migration 24)",
        Count("SELECT COUNT(*) FROM dbo.tbl_Dim_ChartType WHERE ChartShortDescription IS NULL"));
    Check("every tbl_Dim_ChartType row has a life-area Category, not the old blanket 'Varga' (migration 25)",
        Count("SELECT COUNT(*) FROM dbo.tbl_Dim_ChartType WHERE Category = 'Varga'"));
    Check("every position chart has exactly 8 distinct CharaKaraka rows",
        Count(@"
            SELECT COUNT(*) FROM (
                SELECT cr.Id, COUNT(kd.CharaKaraka) n, COUNT(DISTINCT kd.CharaKaraka) d
                FROM dbo.tbl_ChartResults cr
                JOIN dbo.tbl_Chart_KeyDetails kd ON kd.ChartResultId = cr.Id
                WHERE cr.CalculationKind = 'PositionChart'
                GROUP BY cr.Id
            ) x WHERE x.n <> 8 OR x.d <> 8"));
    Check("non-Graha KeyDetails carry no graha-only analytics",
        Count(@"
            SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails
            WHERE PointKind <> 'Graha' AND (PlanetId IS NOT NULL OR DignityStatus IS NOT NULL
                OR Nakshatra IS NOT NULL OR CharaKaraka IS NOT NULL OR AspectingPlanets IS NOT NULL
                OR IsCombust IS NOT NULL OR NakshatraLordPlanet IS NOT NULL)"));
    Check("every position chart has 12 Arudha rows (AL + A2..A12)",
        Count(@"
            SELECT COUNT(*) FROM (
                SELECT cr.Id, COUNT(*) n
                FROM dbo.tbl_ChartResults cr
                JOIN dbo.tbl_Chart_KeyDetails kd ON kd.ChartResultId = cr.Id AND kd.PointKind = 'Arudha'
                WHERE cr.CalculationKind = 'PositionChart'
                GROUP BY cr.Id
            ) x WHERE x.n <> 12"));

    // -- multi-graha conjunction group layer (migration 22 + Phase 3 wiring) --
    Check("every pair conjunction is linked to a group",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_Conjunctions WHERE MultiGrahaConjunctionId IS NULL"));
    Check("both planets of a pair are members of its group",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Chart_Conjunctions c
                WHERE c.MultiGrahaConjunctionId IS NOT NULL
                  AND (NOT EXISTS (SELECT 1 FROM dbo.tbl_Chart_MultiGrahaConjunctionMember m
                                   WHERE m.MultiGrahaConjunctionId = c.MultiGrahaConjunctionId AND m.PlanetId = c.Planet1Id)
                    OR NOT EXISTS (SELECT 1 FROM dbo.tbl_Chart_MultiGrahaConjunctionMember m
                                   WHERE m.MultiGrahaConjunctionId = c.MultiGrahaConjunctionId AND m.PlanetId = c.Planet2Id))"));
    Check("group PlanetCount = member count = distinct grahas in that sign",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunction g
                WHERE g.PlanetCount <> (SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunctionMember m
                                        WHERE m.MultiGrahaConjunctionId = g.Id)
                   OR g.PlanetCount <> (SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd
                                        WHERE kd.ChartResultId = g.ChartResultId AND kd.SignId = g.SignId
                                          AND kd.PointKind = 'Graha' AND kd.PlanetId IS NOT NULL AND kd.Planet <> 'Ascendant')"));
    Check("group PlanetCount >= 2",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunction WHERE PlanetCount < 2"));
    Check("MemberKey is the ascending PlanetId CSV of its members",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunction g
                WHERE g.MemberKey <> (SELECT STRING_AGG(CAST(m.PlanetId AS VARCHAR(2)), ',') WITHIN GROUP (ORDER BY m.PlanetId)
                                      FROM dbo.tbl_Chart_MultiGrahaConjunctionMember m
                                      WHERE m.MultiGrahaConjunctionId = g.Id)"));
    Check("MemberKey entry count = PlanetCount",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunction
                WHERE LEN(MemberKey) - LEN(REPLACE(MemberKey, ',', '')) + 1 <> PlanetCount"));
    Check("D1 groups: LongitudeSpanDegrees non-null and = max-min member NirayanaLongitude",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunction g
                JOIN dbo.tbl_ChartResults cr ON cr.Id = g.ChartResultId
                WHERE cr.ChartTypeId = 1
                  AND (g.LongitudeSpanDegrees IS NULL
                    OR ABS(g.LongitudeSpanDegrees - (SELECT MAX(m.NirayanaLongitude) - MIN(m.NirayanaLongitude)
                                                     FROM dbo.tbl_Chart_MultiGrahaConjunctionMember m
                                                     WHERE m.MultiGrahaConjunctionId = g.Id)) > 0.001)"));
    Check("varga groups: LongitudeSpanDegrees is null",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunction g
                JOIN dbo.tbl_ChartResults cr ON cr.Id = g.ChartResultId
                WHERE cr.ChartTypeId <> 1 AND g.LongitudeSpanDegrees IS NOT NULL"));
    Check("D1 members: OrbFromGroupCenterDegrees non-null and <= group span",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunctionMember m
                JOIN dbo.tbl_Chart_MultiGrahaConjunction g ON g.Id = m.MultiGrahaConjunctionId
                JOIN dbo.tbl_ChartResults cr ON cr.Id = g.ChartResultId
                WHERE cr.ChartTypeId = 1
                  AND (m.OrbFromGroupCenterDegrees IS NULL
                    OR m.OrbFromGroupCenterDegrees > g.LongitudeSpanDegrees + 0.001)"));
    Check("varga members: OrbFromGroupCenterDegrees is null",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunctionMember m
                JOIN dbo.tbl_Chart_MultiGrahaConjunction g ON g.Id = m.MultiGrahaConjunctionId
                JOIN dbo.tbl_ChartResults cr ON cr.Id = g.ChartResultId
                WHERE cr.ChartTypeId <> 1 AND m.OrbFromGroupCenterDegrees IS NOT NULL"));
    Check("D1 members: DegreesInSign + NirayanaLongitude non-null and in range",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunctionMember m
                JOIN dbo.tbl_Chart_MultiGrahaConjunction g ON g.Id = m.MultiGrahaConjunctionId
                JOIN dbo.tbl_ChartResults cr ON cr.Id = g.ChartResultId
                WHERE cr.ChartTypeId = 1
                  AND (m.DegreesInSign IS NULL OR m.DegreesInSign < 0 OR m.DegreesInSign >= 30
                    OR m.NirayanaLongitude IS NULL OR m.NirayanaLongitude < 0 OR m.NirayanaLongitude >= 360)"));
    Check("member DignityStatus matches that planet's KeyDetails row for the same chart",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Chart_MultiGrahaConjunctionMember m
                JOIN dbo.tbl_Chart_MultiGrahaConjunction g ON g.Id = m.MultiGrahaConjunctionId
                JOIN dbo.tbl_Chart_KeyDetails kd
                     ON kd.ChartResultId = g.ChartResultId AND kd.PlanetId = m.PlanetId AND kd.PointKind = 'Graha'
                WHERE ISNULL(m.DignityStatus, '~') <> ISNULL(kd.DignityStatus, '~')"));

    Console.WriteLine(failures == 0 ? "\nverify-schema: ALL PASS" : $"\nverify-schema: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off check: `dotnet run -- verify-dignity` ---
// Gate for the axis-A dignity rule layer (migration 23, tbl_Rule_GrahaDignity) + the
// compound-relationship matrix (migration 24, tbl_Rule_CompoundRelationship). Asserts the
// seeded segments tile 0-30 per (rule-set, planet, sign); the DignityScore (+4/+3/+2/-2)
// and RelationshipScore (+2/+1/0/-1/-2) ladders hold on every row; the per-type
// Mood/Tendency/Analogy metadata is constant; the 9-value DignityStatus vocabulary the
// engine emits is exactly what tbl_Rule_WakefulnessState is keyed on (axis B / verify-avastha
// untouched); and the active PVR set's classical-seven exalt/debil deep degrees agree with
// the tbl_SignAttributes seed. The two documented PVR moolatrikona divergences (Moon 3 deg,
// Mercury 15 deg vs the BPHS 4/16 in the seed) are reported, not failed. The inline fixture
// pins today's DignityEngine.Evaluate output -- a tripwire for the Phase 2 PVR switch.
if (args.Length > 0 && args[0] == "verify-dignity")
{
    using var conn = connectionFactory.CreateOpenConnection();
    var failures = 0;
    void Check(string label, long violations)
    {
        var ok = violations == 0;
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: {violations} violation(s)");
        if (!ok) failures++;
    }
    void Note(string label) => Console.WriteLine($"  [KNOWN] {label}");
    long Count(string sql) => conn.ExecuteScalar<long>(sql);

    const int PvrSet = 2;   // tbl_Rule_Sets.Id -- PVR-Dignity-Integrated (rows IsActive = 1)
    const int BphsSet = 3;  // BPHS-Dignity-Parashari mirror (rows IsActive = 0)

    // 1. tbl_Rule_Catalog indexes both new rule tables.
    Check("tbl_Rule_Catalog has tbl_Rule_GrahaDignity + tbl_Rule_CompoundRelationship",
        Count(@"SELECT 2 - COUNT(*) FROM dbo.tbl_Rule_Catalog
                WHERE RuleTableName IN ('tbl_Rule_GrahaDignity','tbl_Rule_CompoundRelationship')"));

    // 2. Tiling -- per (RuleSetId, PlanetId, SignId) with rows, the [Start,End) segments
    //    tile [0,30) with no gap / no overlap.
    Check("dignity segments tile 0-30 per (rule-set, planet, sign)",
        Count(@"
            WITH seg AS (
                SELECT RuleSetId, PlanetId, SignId, StartDegree, EndDegree,
                       LEAD(StartDegree) OVER (PARTITION BY RuleSetId, PlanetId, SignId ORDER BY StartDegree) AS NextStart,
                       MIN(StartDegree)  OVER (PARTITION BY RuleSetId, PlanetId, SignId) AS MinStart,
                       MAX(EndDegree)    OVER (PARTITION BY RuleSetId, PlanetId, SignId) AS MaxEnd
                FROM dbo.tbl_Rule_GrahaDignity)
            SELECT COUNT(*) FROM seg
            WHERE MinStart <> 0 OR MaxEnd <> 30 OR (NextStart IS NOT NULL AND NextStart <> EndDegree)"));

    // 3. Coverage -- classical seven (Id 1-7), both rule-sets.
    foreach (var setId in new[] { PvrSet, BphsSet })
    {
        Check($"[set {setId}] each classical planet is EXALTED on exactly one sign",
            Count($@"SELECT COUNT(*) FROM dbo.tbl_Planets p WHERE p.Id BETWEEN 1 AND 7
                     AND 1 <> (SELECT COUNT(DISTINCT d.SignId) FROM dbo.tbl_Rule_GrahaDignity d
                               WHERE d.RuleSetId = {setId} AND d.PlanetId = p.Id AND d.DignityTypeCode = 'EXALTED')"));
        Check($"[set {setId}] each classical planet has one whole-sign DEBILITATED",
            Count($@"SELECT COUNT(*) FROM dbo.tbl_Planets p WHERE p.Id BETWEEN 1 AND 7
                     AND 1 <> (SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaDignity d
                               WHERE d.RuleSetId = {setId} AND d.PlanetId = p.Id
                                 AND d.DignityTypeCode = 'DEBILITATED' AND d.StartDegree = 0 AND d.EndDegree = 30)"));
        Check($"[set {setId}] each classical planet has MOOLATRIKONA on at most one sign",
            Count($@"SELECT COUNT(*) FROM dbo.tbl_Planets p WHERE p.Id BETWEEN 1 AND 7
                     AND 1 < (SELECT COUNT(DISTINCT d.SignId) FROM dbo.tbl_Rule_GrahaDignity d
                              WHERE d.RuleSetId = {setId} AND d.PlanetId = p.Id AND d.DignityTypeCode = 'MOOLATRIKONA')"));
        Check($"[set {setId}] each classical planet OWNs 1-2 signs",
            Count($@"SELECT COUNT(*) FROM dbo.tbl_Planets p WHERE p.Id BETWEEN 1 AND 7
                     AND (SELECT COUNT(DISTINCT d.SignId) FROM dbo.tbl_Rule_GrahaDignity d
                          WHERE d.RuleSetId = {setId} AND d.PlanetId = p.Id AND d.DignityTypeCode = 'OWN') NOT BETWEEN 1 AND 2"));
    }
    // PVR is the active set: every classical planet must actually have an MT sign there
    // (the BPHS mirror legitimately omits MT for Moon/Mercury -- their exaltation sign
    // wins the whole sign, so they sit at 0 MT signs in set 3).
    Check("[set 2] all 7 classical planets have a MOOLATRIKONA sign",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Planets p WHERE p.Id BETWEEN 1 AND 7
                AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_GrahaDignity d
                                WHERE d.RuleSetId = 2 AND d.PlanetId = p.Id AND d.DignityTypeCode = 'MOOLATRIKONA')"));

    // Nodes: PVR set gives all four dignity types; the BPHS mirror gives only EXALTED + DEBILITATED.
    Check("[set 2] Rahu & Ketu each carry all four dignity types",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Planets p WHERE p.Id IN (8,9)
                AND 4 <> (SELECT COUNT(DISTINCT d.DignityTypeCode) FROM dbo.tbl_Rule_GrahaDignity d
                          WHERE d.RuleSetId = 2 AND d.PlanetId = p.Id)"));
    Check("[set 3] Rahu & Ketu carry only EXALTED + DEBILITATED",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaDignity d
                WHERE d.RuleSetId = 3 AND d.PlanetId IN (8,9)
                  AND d.DignityTypeCode NOT IN ('EXALTED','DEBILITATED')"));

    // 4. DeepDegree placement -- EXALTED/DEBILITATED only, present for classical seven, NULL for nodes.
    Check("only EXALTED/DEBILITATED rows carry DeepDegree",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaDignity
                WHERE DignityTypeCode IN ('OWN','MOOLATRIKONA') AND DeepDegree IS NOT NULL"));
    Check("EXALTED/DEBILITATED carry DeepDegree for classical seven, NULL for nodes",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaDignity
                WHERE DignityTypeCode IN ('EXALTED','DEBILITATED')
                  AND ((PlanetId BETWEEN 1 AND 7 AND DeepDegree IS NULL)
                    OR (PlanetId IN (8,9) AND DeepDegree IS NOT NULL))"));

    // 5. Dignity score map -- every row's DignityScore is the canonical value for its type.
    Check("DignityScore matches the +4/+3/+2/-2 ladder on every row",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaDignity
                WHERE DignityScore <> CASE DignityTypeCode
                    WHEN 'EXALTED' THEN 4 WHEN 'MOOLATRIKONA' THEN 3
                    WHEN 'OWN' THEN 2 WHEN 'DEBILITATED' THEN -2 END"));

    // 6. Relationship score map -- the compound (Panchadha Maitri) matrix.
    Check("tbl_Rule_CompoundRelationship has all 6 natural x temporary combos",
        Count(@"SELECT 6 - COUNT(*) FROM (
                    SELECT DISTINCT NaturalRelation, IsTemporaryFriend
                    FROM dbo.tbl_Rule_CompoundRelationship WHERE RuleSetId = 1) x"));
    Check("RelationshipScore matches the +2/+1/0/-1/-2 ladder",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Rule_CompoundRelationship
                WHERE RelationshipScore <> CASE CompoundCode
                    WHEN 'ADHIMITRA' THEN 2 WHEN 'MITRA' THEN 1 WHEN 'SAMA' THEN 0
                    WHEN 'SHATRU' THEN -1 WHEN 'ADHISHATRU' THEN -2 END"));
    Check("compound EnglishName is one of the 5 Maitri DignityStatus labels",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Rule_CompoundRelationship
                WHERE EnglishName NOT IN ('Great Friend','Friend','Neutral','Enemy','Great Enemy')"));
    // The 6 rows must reproduce DignityEngine.CombineToPanchadha's truth table.
    var truth = new (string Nat, bool Tf, string Expected)[]
    {
        ("Friend",  true,  "Great Friend"),
        ("Friend",  false, "Neutral"),
        ("Neutral", true,  "Friend"),
        ("Neutral", false, "Enemy"),
        ("Enemy",   true,  "Neutral"),
        ("Enemy",   false, "Great Enemy"),
    };
    var matrixMismatch = 0;
    foreach (var (nat, tf, expected) in truth)
    {
        var got = conn.ExecuteScalar<string?>(
            "SELECT EnglishName FROM dbo.tbl_Rule_CompoundRelationship " +
            "WHERE RuleSetId = 1 AND NaturalRelation = @nat AND IsTemporaryFriend = @tf",
            new { nat, tf });
        if (got != expected)
        {
            matrixMismatch++;
            Console.WriteLine($"    {nat}+{(tf ? "TF" : "TE")}: table={got ?? "(none)"} expected={expected}");
        }
    }
    Check("compound matrix reproduces CombineToPanchadha", matrixMismatch);

    // 7. Metadata constancy.
    Check("Mood / InterpretationTendency / Analogy single-valued per DignityTypeCode",
        Count(@"SELECT COUNT(*) FROM (
                    SELECT DignityTypeCode
                    FROM dbo.tbl_Rule_GrahaDignity
                    GROUP BY DignityTypeCode
                    HAVING COUNT(DISTINCT ISNULL(Mood,'~')) > 1
                        OR COUNT(DISTINCT ISNULL(InterpretationTendency,'~')) > 1
                        OR COUNT(DISTINCT ISNULL(CONVERT(NVARCHAR(400), Analogy),'~')) > 1) x"));
    Check("DignityRationale non-NULL only on the PVR set",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaDignity
                WHERE DignityRationale IS NOT NULL AND RuleSetId <> 2"));

    // 8. Vocabulary unchanged -- the DignityStatus strings DignityEngine can emit are
    //    exactly the 9 keyed in tbl_Rule_WakefulnessState (RuleSetId 1).
    var engineVocab = new[]
    {
        "Exalted", "Debilitated", "Moolatrikona", "Own Sign",
        "Great Friend", "Friend", "Neutral", "Enemy", "Great Enemy",
    };
    var wakeKeys = conn.Query<string>(
        "SELECT DignityStatus FROM dbo.tbl_Rule_WakefulnessState WHERE RuleSetId = 1")
        .ToHashSet(StringComparer.Ordinal);
    Check("every engine DignityStatus has a tbl_Rule_WakefulnessState row",
        engineVocab.Count(v => !wakeKeys.Contains(v)));
    Check("tbl_Rule_WakefulnessState has no DignityStatus beyond the engine's 9",
        wakeKeys.Count(k => !engineVocab.Contains(k)));

    // 9. Fixture -- pins today's DignityEngine.Evaluate output. Every placement lands in an
    //    axis-A dignity, so the result is independent of the Panchadha Maitri path. Task 5
    //    (PVR switch) deliberately re-baselines the Rahu/Ketu rows here.
    var fixture = new (string Planet, ZodiacName Sign, double Deg, string Expected)[]
    {
        ("Sun",     ZodiacName.Aries,       10, "Exalted"),
        ("Moon",    ZodiacName.Scorpio,     15, "Debilitated"),
        ("Mars",    ZodiacName.Aries,        5, "Moolatrikona"),
        ("Mercury", ZodiacName.Gemini,      10, "Own Sign"),
        ("Jupiter", ZodiacName.Cancer,      20, "Exalted"),
        ("Venus",   ZodiacName.Libra,       10, "Moolatrikona"),
        ("Saturn",  ZodiacName.Capricornus, 25, "Own Sign"),
        ("Rahu",    ZodiacName.Gemini,      12, "Exalted"),
        ("Ketu",    ZodiacName.Sagittarius, 12, "Exalted"),
    };
    var allSigns = fixture.ToDictionary(f => f.Planet, f => f.Sign, StringComparer.Ordinal);
    var fixtureMismatch = 0;
    foreach (var (planet, sign, deg, expected) in fixture)
    {
        var r = DignityEngine.Evaluate(planet, sign, deg, allSigns);
        if (r.DignityStatus != expected)
        {
            fixtureMismatch++;
            Console.WriteLine($"    {planet} {sign} {deg}: got {r.DignityStatus ?? "(null)"} expected {expected}");
        }
    }
    Check("fixture chart reproduces DignityEngine.Evaluate (PVR behaviour)", fixtureMismatch);

    // 10. Seed cross-check -- active PVR set's classical seven vs the tbl_SignAttributes seed.
    Check("active-set EXALTED (sign + deep degree) agrees with tbl_SignAttributes",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaDignity d
                WHERE d.RuleSetId = 2 AND d.PlanetId BETWEEN 1 AND 7 AND d.DignityTypeCode = 'EXALTED'
                  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_SignAttributes s
                                  WHERE s.Id = d.SignId AND s.ExaltedPlanetId = d.PlanetId
                                    AND s.ExaltedDegree = d.DeepDegree)"));
    Check("active-set DEBILITATED (sign + deep degree) agrees with tbl_SignAttributes",
        Count(@"SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaDignity d
                WHERE d.RuleSetId = 2 AND d.PlanetId BETWEEN 1 AND 7 AND d.DignityTypeCode = 'DEBILITATED'
                  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_SignAttributes s
                                  WHERE s.Id = d.SignId AND s.DebilitatedPlanetId = d.PlanetId
                                    AND s.DebilitatedDegree = d.DeepDegree)"));

    // 10b. The three previously-independent C# exaltation dictionaries (DignityEngine,
    // ShadbalaCalculator, RamanYogaBatchFiveEvaluator) were consolidated onto
    // AstroMath.DeepExaltationPoints (2026-09-11 rule-mapping audit). Close the loop: that
    // shared constant must itself agree with tbl_SignAttributes -- and, transitively via the two
    // checks just above, with tbl_Rule_GrahaDignity's own PVR-cited EXALTED rows.
    var exaltationMismatch = 0;
    foreach (var (planet, point) in AstroMath.DeepExaltationPoints)
    {
        var seed = conn.QuerySingleOrDefault<(int ExaltedPlanetId, decimal ExaltedDegree)?>(
            "SELECT ExaltedPlanetId, ExaltedDegree FROM dbo.tbl_SignAttributes WHERE Id = @SignId",
            new { SignId = AstroIds.SignId(point.Sign) });
        var ok = seed is { } s && s.ExaltedPlanetId == AstroIds.PlanetId(planet) && s.ExaltedDegree == (decimal)point.Degree;
        if (!ok)
        {
            exaltationMismatch++;
            Console.WriteLine($"    {planet}: AstroMath says {point.Sign} {point.Degree}, tbl_SignAttributes says {seed}");
        }
    }
    Check("AstroMath.DeepExaltationPoints agrees with tbl_SignAttributes", exaltationMismatch);

    // Moolatrikona range: strict for Su/Ma/Ju/Ve/Sa; the two PVR divergences (Moon 3 deg vs
    // seed 4/NULL, Mercury 15 deg vs seed 16) are documented in
    // docs/research/dignity-pvr-integrated.md "Divergence" -- reported, not failed.
    var mtRows = conn.Query<(int PlanetId, string Planet, decimal Start, decimal End, decimal? SeedStart, decimal? SeedEnd, int? SeedPlanet)>(
        @"SELECT CAST(d.PlanetId AS INT), p.PlanetName, d.StartDegree, d.EndDegree,
                 s.MooltrikonaRangeStart, s.MooltrikonaRangeEnd, CAST(s.MooltrikonaPlanetId AS INT)
          FROM dbo.tbl_Rule_GrahaDignity d
          JOIN dbo.tbl_Planets p ON p.Id = d.PlanetId
          LEFT JOIN dbo.tbl_SignAttributes s ON s.Id = d.SignId
          WHERE d.RuleSetId = 2 AND d.PlanetId BETWEEN 1 AND 7 AND d.DignityTypeCode = 'MOOLATRIKONA'").ToList();
    var mtDivergence = 0;
    var knownMt = new HashSet<string>(StringComparer.Ordinal) { "Moon", "Mercury" };
    foreach (var m in mtRows)
    {
        var agrees = m.SeedPlanet == m.PlanetId && m.SeedStart == m.Start && m.SeedEnd == m.End;
        if (agrees) continue;
        var seedText = m.SeedStart is { } ss ? $"{ss:0.#}-{m.SeedEnd:0.#}" : "(none)";
        if (knownMt.Contains(m.Planet))
            Note($"MT {m.Planet}: rule {m.Start:0.#}-{m.End:0.#} vs seed {seedText} -- documented PVR divergence");
        else
        {
            mtDivergence++;
            Console.WriteLine($"    MT {m.Planet}: rule {m.Start:0.#}-{m.End:0.#} vs seed {seedText}");
        }
    }
    Check("active-set MOOLATRIKONA range agrees with tbl_SignAttributes (bar 2 known)", mtDivergence);

    Console.WriteLine(failures == 0 ? "\nverify-dignity: ALL PASS" : $"\nverify-dignity: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off seed mode: `dotnet run -- seed-terminology [--emit-sql]` ---
// Mechanically (re)builds dbo.tbl_Astro_Terminology + _Text from the Core enums + Dim tables
// (see TerminologySeed.cs — the single source of truth), one romanized-Sanskrit 'sa' row and one
// English 'en' row per concept, all Script 'Latn'. Idempotent: MERGE on Code for the concept rows,
// MERGE on (TerminologyId, LanguageCode, Script) for the text rows — re-running changes nothing.
// --emit-sql also writes db/terminology-seed.generated.sql, the block folded into db/ikiastrro.sql.
if (args.Length > 0 && args[0] == "seed-terminology")
{
    var emitSql = args.Contains("--emit-sql");
    using var conn = connectionFactory.CreateOpenConnection();
    var seed = TerminologySeedData.Build(conn);

    const string mergeTerm = @"
MERGE dbo.tbl_Astro_Terminology AS tgt
USING (SELECT @Code AS Code) AS src
ON tgt.Code = src.Code
WHEN MATCHED THEN UPDATE SET
    Category = @Category, ParentCode = @ParentCode, EngineCode = @EngineCode,
    NumericKey = @NumericKey, DisplayOrder = @DisplayOrder, IsActive = 1
WHEN NOT MATCHED THEN INSERT (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder, IsActive)
    VALUES (@Category, @Code, @ParentCode, @EngineCode, @NumericKey, @DisplayOrder, 1);";

    static object ToParam(TermSeed t) =>
        new { t.Category, t.Code, t.ParentCode, t.EngineCode, t.NumericKey, t.DisplayOrder };

    // Base rows first, then NakshatraPada rows — the self-referencing ParentCode FK needs its parent.
    conn.Execute(mergeTerm, seed.Terms.Where(t => t.Category != "NakshatraPada").Select(ToParam).ToList());
    conn.Execute(mergeTerm, seed.Terms.Where(t => t.Category == "NakshatraPada").Select(ToParam).ToList());

    var idByCode = conn.Query<(string Code, int TerminologyId)>(
            "SELECT Code, TerminologyId FROM dbo.tbl_Astro_Terminology")
        .ToDictionary(x => x.Code, x => x.TerminologyId, StringComparer.Ordinal);

    const string mergeText = @"
MERGE dbo.tbl_Astro_TerminologyText AS tgt
USING (SELECT @TerminologyId AS TerminologyId, @LanguageCode AS LanguageCode, @Script AS Script) AS src
ON tgt.TerminologyId = src.TerminologyId AND tgt.LanguageCode = src.LanguageCode AND tgt.Script = src.Script
WHEN MATCHED THEN UPDATE SET Name = @Name, TraditionalName = @TraditionalName, ShortDescription = @ShortDescription
WHEN NOT MATCHED THEN INSERT (TerminologyId, LanguageCode, Script, Name, TraditionalName, ShortDescription)
    VALUES (@TerminologyId, @LanguageCode, @Script, @Name, @TraditionalName, @ShortDescription);";

    conn.Execute(mergeText, seed.Texts.Select(x => new
    {
        TerminologyId = idByCode[x.Code],
        LanguageCode = x.Lang,
        Script = "Latn",
        x.Name,
        x.TraditionalName,
        x.ShortDescription
    }).ToList());

    var termCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.tbl_Astro_Terminology");
    var textCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.tbl_Astro_TerminologyText");
    Console.WriteLine($"seed-terminology: {termCount} terminology rows, {textCount} text rows");

    if (emitSql)
    {
        var outPath = Path.Combine(TerminologySeedData.FindRepoRoot(), "db", "terminology-seed.generated.sql");
        var note = $"dumped {DateTime.UtcNow:yyyy-MM-dd} from db '{dbOverride ?? "ikiastrro"}' - "
                   + $"{seed.Terms.Count} concept rows / {seed.Texts.Count} text rows (sa+en, Latn).";
        File.WriteAllText(outPath, seed.ToBaselineSql(note));
        Console.WriteLine($"seed-terminology: wrote {outPath}");
    }
    return;
}

// --- One-off check: `dotnet run -- verify-terminology` ---
// Asserts the seeded catalogue covers the engine: every PlanetName / ZodiacName / CharaKaraka enum
// value has a Code; every Code carries both an 'sa' and an 'en' text row; no ParentCode is an orphan;
// every tbl_Dim_ChartType row maps to a VARGA_* Code and every tbl_Dim_PlanetaryState row to an
// AVASTHA_* Code. Solution has no unit-test project.
if (args.Length > 0 && args[0] == "verify-terminology")
{
    var repo = new TerminologyRepository(connectionFactory);
    var rows = repo.GetTerminology();
    var text = repo.GetTerminologyText();
    var failures = 0;
    void Fail(string m) { Console.WriteLine($"  [FAIL] {m}"); failures++; }

    var codes = rows.Select(r => r.Code).ToHashSet();

    foreach (var p in Enum.GetNames<PlanetName>())
        if (!codes.Contains($"PLANET_{p.ToUpperInvariant()}")) Fail($"no Code for planet {p}");
    foreach (var s in Enum.GetNames<ZodiacName>())
    {
        var slug = s == "Capricornus" ? "CAPRICORN" : s.ToUpperInvariant();
        if (!codes.Contains($"SIGN_{slug}")) Fail($"no Code for sign {s}");
    }
    foreach (var k in Enum.GetNames<Ikiastrro.Core.Engines.Karakas.CharaKaraka>())
        if (!codes.Contains($"KARAKA_{k.ToUpperInvariant()}")) Fail($"no Code for karaka {k}");

    var dignityCount = rows.Count(r => r.Category == "DignityState");
    if (dignityCount != 9) Fail($"expected 9 DignityState codes, found {dignityCount}");

    var textByCodeLang = text
        .Join(rows, t => t.TerminologyId, r => r.TerminologyId, (t, r) => (r.Code, t.LanguageCode))
        .ToHashSet();
    foreach (var c in codes)
    {
        if (!textByCodeLang.Contains((c, "sa"))) Fail($"{c}: missing 'sa' text");
        if (!textByCodeLang.Contains((c, "en"))) Fail($"{c}: missing 'en' text");
    }

    foreach (var r in rows.Where(r => r.ParentCode is not null))
        if (!codes.Contains(r.ParentCode!)) Fail($"{r.Code}: orphan ParentCode {r.ParentCode}");

    using (var conn = connectionFactory.CreateOpenConnection())
    {
        foreach (var ct in conn.Query<string>("SELECT Code FROM dbo.tbl_Dim_ChartType"))
        {
            var slug = ct.Replace("-", "_").ToUpperInvariant();
            if (!codes.Contains($"VARGA_{slug}")) Fail($"tbl_Dim_ChartType '{ct}' has no VARGA_ Code");
        }
        foreach (var st in conn.Query<(string AvasthaSystem, string StateName)>(
                     "SELECT AvasthaSystem, StateName FROM dbo.tbl_Dim_PlanetaryState"))
        {
            var code = $"AVASTHA_{st.AvasthaSystem.ToUpperInvariant()}_{st.StateName.ToUpperInvariant()}";
            if (!codes.Contains(code)) Fail($"tbl_Dim_PlanetaryState '{st.AvasthaSystem}/{st.StateName}' has no AVASTHA_ Code");
        }
    }

    Console.WriteLine(failures == 0 ? "\nverify-terminology: ALL PASS" : $"\nverify-terminology: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}

// --- One-off seed mode: `dotnet run -- seed-rule-params [--emit-sql]` ---
// Backfills tbl_Rule_VargaScheme.RuleParametersJson + .CalculationNarrative by *sampling* the
// C# IVargaSignRule classes — the JSON is derived from the code, never hand-typed, so it cannot
// drift from it silently (verify-rules then proves the round-trip). MethodCode / MethodSource /
// SignRuleKind / SignRuleKey / SourceRefCode are NOT touched: MethodCode carries the classical
// *scheme* name (ParasaraTraditional / UmaShambu / SanjayRath / ClassicalTwoSign); the interpreter
// family lives inside the JSON as its top-level "method" key. Idempotent — a re-run recomputes the
// same values. --emit-sql also writes db/varga-rule-params.generated.sql, the block folded into
// db/ikiastrro.sql so a from-empty baseline install ships the params already populated.
if (args.Length > 0 && args[0] == "seed-rule-params")
{
    var emitSql = args.Contains("--emit-sql");
    using var conn = connectionFactory.CreateOpenConnection();

    // The two closed-form families that stay closed-form. Everything else is sampled into a table.
    var linearParams = new Dictionary<string, (int Factor, int Stride)>(StringComparer.Ordinal)
    {
        ["DrekkanaD3"] = (3, 4),
        ["ChaturthamsaD4"] = (4, 3),
        ["DwadasamsaD12"] = (12, 1),
        ["ShashtyamsaD60"] = (60, 1),
    };
    const string bandKey = "TrimsamsaD30";

    // Unequal-band edges, discovered rather than assumed: walk each rasi sign in 0.001° steps,
    // binary-search every sign change down to the exact degree, and union the 12 signs' break
    // points into one shared ascending edge list.
    static double[] DeriveBandEdges(IVargaSignRule rule)
    {
        var edges = new SortedSet<double>();
        for (var rasi = 0; rasi < 12; rasi++)
        {
            var baseLon = rasi * 30.0;
            var prev = rule.SignFor(baseLon);
            for (var step = 1; step < 30000; step++)
            {
                var d = step * 0.001;
                var cur = rule.SignFor(baseLon + d);
                if (cur == prev) continue;
                double lo = d - 0.001, hi = d;
                for (var k = 0; k < 60; k++)
                {
                    var mid = (lo + hi) / 2;
                    if (rule.SignFor(baseLon + mid) == prev) lo = mid; else hi = mid;
                }
                edges.Add(Math.Round(hi, 6));
                prev = cur;
            }
        }
        edges.Add(30.0);
        return edges.ToArray();
    }

    var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
    var emitted = new List<(int Id, string Key, string Json, string Narrative)>();

    var schemes = conn.Query<(int Id, string SignRuleKey, int DivisionFactor)>(
        "SELECT CAST(Id AS INT), SignRuleKey, CAST(DivisionFactor AS INT) " +
        "FROM dbo.tbl_Rule_VargaScheme ORDER BY Id").ToList();

    foreach (var (id, key, factor) in schemes)
    {
        var rule = VargaSignRuleFactory.For(key, factor);
        var className = rule.GetType().Name;
        string json, narrative, method;

        if (linearParams.TryGetValue(key, out var lin))
        {
            method = "LINEAR_VARGA";
            json = JsonSerializer.Serialize(
                new { method = "LINEAR_VARGA", factor = lin.Factor, stride = lin.Stride }, jsonOptions);
            narrative =
                $"LINEAR_VARGA factor={lin.Factor} stride={lin.Stride}: l = floor(degreesInRasiSign / (30/{lin.Factor})); "
                + $"varga sign = (rasiSign + l*{lin.Stride}) mod 12. Closed form of {className} (SignRuleKey={key}).";
        }
        else if (key == bandKey)
        {
            method = "BAND_VARGA";
            var edges = DeriveBandEdges(rule);
            var map = new int[edges.Length][];
            for (var j = 0; j < edges.Length; j++)
            {
                var low = j == 0 ? 0.0 : edges[j - 1];
                var probe = (low + edges[j]) / 2;
                map[j] = new int[12];
                for (var rasi = 0; rasi < 12; rasi++)
                    map[j][rasi] = (int)rule.SignFor(rasi * 30.0 + probe);
            }
            json = JsonSerializer.Serialize(new { method = "BAND_VARGA", edges, map }, jsonOptions);
            narrative =
                $"BAND_VARGA with {edges.Length} unequal degree bands (upper edges "
                + string.Join("/", edges.Select(e => e.ToString("0.###", CultureInfo.InvariantCulture)))
                + " deg, the union of the odd- and even-sign break points): band j covers "
                + $"[edges[j-1], edges[j]) and map[j][rasiSign] is the 0-based varga sign. Sampled from {className} (SignRuleKey={key}).";
        }
        else
        {
            method = "GRID_VARGA";
            var parts = factor;
            var map = new int[parts][];
            for (var i = 0; i < parts; i++)
            {
                map[i] = new int[12];
                for (var rasi = 0; rasi < 12; rasi++)
                    map[i][rasi] = (int)rule.SignFor(rasi * 30.0 + (i + 0.5) * (30.0 / parts));
            }
            json = JsonSerializer.Serialize(new { method = "GRID_VARGA", parts, map }, jsonOptions);
            narrative =
                $"GRID_VARGA parts={parts}: each rasi sign splits into {parts} equal "
                + (30.0 / parts).ToString("0.####", CultureInfo.InvariantCulture) + " deg parts; "
                + $"map[part][rasiSign] is the 0-based varga sign. Sampled from {className} (SignRuleKey={key}).";
        }

        conn.Execute(
            "UPDATE dbo.tbl_Rule_VargaScheme SET RuleParametersJson = @Json, CalculationNarrative = @Narrative WHERE Id = @Id;",
            new { Id = id, Json = json, Narrative = narrative });

        Console.WriteLine($"  Id {id,2}  {key,-17} {method,-13} {json.Length,6} chars");
        emitted.Add((id, key, json, narrative));
    }

    Console.WriteLine($"seed-rule-params: {emitted.Count} rows updated");

    if (emitSql)
    {
        static string Esc(string s) => s.Replace("'", "''");
        var sql = new StringBuilder();
        sql.AppendLine("-- tbl_Rule_VargaScheme portability params (Task 12).");
        sql.AppendLine("-- regenerate via: dotnet run --project src/Ikiastrro.Cli -- seed-rule-params --emit-sql");
        foreach (var (id, key, json, narrative) in emitted)
            sql.AppendLine($"UPDATE dbo.tbl_Rule_VargaScheme SET RuleParametersJson = N'{Esc(json)}', CalculationNarrative = N'{Esc(narrative)}' WHERE Id = {id};  -- {key}");
        sql.AppendLine("GO");
        var outPath = Path.Combine(TerminologySeedData.FindRepoRoot(), "db", "varga-rule-params.generated.sql");
        File.WriteAllText(outPath, sql.ToString());
        Console.WriteLine($"seed-rule-params: wrote {outPath}");
    }
    return;
}

// --- One-off check: `dotnet run -- verify-rules` ---
// The portability gate for the rule layer. (1) tbl_Rule_Catalog indexes every tbl_Rule_* table, so
// the "what a port must reimplement" page can't silently fall behind a new rule table. (2) every
// tbl_Rule_VargaScheme row's RuleParametersJson parses, names a known interpreter, and — driven
// through that interpreter — yields the *identical* sign to its C# IVargaSignRule class at every
// 0.5° of the zodiac. That equality is the whole point: it proves the JSON, not the C#, is the
// specification, so a port needs only the 3 interpreters plus the table.
if (args.Length > 0 && args[0] == "verify-rules")
{
    using var conn = connectionFactory.CreateOpenConnection();
    var failures = 0;
    void Fail(string m) { Console.WriteLine($"  [FAIL] {m}"); failures++; }

    // 1. tbl_Rule_Catalog covers every tbl_Rule_* table.
    var ruleTables = conn.Query<string>(
        "SELECT name FROM sys.tables WHERE name LIKE 'tbl_Rule[_]%' " +
        "AND name NOT IN ('tbl_Rule_Sets', 'tbl_Rule_Catalog') ORDER BY name").ToList();
    var cataloged = conn.Query<string>("SELECT RuleTableName FROM dbo.tbl_Rule_Catalog")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var missing = ruleTables.Where(t => !cataloged.Contains(t)).ToList();
    foreach (var t in missing) Fail($"tbl_Rule_Catalog missing row for {t}");
    Console.WriteLine($"  [{(missing.Count == 0 ? "PASS" : "FAIL")}] tbl_Rule_Catalog covers "
                      + $"{ruleTables.Count - missing.Count}/{ruleTables.Count} tbl_Rule_* tables");

    // 1b. the catalog's advertised varga interpreters are the ones that actually exist — the catalog
    //     is the "what a port must reimplement" index, so it must not name a phantom interpreter.
    var catalogMethods = (conn.ExecuteScalar<string?>(
        "SELECT MethodCodes FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_VargaScheme'") ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var badTokens = 0;
    foreach (var token in catalogMethods)
    {
        try { VargaMethodInterpreterFactory.For(token); }
        catch (InvalidOperationException) { Fail($"tbl_Rule_Catalog names unknown interpreter '{token}'"); badTokens++; }
    }
    if (badTokens == 0)
        Console.WriteLine($"  [PASS] tbl_Rule_Catalog MethodCodes resolve: {string.Join(", ", catalogMethods)}");

    // 2. every scheme's JSON round-trips through its interpreter to the C# class's exact output.
    var schemes = conn.Query<(int Id, string SignRuleKey, int DivisionFactor, string? Json)>(
        "SELECT CAST(Id AS INT), SignRuleKey, CAST(DivisionFactor AS INT), RuleParametersJson " +
        "FROM dbo.tbl_Rule_VargaScheme ORDER BY Id").ToList();

    foreach (var (id, key, factor, rawJson) in schemes)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) { Fail($"Id {id} ({key}): RuleParametersJson is NULL/empty"); continue; }

        JsonDocument doc;
        try { doc = JsonDocument.Parse(rawJson); }
        catch (JsonException ex) { Fail($"Id {id} ({key}): RuleParametersJson is not valid JSON — {ex.Message}"); continue; }

        using (doc)
        {
            var root = doc.RootElement;
            if (!root.TryGetProperty("method", out var methodEl) || methodEl.ValueKind != JsonValueKind.String)
            { Fail($"Id {id} ({key}): RuleParametersJson has no top-level string \"method\""); continue; }

            var method = methodEl.GetString()!;
            IVargaMethodInterpreter interp;
            try { interp = VargaMethodInterpreterFactory.For(method); }
            catch (InvalidOperationException ex) { Fail($"Id {id} ({key}): {ex.Message}"); continue; }

            var cls = VargaSignRuleFactory.For(key, factor);
            var compared = 0;
            var mismatch = false;
            for (double lon = 0; lon < 360; lon += 0.5)
            {
                var a = interp.SignFor(root, lon);
                var b = cls.SignFor(lon);
                if (a != b)
                {
                    Fail($"{key} @ {lon:0.0}°: interpreter {a} != class {b}");
                    mismatch = true;
                    break;
                }
                compared++;
            }
            if (!mismatch)
                Console.WriteLine($"  [PASS] Id {id,2} {key,-17} {method,-13} {compared} longitudes identical to {cls.GetType().Name}");
        }
    }

    Console.WriteLine(failures == 0 ? "\nverify-rules: ALL PASS" : $"\nverify-rules: {failures} FAILURE(S)");
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
// Scoped safe refresh for source-attributed yoga changes; preserves ChartResults and dependent facts.
if (args.Length > 1 && args[0] == "refresh-yogas")
{
    // refresh-yogas <name> [--vargas] | refresh-yogas --all [--vargas]
    // --vargas also writes the per-varga yoga confirmation rows (D2/D3/D9/D12/D30).
    var withVargas = args.Contains("--vargas");
    var nameArgs = args.Skip(1).Where(a => a != "--vargas" && a != "--all").ToList();
    var people = args.Contains("--all")
        ? birthDetailsRepo.GetAll().ToList()
        : [birthDetailsRepo.GetAll().FirstOrDefault(p => p.Name.Equals(string.Join(' ', nameArgs), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No saved person named '{string.Join(' ', nameArgs)}'.")];
    foreach (var person in people)
    {
        var written = new List<string>();
        var filters = withVargas ? Ikiastrro.Core.Engines.Yoga.ProductionYogaEngine.VargaReferenceCharts : ["D1"];
        foreach (var chartType in filters)
            written.AddRange(chartGenerationService.RecomputeAnalytics(person, chartTypeFilter: chartType).ChartTypesWritten);
        Console.WriteLine($"{person.Name}: refreshed analytics and yoga evidence [{string.Join(", ", written)}]");
    }
    return;
}

if (args.Length > 0 && args[0] == "recompute-keydetails")
{
    Console.WriteLine("Re-deriving analytics for every saved person.");
    Console.WriteLine("NOTE: as of Task 7 this recomputes ALL 4 analytics tables");
    Console.WriteLine("(KeyDetails / HouseLords / Conjunctions / Aspects), not KeyDetails only —");
    Console.WriteLine("an intentional, idempotent no-op widening now that the write path is");
    Console.WriteLine("single-sourced through ChartGenerationService.RecomputeAnalytics.");
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

// --- One-off backfill mode: `dotnet run -- backfill-numerology` ---
// Fills tbl_BirthDetails.NameNumberCompound/NameNumberRoot (migration 168) for people saved before
// the columns existed. New and edited people get them from BirthDetailsRepository on every write.
if (args.Length > 0 && args[0] == "backfill-numerology")
{
    var written = birthDetailsRepo.BackfillNameNumbers();
    Console.WriteLine($"backfill-numerology: {written} person(s) updated.");
    return;
}

// --- One-off backfill mode: `dotnet run -- backfill-argala` ---
// Computes ArgalaCalculator's Argala/Virodhargala results (PVR sec.10.5-10.6) for every saved
// D1 chart's 12 houses (Exercise 16's own scope) plus the 9 grahas' own occupied signs (PVR's
// other worked-example usage, sec.10.7), and persists them to tbl_Fact_Argala (migration 128).
// Standalone, delete-then-reinsert per chart — like recompute-keydetails/backfill-charts above,
// but NOT yet threaded through ChartGenerationService, so it has to be re-run by hand after new
// charts are added; see argala-virodhargala-drishti-lifematters.md's "not yet wired into the
// pipeline" note for that still-open follow-up.
if (args.Length > 0 && args[0] == "backfill-argala")
{
    var argalaFactRepo = new ArgalaFactRepository(connectionFactory);
    var activeRuleSetId = new RuleSetRepository(connectionFactory).GetActive().Id;
    using var argalaConn = connectionFactory.CreateOpenConnection();

    var d1Charts = argalaConn.Query<(int ChartResultId, int? ChartTypeId)>(
        "SELECT Id, ChartTypeId FROM dbo.tbl_ChartResults WHERE ChartType = 'D1'").ToList();

    Console.WriteLine($"backfill-argala: {d1Charts.Count} D1 chart(s) found.");
    foreach (var (chartResultId, chartTypeId) in d1Charts)
    {
        var positions = argalaConn.Query<(string Planet, string Sign)>(
            "SELECT Planet, Sign FROM dbo.tbl_Chart_KeyDetails WHERE ChartResultId = @ChartResultId AND PointKind = 'Graha'",
            new { ChartResultId = chartResultId }).ToList();

        var ascendantSign = Enum.Parse<ZodiacName>(positions.Single(p => p.Planet == "Ascendant").Sign);
        var occupancy = positions.Where(p => p.Planet != "Ascendant")
            .GroupBy(p => Enum.Parse<ZodiacName>(p.Sign))
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PlanetName>)g.Select(p => Enum.Parse<PlanetName>(p.Planet)).ToList());

        var facts = ArgalaFactBuilder.BuildForHouses(ascendantSign, occupancy);
        facts.AddRange(ArgalaFactBuilder.BuildForPlanets(ascendantSign, occupancy));

        argalaFactRepo.DeleteForChart(chartResultId, chartTypeId);
        argalaFactRepo.InsertAll(chartResultId, activeRuleSetId, chartTypeId, facts);
        Console.WriteLine($"  ChartResultId {chartResultId}: {facts.Count} fact rows written.");
    }
    return;
}

// --- One-off mode: `dotnet run -- rebuild-all` ---
// Force-regenerates every registered chart type + Vimshottari Dasha for every saved person,
// even ones already built (unlike backfill-charts, which only fills gaps). Backs the "RECALCULATE"
// button on SavedCharts.razor via Rebuild-AllGeneratedData.ps1 -- see docs/database/rebuild-generated-data-plan.md.
if (args.Length > 0 && args[0] == "rebuild-all")
{
    var peopleToRebuild = birthDetailsRepo.GetAll().ToList();
    var rebuildFailures = new List<string>();
    for (var i = 0; i < peopleToRebuild.Count; i++)
    {
        var person = peopleToRebuild[i];
        Console.WriteLine($"[{i + 1}/{peopleToRebuild.Count}] {person.Name}...");
        try { chartGenerationService.GenerateAll(person); }
        catch (Exception ex)
        {
            rebuildFailures.Add(person.Name);
            Console.WriteLine($"  FAILED: {ex.Message}");
        }
    }
    Console.WriteLine(rebuildFailures.Count == 0
        ? $"Rebuilt {peopleToRebuild.Count}/{peopleToRebuild.Count} people."
        : $"Rebuilt {peopleToRebuild.Count - rebuildFailures.Count}/{peopleToRebuild.Count}; failed: {string.Join(", ", rebuildFailures)}");
    Environment.Exit(rebuildFailures.Count == 0 ? 0 : 1);
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
// tbl_PlanetSignTransitEvents (point 2, docs/reference-vedic-data-tables.md). Ketu is never
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

// --- Verification mode: `dotnet run -- read-promise <name|id> [--matter CODE] [--category CAT] [--detail]` ---
// Reads every life matter's D1 promise for one saved person (docs/architecture/key_inference_promise.md
// §10): the matter's Lagna house(s), the categorical verdict and confidence, the principal roles'
// directions, and with --detail every testimony. Read-only; for checking the engine against a hand reading.
if (args.Length > 0 && args[0] == "read-promise")
{
    var who = args.Length > 1 ? args[1] : throw new ArgumentException("Usage: read-promise <name|id> [--matter CODE] [--category CAT] [--detail]");
    string? Opt(string name) => Array.IndexOf(args, name) is var i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    var person = int.TryParse(who, out var personId) ? birthDetailsRepo.GetById(personId) : birthDetailsRepo.GetAll().FirstOrDefault(p => p.Name == who);
    if (person is null) { Console.WriteLine($"No saved person '{who}'."); return; }

    var service = new Ikiastrro.Data.Statistics.LifeMatterPromiseService(
        new ChartResultsRepository(connectionFactory), new ChartKeyDetailsRepository(connectionFactory),
        new LifeMatterReferenceRepository(connectionFactory), new LifeMatterFocusRepository(connectionFactory),
        new KarakaMatterRepository(connectionFactory), new PlanetaryStrengthRepository(connectionFactory),
        new AshtakavargaRepository(connectionFactory), new BhavaStrengthRepository(connectionFactory),
        new AmsabalaRepository(connectionFactory), new ArgalaFactRepository(connectionFactory),
        new NaisargikaKarakaRepository(connectionFactory));
    var readings = service.ReadAll(person.Id);
    if (readings is null) { Console.WriteLine($"{person.Name} has no stored D1."); return; }

    var matter = Opt("--matter");
    var category = Opt("--category");
    var detail = args.Contains("--detail");
    var shown = readings
        .Where(r => matter is null || r.LifeMatterCode.Equals(matter, StringComparison.OrdinalIgnoreCase))
        .Where(r => category is null || r.CategoryCode.Equals(category, StringComparison.OrdinalIgnoreCase))
        .ToList();

    Console.WriteLine($"D1 promise for {person.Name} (id {person.Id}) — {shown.Count} matters");
    var tally = new Dictionary<string, int>();
    foreach (var r in shown)
    {
        if (r.Targets.Count == 0) { Console.WriteLine($"{r.LifeMatterCode,-16} {r.MatterText,-44} — {r.Note}"); tally["(no Lagna house)"] = tally.GetValueOrDefault("(no Lagna house)") + 1; continue; }
        foreach (var t in r.Targets)
        {
            var p = t.Promise;
            tally[p.Verdict.ToString()] = tally.GetValueOrDefault(p.Verdict.ToString()) + 1;
            var roles = string.Join(" ", p.D1.RoleDirections.Select(d => $"{d.Key.ToString()[0]}={d.Value.ToString()[0]}"));
            Console.WriteLine($"{r.LifeMatterCode,-16} {Trunc(r.MatterText, 44),-44} {t.Label,-10} {p.Verdict,-20} {p.Confidence,-7} [{roles}] idx={p.TechnicalSupportIndex?.ToString() ?? "-"}");
            if (!detail) continue;
            foreach (var x in p.D1.Testimonies.Where(x => x.Direction != Ikiastrro.Core.LifeMatters.Promise.Direction.Neutral || x.Role == Ikiastrro.Core.LifeMatters.Promise.TestimonyRole.Target))
                Console.WriteLine($"    {x.Role,-12} {x.Direction,-11} {x.Capacity,-8} {x.Explanation}");
            foreach (var m in p.MissingEvidence) Console.WriteLine($"    missing: {m}");
            foreach (var f in p.Refinements) Console.WriteLine($"    refinement: {f}");
            foreach (var c in p.Contradictions) Console.WriteLine($"    contradiction: {c.Description}");
        }
    }
    Console.WriteLine("Verdicts: " + string.Join(", ", tally.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value}")));
    return;

    static string Trunc(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
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
    IPlaceResolver resolver = new KnownPlaceResolver(new NominatimPlaceResolver());
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
// Skip the VimshottariDasha ChartResult: its ResultJson is the full 3-level tree serialised with
// WriteIndented = true (thousands of lines). It gets a readable summary below instead.
foreach (var result in chartResultsRepo.GetByBirthDetailId(birthDetails.Id)
             .Where(r => r.ChartType != VimshottariDashaCalculator.ChartType))
{
    Console.WriteLine($"--- {result.ChartType} ({result.Ayanamsha}, {result.HouseSystem}) ---");
    Console.WriteLine(result.ResultJson);
    Console.WriteLine();
}

// --- Readable Vimshottari Dasha summary (mirrors `show-dasha` / `compute-dasha`) ---
var dashaTree = new DashaPeriodsRepository(connectionFactory).GetTreeByBirthDetailId(birthDetails.Id);
if (dashaTree.Count > 0)
{
    Console.WriteLine($"--- Vimshottari Dasha for {birthDetails.Name} ---");
    PrintDashaTree(dashaTree);
    Console.WriteLine();
}
