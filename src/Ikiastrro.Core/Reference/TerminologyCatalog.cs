namespace Ikiastrro.Core.Reference;

/// <summary>
/// Loaded-once, cached lookup over <c>dbo.tbl_Astro_Terminology</c> (+ <c>_Text</c>). Default
/// language is <c>"sa"</c> (romanized Sanskrit), falling back to <c>"en"</c> and finally the Code.
/// </summary>
public sealed class TerminologyCatalog
{
    private readonly IReadOnlyDictionary<string, TerminologyRow> _byCode;
    private readonly IReadOnlyDictionary<(string Code, string Lang), TerminologyTextRow> _text;

    public TerminologyCatalog(IEnumerable<TerminologyRow> rows, IEnumerable<TerminologyTextRow> text)
    {
        var byCode = rows.ToDictionary(r => r.Code, StringComparer.Ordinal);
        _byCode = byCode;

        // id -> code map, so the per-language text rows can be keyed by Code.
        var idToCode = byCode.Values.ToDictionary(r => r.TerminologyId, r => r.Code);

        var map = new Dictionary<(string, string), TerminologyTextRow>();
        foreach (var t in text)
            if (idToCode.TryGetValue(t.TerminologyId, out var code))
                map[(code, t.LanguageCode)] = t; // one Latn row per (code, lang) in practice
        _text = map;
    }

    /// <summary>Display name in <paramref name="lang"/>, then <c>en</c>, then the code itself.</summary>
    public string Label(string code, string lang = "sa") =>
        _text.TryGetValue((code, lang), out var t) ? t.Name
        : _text.TryGetValue((code, "en"), out var en) ? en.Name
        : code;

    /// <summary>The classical/traditional name in <paramref name="lang"/>, if recorded.</summary>
    public string? Traditional(string code, string lang = "sa") =>
        _text.GetValueOrDefault((code, lang))?.TraditionalName;

    /// <summary>Short description in <paramref name="lang"/>, falling back to <c>en</c>.</summary>
    public string? Describe(string code, string lang = "sa") =>
        _text.GetValueOrDefault((code, lang))?.ShortDescription
        ?? _text.GetValueOrDefault((code, "en"))?.ShortDescription;

    /// <summary>The language-neutral concept row, or <c>null</c> if the Code is unknown.</summary>
    public TerminologyRow? Meta(string code) => _byCode.GetValueOrDefault(code);
}
