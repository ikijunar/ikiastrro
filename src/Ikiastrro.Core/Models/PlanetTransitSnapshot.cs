using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Models;

/// <summary>Where a slow planet (Saturn/Jupiter/Rahu/Ketu) sits sidereally as of a date, plus when it
/// last entered that sign and when it next leaves — assembled from tbl_PlanetSignTransitEvents.</summary>
public record PlanetTransitSnapshot(
    PlanetName Planet, byte SignId, DateTime InSignSinceUtc, string MotionDirection, DateTime? NextChangeUtc, double LongitudeDegrees = 0, double DegreeInSign = 0, byte NakshatraId = 0, byte Pada = 0, double SpeedDegreesPerDay = 0, string AyanamsaCode = "");
