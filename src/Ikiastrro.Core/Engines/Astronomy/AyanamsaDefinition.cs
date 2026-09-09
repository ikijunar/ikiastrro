namespace Ikiastrro.Core.Engines.Astronomy;

/// <summary>
/// A named zodiac reference system exposed by JHora's calculation preferences.
/// Swiss Ephemeris supplies the numeric implementation for entries with a
/// <see cref="SwissSiderealMode"/>. Entries without one remain visible in the
/// catalogue but fail explicitly until their custom fixed-star formula is added.
/// </summary>
public sealed record AyanamsaDefinition(
    string Code,
    string DisplayName,
    int? SwissSiderealMode,
    bool IsTropical = false,
    double CorrectionDegrees = 0,
    string CorrectionDirection = "Subtract")
{
    public bool IsImplemented => IsTropical || SwissSiderealMode.HasValue;

    public static readonly AyanamsaDefinition TrueLahiri =
        new("AYANAMSA_TRUE_LAHIRI", "True Lahiri/Chitrapaksha", 27);
    public static readonly AyanamsaDefinition TraditionalLahiri =
        new("AYANAMSA_LAHIRI", "Traditional Lahiri", 1);
    public static readonly AyanamsaDefinition PushyaPaksha =
        new("AYANAMSA_PUSHYA_PAKSHA", "Pushya-paksha ayanamsa", 29);
    public static readonly AyanamsaDefinition Raman =
        new("AYANAMSA_RAMAN", "Raman", 3);
    public static readonly AyanamsaDefinition Krishnamoorthy =
        new("AYANAMSA_KP", "Krishnamoorthy (KP)", 5);
    public static readonly AyanamsaDefinition FixedStarCustom =
        new("AYANAMSA_FIXED_STAR_CUSTOM", "Fixed star based CUSTOM ayanamsa", null);
    public static readonly AyanamsaDefinition Jagannatha =
        new("AYANAMSA_JAGANNATHA", "Jagannatha (Spica in the middle of Chitra always, fixed solar rotation plane)", 26);
    public static readonly AyanamsaDefinition RohiniPaksha =
        new("AYANAMSA_ROHINI_PAKSHA", "Rohini-paksha ayanamsa", null);
    public static readonly AyanamsaDefinition SriSuryaSiddhanta =
        new("AYANAMSA_SRI_SURYA_SIDDHANTA", "Sri Surya Siddhanta", 21);
    public static readonly AyanamsaDefinition DevaDatta =
        new("AYANAMSA_DEVA_DATTA", "Deva-datta", null);
    public static readonly AyanamsaDefinition UshaShashi =
        new("AYANAMSA_USHA_SHASHI", "Usha-Shashi", 4);
    public static readonly AyanamsaDefinition Yukteshwar =
        new("AYANAMSA_YUKTESHWAR", "Yukteshwar", 7);
    public static readonly AyanamsaDefinition JnBhasin =
        new("AYANAMSA_JN_BHASIN", "JN Bhasin", 8);
    public static readonly AyanamsaDefinition ChandraHari =
        new("AYANAMSA_CHANDRA_HARI", "Chandra Hari", null);
    public static readonly AyanamsaDefinition Fagan =
        new("AYANAMSA_FAGAN", "Fagan", 0);
    public static readonly AyanamsaDefinition Deluce =
        new("AYANAMSA_DELUCE", "Deluce", 2);
    public static readonly AyanamsaDefinition DjwhalKhul =
        new("AYANAMSA_DJWHAL_KHUL", "Djwhal Khul", 6);
    public static readonly AyanamsaDefinition Aldebaran15Tau =
        new("AYANAMSA_ALDEBARAN_15_TAU", "Aldebaran at 15Ta0", 14);
    public static readonly AyanamsaDefinition GalacticCenter =
        new("AYANAMSA_GALACTIC_CENTER", "Galaxy center at 0Sg0", 17);
    public static readonly AyanamsaDefinition Hipparchos =
        new("AYANAMSA_HIPPARCHOS", "Hipparchos", 15);
    public static readonly AyanamsaDefinition Sassanian =
        new("AYANAMSA_SASSANIAN", "Sassanian", 16);
    public static readonly AyanamsaDefinition Tropical =
        new("AYANAMSA_TROPICAL", "Tropical (sayana)", null, IsTropical: true);

    public static IReadOnlyList<AyanamsaDefinition> Catalog { get; } =
    [
        TrueLahiri, TraditionalLahiri, PushyaPaksha, Raman, Krishnamoorthy,
        FixedStarCustom, Jagannatha, RohiniPaksha, SriSuryaSiddhanta, DevaDatta,
        UshaShashi, Yukteshwar, JnBhasin, ChandraHari, Fagan, Deluce, DjwhalKhul,
        Aldebaran15Tau, GalacticCenter, Hipparchos, Sassanian, Tropical
    ];

    // Lahiri / Chitrapaksha (Swiss SE_SIDM_LAHIRI = 1) is the project baseline — a polynomial
    // model that needs no Swiss data files, unlike True Chitrapaksha (27), which requires
    // sefstars.txt and cannot run in this file-less Moshier configuration. It is the frame
    // every verify-* reference chart was built in. c238aa8 switched chart generation from this
    // hard-coded mode to the DB default, which migration 36 had seeded as Jagannatha (mode 26
    // = SE_SIDM_SS_CITRA, ~22.745° for 1981) — a ~0.85° regression — now reverted here and in
    // tbl_Rule_Ayanamsa. This constant is only the fallback when no DB row is available.
    public static AyanamsaDefinition Default => TraditionalLahiri;

    public static AyanamsaDefinition FromCode(string? code) =>
        Catalog.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase))
        ?? Default;
}
