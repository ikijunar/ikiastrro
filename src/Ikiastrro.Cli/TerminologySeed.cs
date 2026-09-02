using System.Data;
using System.Globalization;
using System.Text;
using Dapper;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Karakas;

namespace Ikiastrro.Cli;

/// <summary>One language-neutral concept row, built in code from the enums + Dim tables.</summary>
internal sealed record TermSeed(
    string Category, string Code, string? ParentCode, string EngineCode, int? NumericKey, int DisplayOrder);

/// <summary>One per-language (sa|en) / Latn text row for a concept, keyed by Code.</summary>
internal sealed record TextSeed(
    string Code, string Lang, string Name, string? TraditionalName, string? ShortDescription);

/// <summary>
/// Mechanical generator for the terminology catalogue: every <c>PlanetName</c> / <c>ZodiacName</c> /
/// <c>ConstellationName</c> / <c>CharaKaraka</c> enum value plus the <c>tbl_Dim_*</c> / reference rows
/// become a <see cref="TermSeed"/> with one <c>sa</c> and one <c>en</c> <see cref="TextSeed"/>.
/// Consumed by the <c>seed-terminology</c> CLI mode (idempotent MERGE) and by
/// <see cref="ToBaselineSql"/> which folds the same rows into <c>db/ikiastrro.sql</c>.
/// No classical-source author/title strings appear in any generated prose.
/// </summary>
internal sealed class TerminologySeedData
{
    public List<TermSeed> Terms { get; } = new();
    public List<TextSeed> Texts { get; } = new();

    private int _order;

    private void Add(string category, string code, string? parentCode, string engineCode, int? numericKey,
                     string saName, string enName, string? enDescription)
    {
        _order++;
        Terms.Add(new TermSeed(category, code, parentCode, engineCode, numericKey, _order));
        Texts.Add(new TextSeed(code, "sa", saName, saName, null));
        Texts.Add(new TextSeed(code, "en", enName, null, enDescription));
    }

