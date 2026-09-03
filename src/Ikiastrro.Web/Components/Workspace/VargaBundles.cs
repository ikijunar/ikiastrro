namespace Ikiastrro.Web.Components.Workspace;

public static class VargaBundles
{
    // Source: docs/reference-chart-varga-index.md "Groups:" annotations + classical Parashari
    // Shodashavarga membership. D9/D10/D60 not in that doc's rows (they have full guides):
    // D9 -> 6·7·10·16, D10 -> 10·16, D60 -> 6·7·10·16 (classical).
    public static readonly IReadOnlyList<(string Title, IReadOnlyList<string> Codes)> Groups = new[]
    {
        ("Shadvarga (6)",       (IReadOnlyList<string>)new[] { "D1","D2-US","D3","D9","D12","D30" }),
        ("Saptavarga (7)",      new[] { "D7" }),
        ("Dasavarga (10)",      new[] { "D10","D16","D60" }),
        ("Shodasavarga (16)",   new[] { "D4","D20","D24","D27","D40","D45" }),
        ("Extra vargas",        new[] { "D2","D5","D6","D8","D11" }),
    };
    // Rail order = the sequence a prev/next in VargaView walks. Groups top-to-bottom, codes in listed order.
    public static readonly IReadOnlyList<string> RailOrder =
        Groups.SelectMany(g => g.Codes).ToList();
}
