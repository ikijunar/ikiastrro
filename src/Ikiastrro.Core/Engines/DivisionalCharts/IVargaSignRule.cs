using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>
/// Maps a planet's real (nirayana) longitude to its sign in one divisional chart.
/// One implementation per varga scheme (see tbl_Rule_VargaScheme.SignRuleKey);
/// VargaSignRuleFactory builds the right one from a scheme row. Takes the full
/// 0-360 longitude so wrapper rules can defer to AstroMath.GetXSign unchanged.
/// </summary>
public interface IVargaSignRule
{
    ZodiacName SignFor(double siderealLongitude);
}
