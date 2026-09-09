using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

/// <summary>Raman combinations 101–117. Uses ChartBundle evidence and reports unsupported qualifications.</summary>
public static class RamanYogaBatchSevenEvaluator
{
    private static readonly PlanetName[] Benefics = [PlanetName.Moon, PlanetName.Mercury, PlanetName.Jupiter, PlanetName.Venus];
    private static readonly PlanetName[] Malefics = [PlanetName.Sun, PlanetName.Mars, PlanetName.Saturn];
    private static readonly HashSet<ZodiacName> DrySigns = [ZodiacName.Aries, ZodiacName.Taurus, ZodiacName.Gemini, ZodiacName.Leo, ZodiacName.Virgo, ZodiacName.Sagittarius];
    private static readonly HashSet<PlanetName> DryPlanets = [PlanetName.Sun, PlanetName.Mars, PlanetName.Saturn, PlanetName.Mercury];
    private static readonly HashSet<ZodiacName> WaterySigns = [ZodiacName.Cancer, ZodiacName.Aquarius, ZodiacName.Capricornus, ZodiacName.Pisces, ZodiacName.Scorpio, ZodiacName.Libra];

    public static IReadOnlyList<ContextualYogaResult> Evaluate(ChartBundle bundle)
    {
        var d1 = bundle.Charts.FirstOrDefault(c => c.ChartType.Equals("D1", StringComparison.OrdinalIgnoreCase));
        if (d1 is null) return Array.Empty<ContextualYogaResult>();
        var d9 = bundle.Charts.FirstOrDefault(c => c.ChartType.Equals("D9", StringComparison.OrdinalIgnoreCase));
        return
        [
            Row("YOGA_SRIK",101,125,137,Benefics.All(p=>Kendra(d1,p))),
            Row("YOGA_SARPA",102,125,137,Malefics.All(p=>Kendra(d1,p))),
            Row("YOGA_DURYOGA",103,127,139,Find(d1,Lord(d1,10))?.HouseNumber is 6 or 8 or 12),
            Row("YOGA_DARIDRA",104,128,140,Find(d1,Lord(d1,11))?.HouseNumber is 6 or 8 or 12),
            Row("YOGA_HARSHA",105,129,141,Find(d1,Lord(d1,6))?.HouseNumber==6),
            Row("YOGA_SARALA",106,129,141,Find(d1,Lord(d1,8))?.HouseNumber==8),
            Row("YOGA_VIMALA",107,129,141,Find(d1,Lord(d1,12))?.HouseNumber==12),
            Row("YOGA_SAREERA_SOUKHYA",108,130,142,Kendra(d1,Lord(d1,1))||Kendra(d1,PlanetName.Jupiter)||Kendra(d1,PlanetName.Venus)),
            Row("YOGA_DEHAPUSHTI",109,130,142,Dehapushti(d1)),
            Row("YOGA_DEHAKASHTA",110,131,143,Dehakashta(d1)),
            Rogagrastha(d1),
            Row("YOGA_KRISANGA",112,132,144,Krisanga112(d1)),
            d9 is null?Missing("YOGA_KRISANGA",113,132,144,"D9 ascendant is required."):Row("YOGA_KRISANGA",113,132,144,Krisanga113(d1,d9)),
            d9 is null?Missing("YOGA_DEHASTHOULYA",114,133,145,"D9 placement is required."):Row("YOGA_DEHASTHOULYA",114,133,145,Dehasthoulya114(d1,d9)),
            Row("YOGA_DEHASTHOULYA",115,133,145,Dehasthoulya115(d1)),
            Row("YOGA_DEHASTHOULYA",116,133,145,Dehasthoulya116(d1)),
            Row("YOGA_SADA_SANCHARA",117,135,147,SadaSanchara(d1))
        ];
    }

