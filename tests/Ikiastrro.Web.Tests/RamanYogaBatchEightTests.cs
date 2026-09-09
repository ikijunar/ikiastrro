using Ikiastrro.Core.Engines.Astronomy;using Ikiastrro.Core.Engines.Yoga;using Ikiastrro.Core.Models;using Ikiastrro.Core.Pipeline;
namespace Ikiastrro.Web.Tests;
public sealed class RamanYogaBatchEightTests
{
 [Fact]public void Tracks_Every_Number_151_Through_200(){var r=RamanYogaBatchEightEvaluator.Evaluate(Bundle(Chart()));Assert.Equal(50,r.Count);Assert.Equal(Enumerable.Range(151,50).Select(n=>$"RAMAN_300_{n:000}"),r.Select(x=>x.SourceVariantCode));}
 [Fact]public void Unsupported_Amsa_And_Strength_Rules_Are_Unevaluated(){var r=RamanYogaBatchEightEvaluator.Evaluate(Bundle(Chart()));foreach(var n in new[]{155,156,167,170,177,183,196})Assert.Equal("NOT_EVALUATED",Row(r,n).EvaluationStatus);}
 [Fact]public void Sarpaganda_Uses_Rahu_And_Gulika_In_Second(){var c=Chart(P("Rahu","Taurus",2),P("Gulika","Taurus",2));Assert.True(Row(RamanYogaBatchEightEvaluator.Evaluate(Bundle(c)),174).Present);}
 private static ContextualYogaResult Row(IEnumerable<ContextualYogaResult>r,int n)=>r.Single(x=>x.SourceVariantCode==$"RAMAN_300_{n:000}");
 private static ChartBundle Bundle(params ChartAnalysisInput[]c)=>new(new BirthDetails(),new SiderealPositions(0,new Dictionary<PlanetName,double>(),new Dictionary<PlanetName,double>(),new Dictionary<PlanetName,double>(),0,0),new SunTimes(default,default,default,false),c,new Dictionary<string,string>(),[]);
 private static ChartAnalysisInput Chart(params PlanetPosition[]p)=>new("D1",ZodiacName.Aries,p.ToList());private static PlanetPosition P(string p,string s,int h)=>new(){Planet=p,Sign=s,HouseNumber=h};
}
