using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;
namespace Ikiastrro.Core.Engines.Yoga;

/// <summary>Raman combinations 144–150 of the ten-form Daridra group.</summary>
public static class RamanDaridraYogaEvaluator
{
 private static readonly PlanetName[] Benefics=[PlanetName.Moon,PlanetName.Mercury,PlanetName.Jupiter,PlanetName.Venus];
 private static readonly PlanetName[] Malefics=[PlanetName.Sun,PlanetName.Mars,PlanetName.Saturn];
 public static IReadOnlyList<ContextualYogaResult> Evaluate(ChartAnalysisInput c)=>
 [
  Row(144,Exchange(c,Lord(c,12),12,Lord(c,1),1)&&Influenced(c,Lord(c,1),Lord(c,7))),
  Row(145,Exchange(c,Lord(c,6),6,Lord(c,1),1)&&(Influenced(c,PlanetName.Moon,Lord(c,2))||Influenced(c,PlanetName.Moon,Lord(c,7)))),
  Row(146,Find(c,PlanetName.Ketu)?.HouseNumber==1&&Find(c,PlanetName.Moon)?.HouseNumber==1),
  Row(147,Find(c,Lord(c,1))?.HouseNumber==8&&(Influenced(c,Lord(c,1),Lord(c,2))||Influenced(c,Lord(c,1),Lord(c,7)))),
  Row(148,JoinsAny(c,Lord(c,1),[Lord(c,6),Lord(c,8),Lord(c,12)])&&JoinsAny(c,Lord(c,1),Malefics)&&(Influenced(c,Lord(c,1),Lord(c,2))||Influenced(c,Lord(c,1),Lord(c,7)))),
  Row(149,JoinsAny(c,Lord(c,1),[Lord(c,6),Lord(c,8),Lord(c,12)])&&Malefics.Any(m=>Influenced(c,Lord(c,1),m))),
  Row(150,JoinsAny(c,Lord(c,5),[Lord(c,6),Lord(c,8),Lord(c,12)])&&!Benefics.Any(b=>Influenced(c,Lord(c,5),b)))
 ];
 private static bool Exchange(ChartAnalysisInput c,PlanetName a,int ah,PlanetName b,int bh)=>a!=b&&Find(c,a)?.HouseNumber==bh&&Find(c,b)?.HouseNumber==ah;
 private static bool JoinsAny(ChartAnalysisInput c,PlanetName target,IEnumerable<PlanetName> others){var x=Find(c,target);return x is not null&&others.Distinct().Any(p=>p!=target&&Find(c,p)?.Sign==x.Sign);}
 private static bool Influenced(ChartAnalysisInput c,PlanetName target,PlanetName source){var x=Find(c,target);var y=Find(c,source);return x is not null&&y is not null&&(x.Sign==y.Sign||Aspects(source,y.Sign,x.Sign));}
 private static bool Aspects(PlanetName p,string a,string b){var d=(((int)Enum.Parse<ZodiacName>(b)-(int)Enum.Parse<ZodiacName>(a)+12)%12)+1;return d==7||p==PlanetName.Mars&&d is 4 or 8||p==PlanetName.Jupiter&&d is 5 or 9||p==PlanetName.Saturn&&d is 3 or 10;}
 private static PlanetName Lord(ChartAnalysisInput c,int h)=>Enum.Parse<PlanetName>(HouseEngine.GetSignLord(HouseEngine.GetHouseSign(c.AscendantSign,h)));
 private static PlanetPosition? Find(ChartAnalysisInput c,PlanetName p)=>c.Planets.SingleOrDefault(x=>x.Planet==p.ToString());
 private static ContextualYogaResult Row(int n,bool present)=>new("YOGA_DARIDRA",present,"EVALUATED","SRC_RAMAN_300_COMBINATIONS",$"RAMAN_300_{n:000}",$"combination {n}; printed p.153; scan p.165");
}