    private static ContextualYogaResult Rogagrastha(ChartAnalysisInput c)
    {
        var lord=Lord(c,1);var p=Find(c,lord);
        var first=p?.HouseNumber==1&&new[]{Lord(c,6),Lord(c,8),Lord(c,12)}.Any(x=>Find(c,x)?.Sign==p.Sign);
        return first?Row("YOGA_ROGAGRASTHA",111,131,143,true)
            :Missing("YOGA_ROGAGRASTHA",111,131,143,"The second alternative requires an authoritative weak-ascendant-lord Shadbala threshold.");
    }
    private static bool Dehapushti(ChartAnalysisInput c){var l=Find(c,Lord(c,1));return l is not null&&Movable(l.Sign)&&Benefics.Any(p=>Influences(c,p,l.Sign));}
    private static bool Dehakashta(ChartAnalysisInput c){var l=Find(c,Lord(c,1));return l?.HouseNumber==8||l is not null&&Malefics.Any(p=>Find(c,p)?.Sign==l.Sign);}
    private static bool Krisanga112(ChartAnalysisInput c){var p=Find(c,Lord(c,1));if(p is null)return false;var s=Enum.Parse<ZodiacName>(p.Sign);return DrySigns.Contains(s)||DryPlanets.Contains(Enum.Parse<PlanetName>(HouseEngine.GetSignLord(s)));}
    private static bool Krisanga113(ChartAnalysisInput d1,ChartAnalysisInput d9)=>DryPlanets.Contains(Enum.Parse<PlanetName>(HouseEngine.GetSignLord(d9.AscendantSign)))&&Malefics.Any(p=>Find(d1,p)?.HouseNumber==1);
    private static bool Dehasthoulya114(ChartAnalysisInput d1,ChartAnalysisInput d9){var l=Lord(d1,1);var x=Find(d9,l);if(x is null)return false;var nl=Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(x.Sign)));return Watery(Find(d1,l)?.Sign)&&Watery(Find(d1,nl)?.Sign);}
    private static bool Dehasthoulya115(ChartAnalysisInput c){var j=Find(c,PlanetName.Jupiter);return j is not null&&Watery(j.Sign)&&(j.HouseNumber==1||Aspects(PlanetName.Jupiter,j.Sign,c.AscendantSign.ToString()));}
    private static bool Dehasthoulya116(ChartAnalysisInput c)=>(Watery(c.AscendantSign.ToString())&&Benefics.Any(p=>Find(c,p)?.HouseNumber==1))||Lord(c,1) is PlanetName.Moon or PlanetName.Venus;
    private static bool SadaSanchara(ChartAnalysisInput c){var l=Find(c,Lord(c,1));if(l is null)return false;var d=Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(l.Sign)));return Movable(l.Sign)||Movable(Find(c,d)?.Sign);}
    private static bool Kendra(ChartAnalysisInput c,PlanetName p)=>Find(c,p)?.HouseNumber is 1 or 4 or 7 or 10;
    private static bool Influences(ChartAnalysisInput c,PlanetName p,string target){var x=Find(c,p);return x is not null&&(x.Sign==target||Aspects(p,x.Sign,target));}
    private static bool Aspects(PlanetName p,string a,string b){var d=(((int)Enum.Parse<ZodiacName>(b)-(int)Enum.Parse<ZodiacName>(a)+12)%12)+1;return d==7||p==PlanetName.Mars&&d is 4 or 8||p==PlanetName.Jupiter&&d is 5 or 9||p==PlanetName.Saturn&&d is 3 or 10;}
    private static bool Movable(string? s)=>s is not null&&Enum.Parse<ZodiacName>(s) is ZodiacName.Aries or ZodiacName.Cancer or ZodiacName.Libra or ZodiacName.Capricornus;
    private static bool Watery(string? s)=>s is not null&&WaterySigns.Contains(Enum.Parse<ZodiacName>(s));
    private static PlanetName Lord(ChartAnalysisInput c,int h)=>Enum.Parse<PlanetName>(HouseEngine.GetSignLord(HouseEngine.GetHouseSign(c.AscendantSign,h)));
    private static PlanetPosition? Find(ChartAnalysisInput c,PlanetName p)=>c.Planets.SingleOrDefault(x=>x.Planet==p.ToString());
    private static ContextualYogaResult Row(string code,int n,int printed,int scan,bool present)=>new(code,present,"EVALUATED","SRC_RAMAN_300_COMBINATIONS",$"RAMAN_300_{n:000}",$"combination {n}; printed p.{printed}; scan p.{scan}");
    private static ContextualYogaResult Missing(string code,int n,int printed,int scan,string note)=>new(code,null,"NOT_EVALUATED","SRC_RAMAN_300_COMBINATIONS",$"RAMAN_300_{n:000}",$"combination {n}; printed p.{printed}; scan p.{scan}",note);
}