    public static TerminologySeedData Build(IDbConnection conn)
    {
        var d = new TerminologySeedData();

        // ---- Planet (9): PlanetName enum + tbl_Planets ----
        var planetSa = conn.Query<(string PlanetName, string PlanetNameSanskrit)>(
                "SELECT PlanetName, PlanetNameSanskrit FROM dbo.tbl_Planets")
            .ToDictionary(x => x.PlanetName, x => x.PlanetNameSanskrit, StringComparer.Ordinal);
        foreach (var p in Enum.GetValues<PlanetName>())
        {
            var en = p.ToString();
            var sa = planetSa.GetValueOrDefault(en, en);
            d.Add("Planet", $"PLANET_{en.ToUpperInvariant()}", null, "ASTRO_CALC", (int)p,
                sa, en, $"The graha {en} ({sa}).");
        }

        // ---- Sign (12): ZodiacName enum + tbl_SignAttributes (enum "Capricornus" -> SIGN_CAPRICORN) ----
        var signByEnum = conn.Query<(string ZodiacEnumValue, string SignName, string SignNameSanskrit)>(
                "SELECT ZodiacEnumValue, SignName, SignNameSanskrit FROM dbo.tbl_SignAttributes")
            .ToDictionary(x => x.ZodiacEnumValue, StringComparer.Ordinal);
        foreach (var z in Enum.GetValues<ZodiacName>())
        {
            var enumName = z.ToString();
            var slug = enumName == "Capricornus" ? "CAPRICORN" : enumName.ToUpperInvariant();
            signByEnum.TryGetValue(enumName, out var row);
            var sa = row.SignNameSanskrit ?? enumName;
            var en = row.SignName ?? (enumName == "Capricornus" ? "Capricorn" : enumName);
            d.Add("Sign", $"SIGN_{slug}", null, "ASTRO_CALC", (int)z, sa, en, $"Rasi {(int)z + 1} of 12 ({sa}).");
        }

        // ---- House (12): 1..12 ----
        string[] bhava =
        {
            "Tanu", "Dhana", "Sahaja", "Bandhu", "Putra", "Ari",
            "Yuvati", "Randhra", "Dharma", "Karma", "Labha", "Vyaya"
        };
        string[] houseGloss =
        {
            "Self, body and overall personality.",
            "Wealth, family and speech.",
            "Siblings, courage and initiative.",
            "Home, mother and inner comfort.",
            "Children, intellect and creativity.",
            "Enemies, debt and disease.",
            "Spouse and partnership.",
            "Longevity, upheaval and hidden matters.",
            "Fortune, dharma and father.",
            "Career, status and public action.",
            "Gains, income and social networks.",
            "Loss, expense and liberation."
        };
        for (var h = 1; h <= 12; h++)
            d.Add("House", $"HOUSE_{h:00}", null, "HOUSE", h,
                $"{bhava[h - 1]} Bhava", $"{Ordinal(h)} house", houseGloss[h - 1]);

        // ---- Nakshatra (27) + NakshatraPada (108): ConstellationName enum + tbl_Nakshatras / tbl_NakshatraPadas ----
        var naks = conn.Query<(int Id, string NakshatraName, int SequenceNumber)>(
            "SELECT Id, NakshatraName, SequenceNumber FROM dbo.tbl_Nakshatras ORDER BY Id").ToList();
        var nakSlugById = new Dictionary<int, string>();
        var nakNameById = new Dictionary<int, string>();
        foreach (var n in naks)
        {
            var slug = Slug(n.NakshatraName);
            nakSlugById[n.Id] = slug;
            nakNameById[n.Id] = n.NakshatraName;
            d.Add("Nakshatra", $"NAK_{slug}", null, "NAKSHATRA", n.SequenceNumber - 1,
                n.NakshatraName, n.NakshatraName, $"Nakshatra {n.SequenceNumber} of 27.");
        }
        var padas = conn.Query<(int NakshatraId, int PadaNumber)>(
            "SELECT NakshatraId, PadaNumber FROM dbo.tbl_NakshatraPadas ORDER BY NakshatraId, PadaNumber").ToList();
        foreach (var pd in padas)
        {
            var slug = nakSlugById[pd.NakshatraId];
            var nm = nakNameById[pd.NakshatraId];
            d.Add("NakshatraPada", $"NAKPADA_{slug}_{pd.PadaNumber}", $"NAK_{slug}", "NAKSHATRA", pd.PadaNumber,
                $"{nm} pada {pd.PadaNumber}", $"{nm} pada {pd.PadaNumber}",
                $"Quarter {pd.PadaNumber} of nakshatra {nm}.");
        }

        // ---- DivisionalChart (21): tbl_Dim_ChartType ("D2-US" -> VARGA_D2_US) ----
        var charts = conn.Query<(int Id, string Code, string DisplayName, int DivisionalFactor)>(
            "SELECT Id, Code, DisplayName, DivisionalFactor FROM dbo.tbl_Dim_ChartType ORDER BY Id").ToList();
        foreach (var c in charts)
        {
            var slug = c.Code.Replace("-", "_").ToUpperInvariant();
            d.Add("DivisionalChart", $"VARGA_{slug}", null, "VARGA", c.Id,
                c.DisplayName, $"{c.DisplayName} chart ({c.Code})",
                $"Divisional chart {c.Code}: {c.DivisionalFactor}-part division of each rasi.");
        }

        // ---- Karaka (8): CharaKaraka enum ----
        var karakaSa = new Dictionary<CharaKaraka, string>
        {
            [CharaKaraka.AK] = "Atmakaraka", [CharaKaraka.AmK] = "Amatyakaraka",
            [CharaKaraka.BK] = "Bhratrikaraka", [CharaKaraka.MK] = "Matrikaraka",
            [CharaKaraka.PiK] = "Pitrikaraka", [CharaKaraka.PK] = "Putrakaraka",
            [CharaKaraka.GK] = "Gnatikaraka", [CharaKaraka.DK] = "Darakaraka"
        };
        var karakaEn = new Dictionary<CharaKaraka, string>
        {
            [CharaKaraka.AK] = "Self / soul significator", [CharaKaraka.AmK] = "Career / minister significator",
            [CharaKaraka.BK] = "Siblings significator", [CharaKaraka.MK] = "Mother significator",
            [CharaKaraka.PiK] = "Father significator", [CharaKaraka.PK] = "Children significator",
            [CharaKaraka.GK] = "Kin / rivalry significator", [CharaKaraka.DK] = "Spouse significator"
        };
        foreach (var k in Enum.GetValues<CharaKaraka>())
            d.Add("Karaka", $"KARAKA_{k.ToString().ToUpperInvariant()}", null, "KARAKA", (int)k,
                karakaSa[k], karakaEn[k], $"Jaimini chara karaka, rank {(int)k + 1} of 8 by descending longitude.");

        // ---- SpecialPoint (15): SpecialPointSeed codes AL, A2..A12, HL, Gulika, Maandi ----
        d.Add("SpecialPoint", "SPT_AL", null, "KARAKA", null, "Arudha Lagna", "Arudha Lagna",
            "Sign-image of the ascendant (Jaimini pada of the 1st house).");
        for (var n = 2; n <= 12; n++)
            d.Add("SpecialPoint", $"SPT_A{n}", null, "KARAKA", null,
                $"Bhava Arudha {n}", $"Arudha of house {n}", $"Jaimini pada (arudha) of house {n}.");
        d.Add("SpecialPoint", "SPT_HL", null, "KARAKA", null, "Hora Lagna", "Hora Lagna",
            "Time-based lagna advancing one sign per hora from sunrise.");
        d.Add("SpecialPoint", "SPT_GULIKA", null, "KARAKA", null, "Gulika", "Gulika",
            "Upagraha marking the start of the Saturn sub-part of the day or night arc.");
        d.Add("SpecialPoint", "SPT_MAANDI", null, "KARAKA", null, "Maandi", "Maandi",
            "Upagraha tied to the Saturn sub-division of the day or night arc.");

        // ---- AvasthaState (8): tbl_Dim_PlanetaryState ----
        var avasthaEn = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Baala"] = "Infant", ["Kumara"] = "Child", ["Yuva"] = "Youth",
            ["Vriddha"] = "Old", ["Mrita"] = "Dead",
            ["Jagrat"] = "Awake", ["Swapna"] = "Dreaming", ["Sushupti"] = "Sleeping"
        };
        var states = conn.Query<(int Id, string AvasthaSystem, string StateName, string? Meaning)>(
            "SELECT Id, AvasthaSystem, StateName, Meaning FROM dbo.tbl_Dim_PlanetaryState ORDER BY Id").ToList();
        foreach (var s in states)
            d.Add("AvasthaState",
                $"AVASTHA_{s.AvasthaSystem.ToUpperInvariant()}_{s.StateName.ToUpperInvariant()}",
                null, "AVASTHA", s.Id,
                s.StateName, avasthaEn.GetValueOrDefault(s.StateName, s.StateName), s.Meaning);

