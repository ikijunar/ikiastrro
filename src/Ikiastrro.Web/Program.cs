using Ikiastrro.Core.Pipeline;
using Ikiastrro.Core.Geocoding;
using Ikiastrro.Data;
using Ikiastrro.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- ikiastrro services (same components the CLI uses directly) ---
builder.Services.AddSingleton(
    SqlConnectionFactory.Create(builder.Configuration.GetConnectionString("Ikiastrro")));
builder.Services.AddScoped<BirthDetailsRepository>();
builder.Services.AddScoped<ChartResultsRepository>();
builder.Services.AddScoped<ChartKeyDetailsRepository>();
builder.Services.AddScoped<ChartHouseLordsRepository>();
builder.Services.AddScoped<ChartConjunctionsRepository>();
builder.Services.AddScoped<ChartAspectsRepository>();
builder.Services.AddScoped<DashaPeriodsRepository>();
builder.Services.AddScoped<SadeSatiRepository>();
builder.Services.AddScoped<PlanetaryStateRuleRepository>();
builder.Services.AddScoped<PlanetaryStateRepository>();
builder.Services.AddScoped<PlanetSignTransitEventsRepository>();
builder.Services.AddScoped<GocharaRepository>();
builder.Services.AddScoped<RuleSetRepository>();
builder.Services.AddScoped<ChartTypeRepository>();
builder.Services.AddScoped<VargaSchemeRepository>();
builder.Services.AddScoped<VimshottariDashaService>();
builder.Services.AddScoped<ChartGenerationService>();
builder.Services.AddScoped<BirthDetailDeletionService>();
builder.Services.AddScoped<IPlaceResolver, NominatimPlaceResolver>();
builder.Services.AddScoped(sp =>
{
    var schemes = sp.GetRequiredService<VargaSchemeRepository>().GetAll(1);
    return ChartCalculationOrchestrator.CreateDefault(schemes);
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

app.Run();
