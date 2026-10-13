namespace Ikiastrro.Core.Engines.PlanetNature;

/// <param name="Class">Dhruva, Chara, Ugra, Mishra, Kshipra, Mridu or Tikshna.</param>
/// <param name="Suits">What the Muhurta literature says the class is used for.</param>
public sealed record NakshatraTemperamentInfo(string Class, string Suits);

/// <summary>
/// The seven "natures" of the nakshatras from the Muhurta literature (Kusum Vashistha, Muhurta Prakarana, "Nature of nakshatras",
/// pp.56-58; the same classes as Muhurta Chintamani). They time actions — the class says what work a star suits — and are not
/// a benefic/malefic verdict on a planet standing in it, so the table shows them as a fact beside the star and says no more.
/// The English translation's table drops Mula from the Tikshna row; Mula is Tikshna in the classical list (4+5+5+2+3+4+4 with Abhijit).
/// </summary>
public static class NakshatraTemperament
{
    private static readonly (string Class, string Suits, string[] Stars)[] Classes =
    {
        ("Dhruva", "fixed: sowing, house-entry, founding a town, ornaments", new[] { "Rohini", "Uttara Phalguni", "Uttara Ashadha", "Uttara Bhadrapada" }),
        ("Chara", "moving: vehicles, travel, a new branch of learning", new[] { "Punarvasu", "Swati", "Shravana", "Dhanishta", "Shatabhisha" }),
        ("Ugra", "fierce: defeating an enemy, fire, taming animals, cruel work", new[] { "Bharani", "Magha", "Purva Phalguni", "Purva Ashadha", "Purva Bhadrapada" }),
        ("Mishra", "mixed: mixing metals, captivating, fire rites", new[] { "Krittika", "Vishakha" }),
        ("Kshipra", "swift: selling, learning, medicine, arts", new[] { "Ashwini", "Pushya", "Hasta", "Abhijit" }),
        ("Mridu", "soft: friendships, new clothes, ornaments, music", new[] { "Mrigashira", "Chitra", "Anuradha", "Revati" }),
        ("Tikshna", "severe: harsh rites, quarrels, training horses and elephants", new[] { "Ardra", "Ashlesha", "Jyeshtha", "Mula" }),
    };

    public static NakshatraTemperamentInfo? Of(string? nakshatra)
    {
        if (string.IsNullOrWhiteSpace(nakshatra)) return null;
        foreach (var c in Classes)
            if (c.Stars.Contains(nakshatra, StringComparer.OrdinalIgnoreCase)) return new NakshatraTemperamentInfo(c.Class, c.Suits);
        return null;
    }
}
