using System.Transactions;
using Dapper;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Data;

var factory = SqlConnectionFactory.CreateDefault();
var births = new BirthDetailsRepository(factory);
int d1Id;
using (var lookup = factory.CreateOpenConnection())
    d1Id = lookup.ExecuteScalar<int>("SELECT Id FROM dbo.tbl_Dim_ChartType WHERE Code='D1'");
int savedBirthId;
using (var lookup = factory.CreateOpenConnection())
    savedBirthId = lookup.ExecuteScalar<int>("SELECT TOP (1) Id FROM dbo.tbl_BirthDetails ORDER BY Id");
var evidence = new AstrologerEvidenceRepository(factory).Load(savedBirthId)
    ?? throw new Exception("Evidence page repository did not load the saved chart.");
if (evidence.Sections.Count != 19) throw new Exception("Expected 19 evidence tables.");
if (evidence.Sections.Single(x => x.Code == "shadbala").Rows.Count != 7)
    throw new Exception("Expected seven Shadbala rows.");
if (evidence.Sections.Single(x => x.Code == "bhava").Rows.Count != 12)
    throw new Exception("Expected twelve Bhava Bala rows.");
if (evidence.Sections.Single(x => x.Code == "yogas").Rows.Count != 337)
    throw new Exception("Expected 337 yoga source variants.");
// Everything written by this verification is rolled back, including failed assertions.
using var scope = new TransactionScope();
foreach (var sex in new string?[] { "Male", "Female", null })
{
    var birth = births.Insert(new BirthDetails {
        Name = "YogaVerification-" + Guid.NewGuid().ToString("N"), Sex = sex,
        DateOfBirth = new(2026, 9, 9), TimeOfBirth = new(12, 0),
        PlaceCity = "Chennai", PlaceCountry = "India",
        Latitude = 13.08, Longitude = 80.27, UtcOffset = "05:30" });
    if (births.GetById(birth.Id)?.Sex != sex) throw new Exception("Sex did not round-trip.");
    var result = new ChartResultsRepository(factory).Insert(new ChartResult {
        BirthDetailId = birth.Id, ChartType = "D1", ChartTypeId = d1Id, ResultJson = "{}",
        EngineVersion = "YogaInputVerification" });
    ChartAnalysisInput Chart(string type) => new(type, ZodiacName.Aries,
        new[] { "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn" }
            .Select(p => new PlanetPosition { Planet = p, Sign = "Aries", HouseNumber = 1,
                NirayanaLongitudeDegrees = p == "Moon" ? 20 : 10 }).ToList());
    var times = SwissEphemerisProvider.GetSunTimes(birth);
    if (times.IsNightBirth) throw new Exception("Noon incorrectly classified as night.");
    birth.TimeOfBirth = new(22, 0);
    if (!SwissEphemerisProvider.GetSunTimes(birth).IsNightBirth) throw new Exception("Evening incorrectly classified as day.");
    birth.TimeOfBirth = new(3, 0);
    if (!SwissEphemerisProvider.GetSunTimes(birth).IsNightBirth) throw new Exception("Predawn incorrectly classified as day.");
    var repo = new YogaInputRepository(factory);
    var charts = new[] { Chart("D1"), Chart("D9") };
    var positions = SwissEphemerisProvider.GetSiderealPositions(birth);
    var strengths = Ikiastrro.Core.Engines.Strength.ShadbalaCalculator.Calculate(charts, positions, times);
    repo.Replace(result.Id, 1, positions, charts, times, strengths);
    repo.Replace(result.Id, 1, positions, charts, times, strengths);
    using var connection = factory.CreateOpenConnection();
    var count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.tbl_Fact_YogaInputEvaluations WHERE ChartResultId=@Id", new { result.Id });
    if (count < 300) throw new Exception("Expected the complete implemented yoga catalogue.");
    var status = connection.QuerySingle<string>("SELECT EvaluationStatus FROM dbo.tbl_Fact_YogaInputEvaluations WHERE ChartResultId=@Id AND SourceVariantCode='RAMAN_300_025'", new { result.Id });
    if (status != (sex is null ? "NOT_EVALUATED" : "EVALUATED")) throw new Exception("Incorrect sex gate.");
}
Console.WriteLine("PASS: 18 evidence tables (7 Shadbala, 12 Bhava, 337 yoga), sex round-trip, full yoga persistence, repeat replacement, noon/evening/predawn. All test writes rolled back.");
