namespace Ikiastrro.Core.Engines.Yoga;

/// <summary>
/// Source ledger for Raman 201–300. These rows deliberately remain unevaluated until each
/// predicate, qualification and required chart is visually verified and implemented.
/// </summary>
public static class RamanFinalHundredCatalog
{
    private sealed record Group(int Start, int End, string YogaCode, int ScanPage);
    private static readonly Group[] Groups =
    [
        new(201,201,"YOGA_SAHODAREE_SANGAMA",204),new(202,204,"YOGA_KAPATA",204),
        new(205,206,"YOGA_NISHKAPATA",206),new(207,207,"YOGA_MATRU_SATRUTWA",207),
        new(208,208,"YOGA_MATRU_SNEHA",207),new(209,210,"YOGA_VAHANA",208),
        new(211,211,"YOGA_ANAPATHYA",209),new(212,215,"YOGA_SARPASAPA",210),
        new(216,216,"YOGA_PITRUSAPA_SUTAKSHAYA",214),new(217,217,"YOGA_MATRUSAPA_SUTAKSHAYA",218),
        new(218,218,"YOGA_BHRATRUSAPA_SUTAKSHAYA",219),new(219,219,"YOGA_PRETASAPA",220),
        new(220,221,"YOGA_BAHUPUTRA",222),new(222,223,"YOGA_DATTAPUTRA",224),
        new(224,224,"YOGA_APUTRA",225),new(225,225,"YOGA_EKAPUTRA",226),
        new(226,226,"YOGA_SUPUTRA",227),new(227,228,"YOGA_KALANIRDESAT_PUTRA",228),
        new(229,230,"YOGA_KALANIRDESAT_PUTRANASA",228),new(231,231,"YOGA_BUDDHIMATURYA",230),
        new(232,232,"YOGA_THEEVRABUDDHI",233),new(233,233,"YOGA_BUDDHI_JADA",234),
        new(234,234,"YOGA_THRIKALAGNANA",236),new(235,235,"YOGA_PUTRA_SUKHA",238),
        new(236,236,"YOGA_JARA",238),new(237,237,"YOGA_JARAJA_PUTRA",240),
        new(238,238,"YOGA_BAHU_STREE",240),new(239,239,"YOGA_SATKALATRA",242),
        new(240,240,"YOGA_BHAGA_CHUMBANA",244),new(241,241,"YOGA_BHAGYA",244),
        new(242,242,"YOGA_JANANAT_PURVAM_PITRU_MARANA",245),new(243,243,"YOGA_DHATRUTWA",246),
        new(244,244,"YOGA_APAKEERTI",248),new(245,263,"YOGA_RAJA",249),
        new(264,264,"YOGA_GALAKARNA",272),new(265,265,"YOGA_VRANA",272),
        new(266,266,"YOGA_SISNAVYADHI",275),new(267,267,"YOGA_KALATRASHANDA",277),
        new(268,269,"YOGA_KUSHTAROGA",277),new(270,270,"YOGA_KSHAYAROGA",279),
        new(271,271,"YOGA_BANDHANA",286),new(272,272,"YOGA_KARASCHEDA",286),
        new(273,273,"YOGA_SIRACHCHEDA",287),new(274,274,"YOGA_DURMARANA",289),
        new(275,275,"YOGA_YUDDHE_MARANA",290),new(276,277,"YOGA_SANGHATAKA_MARANA",291),
        new(278,278,"YOGA_PEENASAROGA",292),new(279,279,"YOGA_PITTAROGA",293),
        new(280,280,"YOGA_VIKALANGA_PATNI",294),new(281,281,"YOGA_PUTRA_KALATRA_HEENA",295),
        new(282,282,"YOGA_BHARYASAHA_VYABHICHARA",295),new(283,283,"YOGA_VAMSACHEDA",297),
        new(284,284,"YOGA_GUHYAROGA",298),new(285,285,"YOGA_ANGAHEENA",299),
        new(286,286,"YOGA_SWETAKUSHTA",300),new(287,287,"YOGA_PISACHA_GRASTHA",301),
        new(288,289,"YOGA_ANDHA",302),new(290,290,"YOGA_VATHAROGA",303),
        new(291,294,"YOGA_MATIBHRAMANA",305),new(295,295,"YOGA_KHALWATA",312),
        new(296,296,"YOGA_NISHTURABHASHI",313),new(297,297,"YOGA_RAJABHRASHTA",313),
        new(298,299,"YOGA_RAJA_BHANGA",314),new(300,300,"YOGA_GOHANTA",316)
    ];

    public static IReadOnlyList<ContextualYogaResult> Entries()
        => Groups.SelectMany(group => Enumerable.Range(group.Start, group.End - group.Start + 1)
            .Select(number => new ContextualYogaResult(
                group.YogaCode, null, "NOT_EVALUATED", "SRC_RAMAN_300_COMBINATIONS",
                $"RAMAN_300_{number:000}",
                $"combination {number}; printed p.{group.ScanPage - 12}; scan p.{group.ScanPage}",
                "Source entry catalogued; predicate, qualifications and chart requirements await visual verification.")))
            .ToList();
}