        // ---- DignityState (9): distinct tbl_Chart_KeyDetails.DignityStatus ----
        var dignityRank = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Exalted"] = 1, ["Moolatrikona"] = 2, ["Own Sign"] = 3, ["Great Friend"] = 4,
            ["Friend"] = 5, ["Neutral"] = 6, ["Enemy"] = 7, ["Great Enemy"] = 8, ["Debilitated"] = 9
        };
        var dignitySa = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Exalted"] = "Uccha", ["Moolatrikona"] = "Moolatrikona", ["Own Sign"] = "Swakshetra",
            ["Great Friend"] = "Adhimitra", ["Friend"] = "Mitra", ["Neutral"] = "Sama",
            ["Enemy"] = "Shatru", ["Great Enemy"] = "Adhishatru", ["Debilitated"] = "Neecha"
        };
        var dignities = conn.Query<string>(
                "SELECT DISTINCT DignityStatus FROM dbo.tbl_Chart_KeyDetails WHERE DignityStatus IS NOT NULL")
            .OrderBy(x => dignityRank.GetValueOrDefault(x, 99)).ToList();
        foreach (var dg in dignities)
            d.Add("DignityState", $"DIGNITY_{dg.ToUpperInvariant().Replace(" ", "_")}", null, "DIGNITY",
                dignityRank.GetValueOrDefault(dg, 99), dignitySa.GetValueOrDefault(dg, dg), dg,
                $"Planetary dignity: {dg}.");

        // ---- Relationship (6) ----
        (string Code, string Sa, string En, string Desc)[] rels =
        {
            ("REL_YUTI", "Yuti", "Conjunction", "Two grahas occupying the same sign."),
            ("REL_DRISHTI", "Drishti", "Aspect", "A graha casting its glance onto another sign or graha."),
            ("REL_COMBUST", "Asta", "Combustion", "A graha too close to the Sun to give its results."),
            ("REL_GREAT_FRIEND", "Adhimitra", "Great friend", "Compound (natural plus temporal) friendship."),
            ("REL_FRIEND", "Mitra", "Friend", "Natural friendship between two grahas."),
            ("REL_ENEMY", "Shatru", "Enemy", "Natural enmity between two grahas.")
        };
        foreach (var r in rels)
            d.Add("Relationship", r.Code, null, "RELATIONSHIP", null, r.Sa, r.En, r.Desc);

        // ---- Ayanamsa (1) ----
        d.Add("Ayanamsa", "AYANAMSA_LAHIRI", null, "ASTRO_CALC", 1, "Lahiri", "Lahiri ayanamsa",
            "Chitrapaksha sidereal zero-point; the default ayanamsa for this project.");

        return d;
    }

    private static string Ordinal(int n) => n switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"{n}th"
    };

    private static string Slug(string s) => s.ToUpperInvariant().Replace(" ", "_").Replace("-", "_");

    /// <summary>Walks up from the cwd / app base dir to the folder holding <c>Ikiastrro.slnx</c>.</summary>
    public static string FindRepoRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Ikiastrro.slnx"))) return dir.FullName;
                dir = dir.Parent;
            }
        }
        throw new InvalidOperationException("Could not locate repo root (Ikiastrro.slnx).");
    }

    /// <summary>
    /// Emits the same rows as idempotent <c>MERGE</c> statements for <c>db/ikiastrro.sql</c>:
    /// two terminology MERGEs (base rows, then NakshatraPada rows so their self-FK parent exists)
    /// and one text MERGE that joins the VALUES back to <c>Code</c> for the IDENTITY <c>TerminologyId</c>.
    /// </summary>
    public string ToBaselineSql(string dumpNote)
    {
        static string A(string? v) => v is null ? "NULL" : "'" + v.Replace("'", "''") + "'";
        static string U(string? v) => v is null ? "NULL" : "N'" + v.Replace("'", "''") + "'";
        static string I(int? v) => v?.ToString(CultureInfo.InvariantCulture) ?? "NULL";

        var sb = new StringBuilder();
        sb.AppendLine("-- >>> BEGIN TERMINOLOGY SEED (generated - do not hand-edit) >>>");
        sb.AppendLine("-- " + dumpNote);

        var baseTerms = Terms.Where(t => t.Category != "NakshatraPada").ToList();
        var padaTerms = Terms.Where(t => t.Category == "NakshatraPada").ToList();

        sb.AppendLine("MERGE dbo.tbl_Astro_Terminology AS tgt");
        sb.AppendLine("USING (VALUES");
        for (var i = 0; i < baseTerms.Count; i++)
        {
            var t = baseTerms[i];
            sb.Append($"  ({A(t.Category)},{A(t.Code)},{A(t.EngineCode)},{I(t.NumericKey)},{t.DisplayOrder})");
            sb.AppendLine(i == baseTerms.Count - 1 ? "" : ",");
        }
        sb.AppendLine(") AS src (Category, Code, EngineCode, NumericKey, DisplayOrder)");
        sb.AppendLine("ON tgt.Code = src.Code");
        sb.AppendLine("WHEN MATCHED THEN UPDATE SET Category = src.Category, EngineCode = src.EngineCode,");
        sb.AppendLine("    NumericKey = src.NumericKey, DisplayOrder = src.DisplayOrder, IsActive = 1");
        sb.AppendLine("WHEN NOT MATCHED THEN INSERT (Category, Code, EngineCode, NumericKey, DisplayOrder, IsActive)");
        sb.AppendLine("    VALUES (src.Category, src.Code, src.EngineCode, src.NumericKey, src.DisplayOrder, 1);");
        sb.AppendLine("GO");

        sb.AppendLine("MERGE dbo.tbl_Astro_Terminology AS tgt");
        sb.AppendLine("USING (VALUES");
        for (var i = 0; i < padaTerms.Count; i++)
        {
            var t = padaTerms[i];
            sb.Append($"  ({A(t.Category)},{A(t.Code)},{A(t.ParentCode)},{A(t.EngineCode)},{I(t.NumericKey)},{t.DisplayOrder})");
            sb.AppendLine(i == padaTerms.Count - 1 ? "" : ",");
        }
        sb.AppendLine(") AS src (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder)");
        sb.AppendLine("ON tgt.Code = src.Code");
        sb.AppendLine("WHEN MATCHED THEN UPDATE SET Category = src.Category, ParentCode = src.ParentCode,");
        sb.AppendLine("    EngineCode = src.EngineCode, NumericKey = src.NumericKey, DisplayOrder = src.DisplayOrder, IsActive = 1");
        sb.AppendLine("WHEN NOT MATCHED THEN INSERT (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder, IsActive)");
        sb.AppendLine("    VALUES (src.Category, src.Code, src.ParentCode, src.EngineCode, src.NumericKey, src.DisplayOrder, 1);");
        sb.AppendLine("GO");

        sb.AppendLine("MERGE dbo.tbl_Astro_TerminologyText AS tgt");
        sb.AppendLine("USING (");
        sb.AppendLine("  SELECT t.TerminologyId, v.LanguageCode, v.Script, v.Name, v.TraditionalName, v.ShortDescription");
        sb.AppendLine("  FROM (VALUES");
        for (var i = 0; i < Texts.Count; i++)
        {
            var x = Texts[i];
            sb.Append($"   ({A(x.Code)},{A(x.Lang)},'Latn',{U(x.Name)},{U(x.TraditionalName)},{U(x.ShortDescription)})");
            sb.AppendLine(i == Texts.Count - 1 ? "" : ",");
        }
        sb.AppendLine("  ) AS v (Code, LanguageCode, Script, Name, TraditionalName, ShortDescription)");
        sb.AppendLine("  JOIN dbo.tbl_Astro_Terminology t ON t.Code = v.Code");
        sb.AppendLine(") AS src");
        sb.AppendLine("ON tgt.TerminologyId = src.TerminologyId AND tgt.LanguageCode = src.LanguageCode AND tgt.Script = src.Script");
        sb.AppendLine("WHEN MATCHED THEN UPDATE SET Name = src.Name, TraditionalName = src.TraditionalName, ShortDescription = src.ShortDescription");
        sb.AppendLine("WHEN NOT MATCHED THEN INSERT (TerminologyId, LanguageCode, Script, Name, TraditionalName, ShortDescription)");
        sb.AppendLine("    VALUES (src.TerminologyId, src.LanguageCode, src.Script, src.Name, src.TraditionalName, src.ShortDescription);");
        sb.AppendLine("GO");
        sb.AppendLine("-- <<< END TERMINOLOGY SEED <<<");
        return sb.ToString();
    }
}
