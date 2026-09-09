using Ikiastrro.Core.Engines.Astronomy;using Ikiastrro.Core.Engines.Yoga;using Ikiastrro.Core.Models;using Ikiastrro.Core.Pipeline;
namespace Ikiastrro.Web.Tests;
public sealed class RamanDaridraYogaEvaluatorTests
{
 [Fact]public void Tracks_144_Through_150(){var r=RamanDaridraYogaEvaluator.Evaluate(new("D1",ZodiacName.Aries,[]));Assert.Equal(7,r.Count);Assert.Equal("RAMAN_300_144",r.First().SourceVariantCode);Assert.Equal("RAMAN_300_150",r.Last().SourceVariantCode);}
 [Fact]public void Combination_146_Requires_Moon_And_Ketu_In_Lagna(){var c=new ChartAnalysisInput("D1",ZodiacName.Aries,[P("Moon","Aries",1),P("Ketu","Aries",1)]);Assert.True(RamanDaridraYogaEvaluator.Evaluate(c).Single(x=>x.SourceVariantCode=="RAMAN_300_146").Present);}
 private static PlanetPosition P(string p,string s,int h)=>new(){Planet=p,Sign=s,HouseNumber=h};
}
