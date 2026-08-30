using Ikiastrro.Core.Astro;

namespace Ikiastrro.Core.Models;

/// <summary>Where a slow planet (Saturn/Jupiter/Rahu/Ketu) sits sidereally as of a date, plus when it
/// last entered that sign and when it next leaves — assembled from tbl_PlanetSignTransitEvents.</summary>
public record PlanetTransitSnapshot(
    PlanetName Planet, byte SignId, DateTime InSignSinceUtc, string MotionDirection, DateTime? NextChangeUtc);
