using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;
namespace Ikiastrro.Web.Tests;
public sealed class RamanDhanaYogaEvaluatorTests
{
 [Fact] public void Returns_118_Through_143(){var r=RamanDhanaYogaEvaluator.Evaluate(Bundle(Chart()));Assert.Equal(26,r.Count);Assert.Equal("RAMAN_300_118",r.First().SourceVariantCode);Assert.Equal("RAMAN_300_143",r.Last().SourceVariantCode);}
 [Fact] public void Combination_121_Uses_Aries_Sun_Fifth_And_Moon_Jupiter_Eleventh(){var c=Chart(P("Sun","Leo",5),P("Moon","Aquarius",11),P("Jupiter","Aquarius",11));Assert.True(Row(RamanDhanaYogaEvaluator.Evaluate(Bundle(c)),121).Present);}
 [Fact] public void Vaiseshikamsa_Rules_Are_Not_Falsely_Absent(){Assert.Equal("NOT_EVALUATED",Row(RamanDhanaYogaEvaluator.Evaluate(Bundle(Chart())),130).EvaluationStatus);}
 private static ContextualYogaResult Row(IEnumerable<ContextualYogaResult> r,int n)=>r.Single(x=>x.SourceVariantCode==$"RAMAN_300_{n:000}");
 private static ChartBundle Bundle(params ChartAnalysisInput[] c)=>new(new BirthDetails(),new SiderealPositions(0,new Dictionary<PlanetName,double>(),new Dictionary<PlanetName,double>(),new Dictionary<PlanetName,double>(),0,0),new SunTimes(default,default,default,false),c,new Dictionary<string,string>(),[]);
 private static ChartAnalysisInput Chart(params PlanetPosition[] p)=>new("D1",ZodiacName.Aries,p.ToList());
 private static PlanetPosition P(string p,string s,int h)=>new(){Planet=p,Sign=s,HouseNumber=h};
}
