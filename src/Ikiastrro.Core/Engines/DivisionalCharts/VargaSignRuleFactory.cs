namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>
/// Builds the IVargaSignRule for a tbl_Rule_VargaScheme.SignRuleKey. Pure - no
/// I/O. The single switch here is the contract with the seed data: every
/// SignRuleKey the migration inserts must have a case.
/// </summary>
public static class VargaSignRuleFactory
{
    public static IVargaSignRule For(string signRuleKey, int divisionFactor) => signRuleKey switch
    {
        "HoraD2Classic"    => new HoraD2ClassicSignRule(),
        "HoraD2UmaShambu"  => new HoraD2UmaShambuSignRule(),
        "DrekkanaD3"       => new LinearVargaSignRule(3, 4),
        "ChaturthamsaD4"   => new LinearVargaSignRule(4, 3),
        "PanchamsaD5"      => new PanchamsaD5SignRule(),
        "ShashtamsaD6"     => new ShashtamsaD6SignRule(),
        "SaptamsaD7"       => new SaptamsaD7SignRule(),
        "AshtamsaD8"       => new AshtamsaD8SignRule(),
        "NavamsaD9"        => new NavamsaD9SignRule(),
        "DasamsaD10"       => new DasamsaD10SignRule(),
        "RudramsaD11"      => new RudramsaD11SignRule(),
        "DwadasamsaD12"    => new LinearVargaSignRule(12, 1),
        "ShodasamsaD16"    => new ShodasamsaD16SignRule(),
        "VimsamsaD20"      => new VimsamsaD20SignRule(),
        "SiddhamsaD24"     => new SiddhamsaD24SignRule(),
        "NakshatramsaD27"  => new NakshatramsaD27SignRule(),
        "TrimsamsaD30"     => new TrimsamsaD30SignRule(),
        "KhavedamsaD40"    => new KhavedamsaD40SignRule(),
        "AkshavedamsaD45"  => new AkshavedamsaD45SignRule(),
        "ShashtyamsaD60"   => new LinearVargaSignRule(60, 1),
        _ => throw new InvalidOperationException(
            $"No IVargaSignRule for SignRuleKey '{signRuleKey}' (factor {divisionFactor}). " +
            "Add a case in VargaSignRuleFactory or fix the tbl_Rule_VargaScheme seed."),
    };
}
