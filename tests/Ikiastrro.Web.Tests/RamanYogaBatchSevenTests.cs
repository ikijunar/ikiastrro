using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class RamanYogaBatchSevenTests
{
    [Fact] public void Missing_D9_Is_Reported_For_113_And_114()
    {
        var rows=RamanYogaBatchSevenEvaluator.Evaluate(Bundle(Chart()));
        Assert.Equal("NOT_EVALUATED",Row(rows,113).EvaluationStatus);
        Assert.Equal("NOT_EVALUATED",Row(rows,114).EvaluationStatus);
    }
    [Fact] public void Harsha_Sarala_Vimala_Use_Their_Own_Dusthana()
    {
        var c=Chart(P("Mercury","Virgo",6),P("Mars","Scorpio",8),P("Jupiter","Pisces",12));
        var rows=RamanYogaBatchSevenEvaluator.Evaluate(Bundle(c));
        Assert.True(Row(rows,105).Present);Assert.True(Row(rows,106).Present);Assert.True(Row(rows,107).Present);
    }
    [Fact] public void First_Rogagrastha_Alternative_Evaluates_Without_Strength_Threshold()
    {
        var c=Chart(P("Mars","Aries",1),P("Mercury","Aries",1));
        Assert.True(Row(RamanYogaBatchSevenEvaluator.Evaluate(Bundle(c)),111).Present);
    }
    private static ContextualYogaResult Row(IEnumerable<ContextualYogaResult> rows,int n)=>rows.Single(x=>x.SourceVariantCode==$"RAMAN_300_{n:000}");
    private static ChartBundle Bundle(params ChartAnalysisInput[] charts)=>new(new BirthDetails(),new SiderealPositions(0,new Dictionary<PlanetName,double>(),new Dictionary<PlanetName,double>(),new Dictionary<PlanetName,double>(),0,0),new SunTimes(default,default,default,false),charts,new Dictionary<string,string>(),[]);
    private static ChartAnalysisInput Chart(params PlanetPosition[] p)=>new("D1",ZodiacName.Aries,p.ToList());
    private static PlanetPosition P(string p,string s,int h)=>new(){Planet=p,Sign=s,HouseNumber=h};
}
