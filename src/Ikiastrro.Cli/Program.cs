using System.Globalization;
using Dapper;
using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Calculators;
using Ikiastrro.Core.Dasha;
using Ikiastrro.Core.Jaimini;
using Ikiastrro.Core.Geocoding;
using Ikiastrro.Core.Models;
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
var orchestrator = ChartCalculationOrchestrator.CreateDefault(vargaSchemes);
var chartResultsRepo = new ChartResultsRepository(connectionFactory);
var vimshottariDashaService = new VimshottariDashaService(
    chartResultsRepo, new DashaPeriodsRepository(connectionFactory));
var chartGenerationService = new ChartGenerationService(
    orchestrator, vimshottariDashaService,
    chartResultsRepo,
    new ChartKeyDetailsRepository(connectionFactory),
    new ChartHouseLordsRepository(connectionFactory),
    new ChartConjunctionsRepository(connectionFactory),
    new ChartAspectsRepository(connectionFactory),
    new AvasthaRuleRepository(connectionFactory),
    new PlanetAvasthaRepository(connectionFactory),
    new RuleSetRepository(connectionFactory),
    new ChartTypeRepository(connectionFactory));

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

    // Every SignRuleKey seeded in tbl_Rule_VargaScheme must resolve through the factory
    string[] seededKeys =
    {
        "HoraD2Classic","HoraD2UmaShambu","DrekkanaD3","ChaturthamsaD4","PanchamsaD5",
        "ShashtamsaD6","SaptamsaD7","AshtamsaD8","NavamsaD9","DasamsaD10","RudramsaD11",
        "DwadasamsaD12","ShodasamsaD16","VimsamsaD20","SiddhamsaD24","NakshatramsaD27",
        "TrimsamsaD30","KhavedamsaD40","AkshavedamsaD45","ShashtyamsaD60"
    };
    int[] seededFactors = { 2, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 16, 20, 24, 27, 30, 40, 45, 60 };
    var factoryFailures = 0;
    for (var i = 0; i < seededKeys.Length; i++)
    {
        try { _ = VargaSignRuleFactory.For(seededKeys[i], seededFactors[i]).SignFor(15.0); }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] factory {seededKeys[i]}: {ex.Message}"); factoryFailures++; }
    }
    Console.WriteLine($"  [{(factoryFailures == 0 ? "PASS" : "FAIL")}] VargaSignRuleFactory resolves all 20 seeded keys");
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
    // Classical D2 has no export grid (JHora's D-2 is Uma Shambu) - covered by the
    // AstroMath.GetHoraSign checks above instead.
    var jhoraGrid = new Dictionary<string, ZodiacName[]>
    {
        ["D2-US"] = new[] { ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Cancer, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Pisces, ZodiacName.Aries, ZodiacName.Pisces, ZodiacName.Scorpio, ZodiacName.Scorpio },
        ["D3"]  = new[] { ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Scorpio, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Virgo, ZodiacName.Leo, ZodiacName.Capricornus, ZodiacName.Scorpio, ZodiacName.Taurus },
        ["D4"]  = new[] { ZodiacName.Aries, ZodiacName.Cancer, ZodiacName.Scorpio, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Sagittarius, ZodiacName.Cancer, ZodiacName.Sagittarius, ZodiacName.Libra, ZodiacName.Aries },
        ["D5"]  = new[] { ZodiacName.Aries, ZodiacName.Aquarius, ZodiacName.Virgo, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Virgo, ZodiacName.Aquarius, ZodiacName.Virgo, ZodiacName.Pisces, ZodiacName.Pisces },
        ["D6"]  = new[] { ZodiacName.Aries, ZodiacName.Taurus, ZodiacName.Scorpio, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Scorpio, ZodiacName.Gemini, ZodiacName.Sagittarius, ZodiacName.Sagittarius, ZodiacName.Sagittarius },
        ["D7"]  = new[] { ZodiacName.Aries, ZodiacName.Taurus, ZodiacName.Gemini, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Taurus, ZodiacName.Gemini, ZodiacName.Taurus, ZodiacName.Aries, ZodiacName.Libra },
        ["D8"]  = new[] { ZodiacName.Aries, ZodiacName.Gemini, ZodiacName.Capricornus, ZodiacName.Taurus, ZodiacName.Aries, ZodiacName.Libra, ZodiacName.Cancer, ZodiacName.Libra, ZodiacName.Cancer, ZodiacName.Cancer },
        ["D9"]  = new[] { ZodiacName.Aries, ZodiacName.Gemini, ZodiacName.Virgo, ZodiacName.Taurus, ZodiacName.Aries, ZodiacName.Pisces, ZodiacName.Cancer, ZodiacName.Aries, ZodiacName.Libra, ZodiacName.Aries },
        ["D10"] = new[] { ZodiacName.Aries, ZodiacName.Gemini, ZodiacName.Virgo, ZodiacName.Taurus, ZodiacName.Aries, ZodiacName.Cancer, ZodiacName.Cancer, ZodiacName.Leo, ZodiacName.Cancer, ZodiacName.Capricornus },
        ["D11"] = new[] { ZodiacName.Aries, ZodiacName.Cancer, ZodiacName.Scorpio, ZodiacName.Taurus, ZodiacName.Aries, ZodiacName.Aquarius, ZodiacName.Leo, ZodiacName.Pisces, ZodiacName.Taurus, ZodiacName.Scorpio },
        ["D12"] = new[] { ZodiacName.Aries, ZodiacName.Cancer, ZodiacName.Capricornus, ZodiacName.Taurus, ZodiacName.Aries, ZodiacName.Sagittarius, ZodiacName.Leo, ZodiacName.Capricornus, ZodiacName.Sagittarius, ZodiacName.Gemini },
        ["D16"] = new[] { ZodiacName.Aries, ZodiacName.Leo, ZodiacName.Scorpio, ZodiacName.Gemini, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Libra, ZodiacName.Taurus, ZodiacName.Libra, ZodiacName.Libra },
        ["D20"] = new[] { ZodiacName.Aries, ZodiacName.Virgo, ZodiacName.Aries, ZodiacName.Gemini, ZodiacName.Taurus, ZodiacName.Capricornus, ZodiacName.Scorpio, ZodiacName.Pisces, ZodiacName.Sagittarius, ZodiacName.Sagittarius },
        ["D24"] = new[] { ZodiacName.Leo, ZodiacName.Aquarius, ZodiacName.Sagittarius, ZodiacName.Scorpio, ZodiacName.Virgo, ZodiacName.Capricornus, ZodiacName.Taurus, ZodiacName.Pisces, ZodiacName.Taurus, ZodiacName.Taurus },
        ["D27"] = new[] { ZodiacName.Aries, ZodiacName.Scorpio, ZodiacName.Cancer, ZodiacName.Cancer, ZodiacName.Taurus, ZodiacName.Aquarius, ZodiacName.Aquarius, ZodiacName.Aries, ZodiacName.Sagittarius, ZodiacName.Gemini },
        ["D30"] = new[] { ZodiacName.Aries, ZodiacName.Aquarius, ZodiacName.Virgo, ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Virgo, ZodiacName.Sagittarius, ZodiacName.Virgo, ZodiacName.Pisces, ZodiacName.Pisces },
        ["D40"] = new[] { ZodiacName.Aries, ZodiacName.Aquarius, ZodiacName.Cancer, ZodiacName.Virgo, ZodiacName.Gemini, ZodiacName.Virgo, ZodiacName.Cancer, ZodiacName.Sagittarius, ZodiacName.Pisces, ZodiacName.Pisces },
        ["D45"] = new[] { ZodiacName.Aries, ZodiacName.Aries, ZodiacName.Gemini, ZodiacName.Virgo, ZodiacName.Gemini, ZodiacName.Capricornus, ZodiacName.Virgo, ZodiacName.Aries, ZodiacName.Scorpio, ZodiacName.Scorpio },
        ["D60"] = new[] { ZodiacName.Taurus, ZodiacName.Leo, ZodiacName.Capricornus, ZodiacName.Scorpio, ZodiacName.Cancer, ZodiacName.Aquarius, ZodiacName.Pisces, ZodiacName.Gemini, ZodiacName.Virgo, ZodiacName.Pisces },
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
                "WHERE bd.Name = 'Ramakrishnan' AND cr.ChartType = @ct AND kd.PointKind = 'Graha'",
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
              WHERE bd.Name = 'Ramakrishnan' AND cr1.ChartType = 'D1' AND d1.SignId = d9.SignId").ToList();
        Console.WriteLine($"  [INFO] Ramakrishnan D1/D9 Vargottama: {(vargottama.Count == 0 ? "none" : string.Join(", ", vargottama.Select(v => $"{v.Planet}({v.Sign})")))}");
    }

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

