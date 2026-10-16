using System.Text;
using MudBlazor.Services;
using MudBlazor;
using Ikiastrro.Core.Pipeline;
using Ikiastrro.Core.Geocoding;
using Ikiastrro.Data;
using Ikiastrro.Web.Components;
using Ikiastrro.Web.Components.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// MudBlazor registers popover, dialog, snackbar, and related UI services.
builder.Services.AddMudServices();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- ikiastrro services (same components the CLI uses directly) ---
builder.Services.AddSingleton(
    SqlConnectionFactory.Create(builder.Configuration.GetConnectionString("Ikiastrro")));
builder.Services.AddScoped<BirthDetailsRepository>();
builder.Services.AddScoped<ChartResultsRepository>();
builder.Services.AddScoped<ChartKeyDetailsRepository>();
builder.Services.AddScoped<ChartHouseLordsRepository>();
builder.Services.AddScoped<ChartHouseLordInterpretationRepository>();
builder.Services.AddScoped<ChartConjunctionsRepository>();
builder.Services.AddScoped<ChartMultiGrahaConjunctionRepository>();
builder.Services.AddScoped<ChartAspectsRepository>();
builder.Services.AddScoped<ChartMoonContextRepository>();
builder.Services.AddScoped<DashaPeriodsRepository>();
builder.Services.AddScoped<SadeSatiRepository>();
builder.Services.AddScoped<PlanetaryStateRuleRepository>();
builder.Services.AddScoped<PlanetaryStateRepository>();
builder.Services.AddScoped<PostureStateInterpretationRepository>();
builder.Services.AddScoped<PlanetaryStrengthRepository>();
builder.Services.AddScoped<BhavaStrengthRepository>();
builder.Services.AddScoped<AshtakavargaRepository>();
builder.Services.AddScoped<VargottamaRepository>();
builder.Services.AddScoped<KpSubLordChainRepository>();
builder.Services.AddScoped<ArgalaFactRepository>();
builder.Services.AddScoped<Ikiastrro.Data.Statistics.HouseStrengthStatisticsRepository>();
builder.Services.AddScoped<Ikiastrro.Data.Statistics.HouseStrengthStatisticsService>();
builder.Services.AddScoped<Ikiastrro.Data.Statistics.PopulationEvidenceRepository>();
builder.Services.AddScoped<Ikiastrro.Data.Statistics.LifeMatterPopulationEvidenceRepository>();
builder.Services.AddScoped<Ikiastrro.Data.Statistics.DashaMatterPopulationEvidenceRepository>();
builder.Services.AddScoped<Ikiastrro.Data.Statistics.AnalyticsSubjectRepository>();
builder.Services.AddScoped<GrahaDrishtiStrengthRepository>();
builder.Services.AddScoped<PanchangaRepository>();
builder.Services.AddScoped<MatchPersonRepository>();
builder.Services.AddScoped<DoshaChartRepository>();
builder.Services.AddScoped<NavamsaChartRepository>();
builder.Services.AddScoped<PairHistoryRepository>();
builder.Services.AddScoped<DashaCompatibilityRepository>();
builder.Services.AddScoped<FamilyRepository>();
builder.Services.AddScoped<NameOptionRepository>();
builder.Services.AddScoped<YogaInputRepository>();
builder.Services.AddScoped<AmsabalaRepository>();
builder.Services.AddScoped<AmsabalaSchemeRepository>();
builder.Services.AddScoped<TransitStrengthRuleRepository>();
builder.Services.AddScoped<BeneficMaleficSchoolRepository>();
builder.Services.AddScoped<YogaEvaluationRepository>();
builder.Services.AddScoped<FeatureCompareRepository>();
builder.Services.AddScoped<ArgalaRuleRepository>();
builder.Services.AddScoped<RasiNakshatraCombinationRepository>();
builder.Services.AddScoped<NakshatraReferenceRepository>();
builder.Services.AddScoped<InterpretationRepository>();
builder.Services.AddScoped<AstrologerEvidenceRepository>();
builder.Services.AddScoped<Natal_Transit_Comp_WheelRepository>();
builder.Services.AddScoped<PlanetSignTransitEventsRepository>();
builder.Services.AddScoped<GocharaRepository>();
builder.Services.AddScoped<RuleSetRepository>();
builder.Services.AddScoped<AyanamsaRuleRepository>();
builder.Services.AddScoped<ChartTypeRepository>();
builder.Services.AddScoped<LifeAreaReferenceRepository>();
builder.Services.AddScoped<LifeMatterReferenceRepository>();
builder.Services.AddScoped<DivisionalSubjectRepository>();
builder.Services.AddScoped<LifeMatterFocusRepository>();
builder.Services.AddScoped<Ikiastrro.Web.Components.LifeMatters.MarriageMattersReader>();
builder.Services.AddScoped<NaisargikaKarakaRepository>();
builder.Services.AddScoped<Ikiastrro.Data.Statistics.LifeMatterPromiseService>();
builder.Services.AddScoped<Ikiastrro.Data.Statistics.LifeMatterTimingDataService>();
builder.Services.AddScoped<KarakaMatterRepository>();
builder.Services.AddScoped<VargaSchemeRepository>();
builder.Services.AddScoped<SubPlanetRuleRepository>();
builder.Services.AddScoped<VimshottariDashaService>();
builder.Services.AddScoped<ChartGenerationService>();
builder.Services.AddScoped<BirthDetailDeletionService>();
builder.Services.AddScoped<BirthDetailsCsvService>();
builder.Services.AddScoped<JkdInterchangeService>();
builder.Services.AddScoped<DatabaseMaintenanceService>();
builder.Services.AddScoped<IPlaceResolver>(_ => new KnownPlaceResolver(new NominatimPlaceResolver()));

// v2 shell — the person currently opened; read by MainLayout for the header tabs + band.
builder.Services.AddScoped<Ikiastrro.Web.Components.ActivePerson>();
builder.Services.AddScoped(sp =>
{
    var schemes = sp.GetRequiredService<VargaSchemeRepository>().GetAll(1);
    return ChartCalculationOrchestrator.CreateDefault(schemes,
        sp.GetRequiredService<SubPlanetRuleRepository>().GetAll(sp.GetRequiredService<RuleSetRepository>().GetActive().Id));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// --- Saved-people export downloads (docs/ui/components/saved-people.md) ---
// Plain GET endpoints, not Blazor components: a Blazor Server circuit has no filesystem access
// on the client to save to, so the browser's own "download this URL" behavior does the work
// instead of JS interop. CSV is generated in-process; .JKD shells out (JkdInterchangeService)
// to a throwaway temp file that's streamed back and deleted once the response is sent.
app.MapGet("/export/people.csv", (BirthDetailsRepository birthDetailsRepo, BirthDetailsCsvService csvService) =>
{
    var csv = csvService.ExportCsv(birthDetailsRepo.GetAll());
    var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv);
    return Results.File(bytes, "text/csv", $"ikiastrro-saved-people-{DateTime.Now:yyyy-MM-dd}.csv");
});

app.MapGet("/export/people.jkd", async (JkdInterchangeService jkdService) =>
{
    var tempPath = Path.Combine(Path.GetTempPath(), $"ikiastrro-export-{Guid.NewGuid():N}.JKD");
    try
    {
        await jkdService.ExportAsync(tempPath);
        var bytes = await File.ReadAllBytesAsync(tempPath);
        return Results.File(bytes, "application/octet-stream", $"ikiastrro-saved-people-{DateTime.Now:yyyy-MM-dd}.JKD");
    }
    finally
    {
        if (File.Exists(tempPath)) File.Delete(tempPath);
    }
});

app.Run();
