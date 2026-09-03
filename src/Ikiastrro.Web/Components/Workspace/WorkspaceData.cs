using Ikiastrro.Core.Models;
using Ikiastrro.Data;

namespace Ikiastrro.Web.Components.Workspace;

public sealed record LoadedChart(
    string ChartType, string Label, string SanskritName, string? VargaMethod,
    string AscendantSign, string? MoonSign, string? MoonNakshatra,
    IReadOnlyList<ChartKeyDetail> KeyDetails,
    IReadOnlyList<ChartHouseLord> HouseLords,
    IReadOnlyList<ChartAspect> Aspects,
    IReadOnlyList<ChartConjunction> Conjunctions,
    double? AyanamshaDegrees, double? SiderealTimeHours, string EngineVersion)
{
    public IReadOnlyList<ChartKeyDetail> Grahas => KeyDetails.Where(k => k.PointKind == "Graha").ToList();
    public IReadOnlyList<ChartKeyDetail> SpecialPoints => KeyDetails.Where(k => k.PointKind != "Graha").ToList();
}

public sealed class WorkspaceData
{
    public required BirthDetails Person { get; init; }
    public required IReadOnlyDictionary<string, LoadedChart> Charts { get; init; }
    public bool HasAnyChart => Charts.Count > 0;

    public static WorkspaceData? Load(
        int id,
        BirthDetailsRepository people,
        ChartResultsRepository results,
        ChartKeyDetailsRepository keyDetails,
        ChartHouseLordsRepository houseLords,
        ChartAspectsRepository aspects,
        ChartConjunctionsRepository conjunctions,
        IReadOnlyList<ChartTypeRow> chartTypes)
    {
        var person = people.GetById(id);
        if (person is null) return null;

        var headers = results.GetByBirthDetailId(id)
            .Where(r => r.CalculationKind == "PositionChart")
            .ToList();
        var kdByChart = keyDetails.GetByBirthDetailId(id).GroupBy(k => k.ChartResultId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ChartKeyDetail>)g.ToList());
        var hlByChart = houseLords.GetByBirthDetailId(id).GroupBy(h => h.ChartResultId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ChartHouseLord>)g.ToList());
        var asByChart = aspects.GetByBirthDetailId(id).GroupBy(a => a.ChartResultId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ChartAspect>)g.ToList());
        var cjByChart = conjunctions.GetByBirthDetailId(id).GroupBy(c => c.ChartResultId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ChartConjunction>)g.ToList());
        var sanskritByCode = chartTypes.ToDictionary(t => t.Code, t => t.DisplayName, StringComparer.OrdinalIgnoreCase);

        var empty = Array.Empty<ChartKeyDetail>();
        var charts = new Dictionary<string, LoadedChart>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in headers)
        {
            var kd = kdByChart.GetValueOrDefault(h.Id, empty);
            var asc = kd.FirstOrDefault(k => k.Planet == "Ascendant")?.Sign;
            if (asc is null) continue;
            var moon = kd.FirstOrDefault(k => k.Planet == "Moon");
            charts[h.ChartType] = new LoadedChart(
                h.ChartType,
                sanskritByCode.TryGetValue(h.ChartType, out var sn) ? $"{sn} · {h.ChartType}" : h.ChartType,
                sanskritByCode.GetValueOrDefault(h.ChartType, h.ChartType),
                h.VargaMethod,
                asc, moon?.Sign, moon?.Nakshatra,
                kd,
                hlByChart.GetValueOrDefault(h.Id, Array.Empty<ChartHouseLord>()),
                asByChart.GetValueOrDefault(h.Id, Array.Empty<ChartAspect>()),
                cjByChart.GetValueOrDefault(h.Id, Array.Empty<ChartConjunction>()),
                h.AyanamshaDegrees, h.SiderealTimeHours, h.EngineVersion);
        }

        return new WorkspaceData { Person = person, Charts = charts };
    }
}
