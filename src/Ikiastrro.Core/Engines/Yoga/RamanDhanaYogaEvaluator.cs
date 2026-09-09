using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

/// <summary>Raman combinations 118–143; unsupported source-strength qualifiers remain explicit.</summary>
public static class RamanDhanaYogaEvaluator
{
    public static IReadOnlyList<ContextualYogaResult> Evaluate(ChartBundle bundle)
    {
        var c=bundle.Charts.FirstOrDefault(x=>x.ChartType.Equals("D1",StringComparison.OrdinalIgnoreCase));
        if(c is null)return [];
        var d9=bundle.Charts.FirstOrDefault(x=>x.ChartType.Equals("D9",StringComparison.OrdinalIgnoreCase));
        return
        [
            Row(118,135,147,Dhana118(c)),Row(119,135,147,Dhana119(c)),Row(120,135,147,Dhana120(c)),
            Row(121,135,147,Dhana121(c)),Row(122,136,148,Dhana122(c)),
            Row(123,140,152,OwnLagnaInfluenced(c,PlanetName.Sun,PlanetName.Mars,PlanetName.Jupiter)),
            Row(124,140,152,OwnLagnaInfluenced(c,PlanetName.Moon,PlanetName.Jupiter,PlanetName.Mars)),
            Row(125,140,152,OwnLagnaInfluenced(c,PlanetName.Mars,PlanetName.Moon,PlanetName.Venus,PlanetName.Saturn)),
            Row(126,140,152,OwnLagnaInfluenced(c,PlanetName.Mercury,PlanetName.Saturn,PlanetName.Venus)),
            Row(127,140,152,OwnLagnaInfluenced(c,PlanetName.Jupiter,PlanetName.Mercury,PlanetName.Mars)),
            Row(128,140,152,OwnLagnaInfluenced(c,PlanetName.Venus,PlanetName.Saturn,PlanetName.Mercury)),
            Named("YOGA_BAHUDRAVYARJANA",129,143,155,Bahudravyarjana(c)),
            Missing(130,144,156,"Strongest-planet policy and Vaiseshikamsa output are required."),
            Missing(131,144,156,"D9, Vaiseshikamsa and source-qualified strength evidence are required."),
            Dhana132(c),
            Missing(133,146,158,"An authoritative threshold for 'possessing Kalabala' is required."),
            Missing(134,147,159,"A source-qualified strong-dispositor policy is required."),
            d9 is null?Missing(135,147,159,"D9 placement is required."):Row(135,147,159,Dhana135(c,d9)),
            Row(136,148,160,Dhana136(c)),
            Missing(137,148,160,"D9 and Vaiseshikamsa output for the ascendant lord are required."),
            Named("YOGA_MATRUMOOLA_DHANA",138,149,161,Associated(c,Lord(c,2),Lord(c,4))),
            Missing(139,149,161,"Strong-second-lord policy and Vaiseshikamsa output are required."),
            Missing(140,151,163,"Strong-second-lord policy and Vaiseshikamsa output are required."),
            Missing(141,151,163,"Source-qualified strength for the second and ascendant lords is required."),
            Missing(142,151,163,"Source-qualified strength for wealth-giving planets is required."),
            Named("YOGA_AYATNA_DHANA_LABHA",143,152,164,Exchange(c,Lord(c,1),1,Lord(c,2),2))
        ];
    }
    private static bool Dhana118(ChartAnalysisInput c)=>Lord(c,5)==PlanetName.Venus&&Find(c,PlanetName.Venus)?.HouseNumber==5&&Find(c,PlanetName.Saturn)?.HouseNumber==11;
    private static bool Dhana119(ChartAnalysisInput c)=>Lord(c,5)==PlanetName.Mercury&&Find(c,PlanetName.Mercury)?.HouseNumber==5&&Find(c,PlanetName.Moon)?.HouseNumber==11&&Find(c,PlanetName.Mars)?.HouseNumber==11;
    private static bool Dhana120(ChartAnalysisInput c)=>Lord(c,5)==PlanetName.Saturn&&Find(c,PlanetName.Saturn)?.HouseNumber==5&&Find(c,PlanetName.Mercury)?.HouseNumber==11&&Find(c,PlanetName.Mars)?.HouseNumber==11;
    private static bool Dhana121(ChartAnalysisInput c)=>Lord(c,5)==PlanetName.Sun&&Find(c,PlanetName.Sun)?.HouseNumber==5&&Find(c,PlanetName.Jupiter)?.HouseNumber==11&&Find(c,PlanetName.Moon)?.HouseNumber==11;
    private static bool Dhana122(ChartAnalysisInput c)=>Lord(c,5)==PlanetName.Jupiter&&Find(c,PlanetName.Jupiter)?.HouseNumber==5&&Find(c,PlanetName.Mars)?.HouseNumber==11&&Find(c,PlanetName.Moon)?.HouseNumber==11;
    private static bool OwnLagnaInfluenced(ChartAnalysisInput c,PlanetName occupant,params PlanetName[] influences)
    {
        var p=Find(c,occupant);if(p?.HouseNumber!=1||Lord(c,1)!=occupant)return false;
        return influences.All(x=>Influences(c,x,p.Sign));
    }
    private static bool Bahudravyarjana(ChartAnalysisInput c)=>Find(c,Lord(c,1))?.HouseNumber==2&&Find(c,Lord(c,2))?.HouseNumber==11&&Find(c,Lord(c,11))?.HouseNumber==1;
    private static ContextualYogaResult Dhana132(ChartAnalysisInput c)
    {
        var l1=Find(c,Lord(c,1));var l2=Find(c,Lord(c,2));
        if(l1 is not null&&l2 is not null&&Distance(l1.Sign,l2.Sign) is 1 or 4 or 5 or 7 or 9 or 10)return Row(132,144,156,true);
        var planet=Lord(c,2);
        if(IsNaturalBenefic(planet)&&DeeplyExalted(c,planet))return Row(132,144,156,true);
        return Missing(132,144,156,"If the positional alternative is absent, source-qualified benefic/strength evidence is required.");
    }
    private static bool Dhana135(ChartAnalysisInput c,ChartAnalysisInput d9)
    {
        var l1=Lord(c,1);var l2=Lord(c,2);var l10=Lord(c,10);var p1=Find(c,l1);var p2=Find(c,l2);var p10=Find(c,l10);
        if(p1 is null||p2 is null||p10 is null||p2.Sign!=p10.Sign||p2.HouseNumber is not(1 or 4 or 7 or 10))return false;
        var l1d9=Find(d9,l1);if(l1d9 is null)return false;
        var nl=Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(l1d9.Sign)));var np=Find(c,nl);
        return np is not null&&Aspects(nl,np.Sign,p2.Sign);
    }
    private static bool Dhana136(ChartAnalysisInput c)
    {
        var l1=Find(c,Lord(c,1));var l2=Find(c,Lord(c,2));if(l1?.HouseNumber!=3||l2?.HouseNumber!=3)return false;
        return new[]{PlanetName.Moon,PlanetName.Mercury,PlanetName.Jupiter,PlanetName.Venus}.Any(p=>Influences(c,p,l1.Sign));
    }
    private static bool Associated(ChartAnalysisInput c,PlanetName a,PlanetName b){var x=Find(c,a);var y=Find(c,b);return x is not null&&y is not null&&(x.Sign==y.Sign||Aspects(b,y.Sign,x.Sign));}
    private static bool Exchange(ChartAnalysisInput c,PlanetName a,int ah,PlanetName b,int bh)=>a!=b&&Find(c,a)?.HouseNumber==bh&&Find(c,b)?.HouseNumber==ah;
    private static bool Influences(ChartAnalysisInput c,PlanetName p,string target){var x=Find(c,p);return x is not null&&(x.Sign==target||Aspects(p,x.Sign,target));}
    private static bool Aspects(PlanetName p,string a,string b){var d=Distance(a,b);return d==7||p==PlanetName.Mars&&d is 4 or 8||p==PlanetName.Jupiter&&d is 5 or 9||p==PlanetName.Saturn&&d is 3 or 10;}
    private static int Distance(string a,string b)=>(((int)Enum.Parse<ZodiacName>(b)-(int)Enum.Parse<ZodiacName>(a)+12)%12)+1;
    private static bool IsNaturalBenefic(PlanetName p)=>p is PlanetName.Moon or PlanetName.Mercury or PlanetName.Jupiter or PlanetName.Venus;
    private static bool DeeplyExalted(ChartAnalysisInput c,PlanetName p)
    {
        var degrees=new Dictionary<PlanetName,double>{{PlanetName.Sun,10},{PlanetName.Moon,33},{PlanetName.Mars,298},{PlanetName.Mercury,165},{PlanetName.Jupiter,95},{PlanetName.Venus,327},{PlanetName.Saturn,200}};
        var x=Find(c,p);return x?.NirayanaLongitudeDegrees is double lon&&Math.Abs(lon-degrees[p])<.000001;
    }
    private static PlanetName Lord(ChartAnalysisInput c,int h)=>Enum.Parse<PlanetName>(HouseEngine.GetSignLord(HouseEngine.GetHouseSign(c.AscendantSign,h)));
    private static PlanetPosition? Find(ChartAnalysisInput c,PlanetName p)=>c.Planets.SingleOrDefault(x=>x.Planet==p.ToString());
    private static ContextualYogaResult Row(int n,int printed,int scan,bool present)=>Named("YOGA_DHANA",n,printed,scan,present);
    private static ContextualYogaResult Named(string code,int n,int printed,int scan,bool present)=>new(code,present,"EVALUATED","SRC_RAMAN_300_COMBINATIONS",$"RAMAN_300_{n:000}",$"combination {n}; printed p.{printed}; scan p.{scan}");
    private static ContextualYogaResult Missing(int n,int printed,int scan,string note)=>new("YOGA_DHANA",null,"NOT_EVALUATED","SRC_RAMAN_300_COMBINATIONS",$"RAMAN_300_{n:000}",$"combination {n}; printed p.{printed}; scan p.{scan}",note);
}
