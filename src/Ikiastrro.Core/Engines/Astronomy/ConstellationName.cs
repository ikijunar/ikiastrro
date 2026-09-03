namespace Ikiastrro.Core.Engines.Astronomy;

/// <summary>
/// The 27 nakshatras, Aswini-first, in sidereal longitude order (each spans 360/27 = 13°20').
/// Member names are indices only — they are NOT persisted anywhere. The canonical display name (and
/// the value stored in tbl_Chart_KeyDetails.Nakshatra) comes from AstroMath.NakshatraCanonicalNames,
/// which matches tbl_Nakshatras.NakshatraName exactly so stored data joins to the reference table.
/// Member spellings are left in the old VedAstro form to avoid churn; do not rely on ToString().
/// </summary>
public enum ConstellationName
{
    Aswini = 0,
    Bharani = 1,
    Krithika = 2,
    Rohini = 3,
    Mrigasira = 4,
    Aridra = 5,
    Punarvasu = 6,
    Pushyami = 7,
    Aslesha = 8,
    Makha = 9,
    Pubba = 10,
    Uttara = 11,
    Hasta = 12,
    Chitta = 13,
    Swathi = 14,
    Vishhaka = 15,
    Anuradha = 16,
    Jyesta = 17,
    Moola = 18,
    Poorvashada = 19,
    Uttarashada = 20,
    Sravana = 21,
    Dhanishta = 22,
    Satabhisha = 23,
    Poorvabhadra = 24,
    Uttarabhadra = 25,
    Revathi = 26
}