// --- One-off check: `dotnet run -- verify-avastha` ---
// Worked-example assertions for the Baaladi + Jagradadi avastha calculators against the seeded
// tbl_Rule_BaaladiState / tbl_Rule_JagradadiState rows (active rule set). Solution has no unit-test project.
if (args.Length > 0 && args[0] == "verify-avastha")
{
    var rules = new AvasthaRuleRepository(connectionFactory).GetActiveRuleSet();
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    string Baaladi(ZodiacName sign, decimal deg) => BaaladiAvastha.For(sign, deg, rules.Baaladi).StateName;
    decimal Fraction(ZodiacName sign, decimal deg) => BaaladiAvastha.For(sign, deg, rules.Baaladi).EffectFraction;
    string? Jagradadi(string dignity) => JagradadiAvastha.For(dignity, rules.JagradadiByDignity)?.StateName;

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

    Console.WriteLine(failures == 0 ? "\nverify-avastha: ALL PASS" : $"\nverify-avastha: {failures} FAILURE(S)");
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

    var ram = birthDetailsRepo.GetAll().First(p => p.Name == "Ramakrishnan");

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
        [PlanetName.Saturn] = 10.956, [PlanetName.Rahu] = 13.059,   // raw; calc reverses Rahu
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
              WHERE bd.Name = 'Ramakrishnan' AND cr.ChartType = 'D1' AND kd.CharaKaraka IS NOT NULL")
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
              WHERE bd.Name = 'Ramakrishnan' AND cr.ChartType = 'D9' AND kd.CharaKaraka = 'AK'");
        Check("D9 AK label travels", d9ak, "Rahu");
    }

    // --- Phase 3: Arudha Lagna + 12 Bhava Arudhas ---
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        string? SpSign(string chart, string code) => conn.ExecuteScalar<string>(
            @"SELECT kd.Sign FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name='Ramakrishnan' AND cr.ChartType=@c AND kd.Planet=@p AND kd.PointKind<>'Graha'",
            new { c = chart, p = code });

        Check("AL (D1) -> Capricornus", SpSign("D1", "AL"), "Capricornus");
        Check("A2..A12 present in D1", conn.ExecuteScalar<int>(
            @"SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id=kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id=cr.BirthDetailId
              WHERE bd.Name='Ramakrishnan' AND cr.ChartType='D1' AND kd.PointKind='Arudha'"), 12);
        // channel integrity: AL's D9 sign == NavamsaD9 rule applied to AL's D1 longitude
        var alD1Lon = conn.ExecuteScalar<double>(
            @"SELECT kd.NirayanaLongitudeDegrees FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id=kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id=cr.BirthDetailId
              WHERE bd.Name='Ramakrishnan' AND cr.ChartType='D1' AND kd.Planet='AL'");
        var expectedD9 = VargaSignRuleFactory.For("NavamsaD9", 9).SignFor(alD1Lon).ToString();
        Check("AL D9 channel integrity", SpSign("D9", "AL"), expectedD9);
    }

    // --- Phase 4: Hora Lagna + Gulika + Maandi (JHora golden record) ---
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        string? SpSign(string chart, string code) => conn.ExecuteScalar<string>(
            @"SELECT kd.Sign FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name='Ramakrishnan' AND cr.ChartType=@c AND kd.Planet=@p AND kd.PointKind<>'Graha'",
            new { c = chart, p = code });
        double SpLon(string code) => conn.ExecuteScalar<double>(
            @"SELECT kd.NirayanaLongitudeDegrees FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name='Ramakrishnan' AND cr.ChartType='D1' AND kd.Planet=@p",
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
        Check("Gulika (D9) -> Sagittarius", SpSign("D9", "Gulika"), "Sagittarius");
        Check("Maandi (D1) -> Libra",  SpSign("D1", "Maandi"), "Libra");
        Check("Maandi (D9) -> Pisces", SpSign("D9", "Maandi"), "Pisces");
        CheckLon("HL longitude",     SpLon("HL"),     353.9189, 0.5);
        CheckLon("Gulika longitude", SpLon("Gulika"), 187.7439, 0.5);
        CheckLon("Maandi longitude", SpLon("Maandi"), 198.1169, 0.5);
    }

    Console.WriteLine(failures == 0 ? "\nverify-jaimini: ALL PASS" : $"\nverify-jaimini: {failures} FAILURE(S)");
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

    // Forward-looking: every SourceRefCode used by a rule/terminology table must resolve.
    // No such columns exist yet (Plan 1) — this loop is a no-op today, a tripwire later.
    var refColumns = conn.Query<(string TableName, string ColumnName)>(@"
        SELECT t.name, c.name
        FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
        WHERE c.name = 'SourceRefCode'").ToList();
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
    Check("PlanetAvastha.PlanetId populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Fact_PlanetAvastha WHERE PlanetId IS NULL"));

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

    Console.WriteLine(failures == 0 ? "\nverify-schema: ALL PASS" : $"\nverify-schema: {failures} FAILURE(S)");
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
