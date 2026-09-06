namespace Ikiastrro.Web.Components.Shared;

/// <summary>Session-level reading preferences shared by the start page and chart surfaces.</summary>
public sealed class ReadingProfileState
{
    public string Theme { get; set; } = "orange";
    public string DefaultChart { get; set; } = "D1";
    public bool TechnicalMode { get; set; }
    public bool ShowAspects { get; set; }
    public bool ShowPlanetaryDignity { get; set; } = true;
    public bool ShowDispositorRelations { get; set; } = true;
    public bool ShowHouseLordCombinations { get; set; } = true;
    public bool ShowDivisionalConfirmation { get; set; } = true;

    public string Summary => $"{(Theme == "nocturne" ? "Nocturne" : "Orange")} · {DefaultChart} · {(TechnicalMode ? "Technical" : "Clean")}";
}
