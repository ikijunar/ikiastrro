using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;
namespace Ikiastrro.Core.Engines.Yoga;

/// <summary>Raman 151–200. Every entry is tracked; unavailable amsa/strength evidence is explicit.</summary>
public static class RamanYogaBatchEightEvaluator
{
 private static readonly PlanetName[] B=[PlanetName.Moon,PlanetName.Mercury,PlanetName.Jupiter,PlanetName.Venus];
 private static readonly PlanetName[] M=[PlanetName.Sun,PlanetName.Mars,PlanetName.Saturn];
 private static readonly HashSet<int> Unsupported=[155,156,167,170,171,175,176,177,178,179,182,183,185,196,197];
 private static readonly Dictionary<int,string> Codes=new()
 {
  [151]="YOGA_DARIDRA",[152]="YOGA_DARIDRA",[153]="YOGA_DARIDRA",[154]="YOGA_YUKTHI_SAMANWITHAVAGMI",[155]="YOGA_YUKTHI_SAMANWITHAVAGMI",
  [156]="YOGA_PARIHASAKA",[157]="YOGA_ASATYAVADI",[158]="YOGA_JADA",[159]="YOGA_BHASKARA",[160]="YOGA_MARUD",
  [161]="YOGA_SARASWATHI",[162]="YOGA_BUDHA",[163]="YOGA_MOOKA",[164]="YOGA_NETRANASA",[165]="YOGA_ANDHA",
  [166]="YOGA_SUMUKHA",[167]="YOGA_SUMUKHA",[168]="YOGA_DURMUKHA",[169]="YOGA_DURMUKHA",[170]="YOGA_BHOJANA_SOUKHYA",
  [171]="YOGA_ANNADANA",[172]="YOGA_PARANNABHOJANA",[173]="YOGA_SRADDHANNABHUKTHA",[174]="YOGA_SARPAGANDA",[175]="YOGA_VAKCHALANA",
  [176]="YOGA_VISHAPRAYOGA",[177]="YOGA_BHRATRUVRIDDHI",[178]="YOGA_SODARANASA",[179]="YOGA_EKABHAGINI",[180]="YOGA_DWADASA_SAHODARA",
  [181]="YOGA_SAPTHASANKHYA_SAHODARA",[182]="YOGA_PARAKRAMA",[183]="YOGA_YUDDHA_PRAVEENA",[184]="YOGA_YUDDHATPOORVA_DRIDHACHITTA",[185]="YOGA_YUDDHATPASCHAT_DRIDHA",
  [186]="YOGA_SATKATHADI_SRAVANA",[187]="YOGA_UTTAMA_GRIHA",[188]="YOGA_VICHITRA_SAUDHA_PRAKARA",[189]="YOGA_AYATNA_GRIHA_PRAPTA",[190]="YOGA_AYATNA_GRIHA_PRAPTA",
  [191]="YOGA_GRIHANASA",[192]="YOGA_GRIHANASA",[193]="YOGA_BANDHU_PUJYA",[194]="YOGA_BANDHU_PUJYA",[195]="YOGA_BANDHUBHISTHYAKTHA",
  [196]="YOGA_MATRU_DEERGHAYUR",[197]="YOGA_MATRU_DEERGHAYUR",[198]="YOGA_MATRUNASA",[199]="YOGA_MATRUNASA",[200]="YOGA_MATRUGAMI"
 };
 public static IReadOnlyList<ContextualYogaResult> Evaluate(ChartBundle bundle)
 {
  var c=bundle.Charts.FirstOrDefault(x=>x.ChartType.Equals("D1",StringComparison.OrdinalIgnoreCase));if(c is null)return[];
  var d9=bundle.Charts.FirstOrDefault(x=>x.ChartType.Equals("D9",StringComparison.OrdinalIgnoreCase));
  return Enumerable.Range(151,50).Select(n=>Unsupported.Contains(n)?Missing(n,Gap(n)):EvaluateOne(n,c,d9)).ToList();
 }
 private static ContextualYogaResult EvaluateOne(int n,ChartAnalysisInput c,ChartAnalysisInput? d9)
 {
  bool? v=n switch
  {
   151=>Find(c,Lord(c,5))?.HouseNumber is 6 or 10&&new[]{2,6,7,8,12}.Any(h=>Influenced(c,Lord(c,5),Lord(c,h))),
   152=>M.Where(p=>!new[]{Lord(c,9),Lord(c,10)}.Contains(p)).Any(p=>Find(c,p)?.HouseNumber==1&&(Influenced(c,p,Lord(c,2))||Influenced(c,p,Lord(c,7)))),
   153=>d9 is null?null:new[]{Find(c,Lord(c,1))?.HouseNumber,Find(c,Enum.Parse<PlanetName>(HouseEngine.GetSignLord(d9.AscendantSign)))?.HouseNumber}.All(h=>h is 6 or 8 or 12)&&(Influenced(c,Lord(c,1),Lord(c,2))||Influenced(c,Lord(c,1),Lord(c,7))),
   154=>Y154(c),157=>Y157(c),158=>Y158(c),159=>Rel(c,PlanetName.Sun,PlanetName.Mercury,2)&&Rel(c,PlanetName.Mercury,PlanetName.Moon,11)&&(Rel(c,PlanetName.Moon,PlanetName.Jupiter,5)||Rel(c,PlanetName.Moon,PlanetName.Jupiter,9)),
   160=>(Rel(c,PlanetName.Venus,PlanetName.Jupiter,5)||Rel(c,PlanetName.Venus,PlanetName.Jupiter,9))&&Rel(c,PlanetName.Jupiter,PlanetName.Moon,5)&&new[]{1,4,7,10}.Contains(Distance(Find(c,PlanetName.Moon)?.Sign,Find(c,PlanetName.Sun)?.Sign)),
   161=>Y161(c),162=>Y162(c),163=>Find(c,Lord(c,2))?.HouseNumber==8&&Same(c,Lord(c,2),PlanetName.Jupiter),
   164=>Same3(c,Lord(c,10),Lord(c,6),Lord(c,2))&&Find(c,Lord(c,10))?.HouseNumber==1,
   165=>(Same(c,PlanetName.Mercury,PlanetName.Moon)&&Find(c,PlanetName.Mercury)?.HouseNumber==2)||(Same3(c,Lord(c,1),Lord(c,2),PlanetName.Sun)&&Find(c,Lord(c,1))?.HouseNumber==2),
   166=>Kendra(c,Lord(c,2))&&B.Any(p=>Influenced(c,Lord(c,2),p))||B.Any(p=>Find(c,p)?.HouseNumber==2),
   168=>M.Any(p=>Find(c,p)?.HouseNumber==2)&&(M.Any(p=>Same(c,Lord(c,2),p))||Dignity(c,Lord(c,2))=="DEBILITATED"),
   169=>d9 is null?null:Same(c,Lord(c,2),"Gulika")||M.Any(p=>Same(c,Lord(c,2),p))&&Dignity(d9,Lord(c,2))=="DEBILITATED",
   172=>d9 is null?null:Dignity(c,Lord(c,2))=="DEBILITATED"&&M.Any(p=>Influenced(c,Lord(c,2),p)&&Dignity(c,p)=="DEBILITATED"),
   173=>Lord(c,2)==PlanetName.Saturn||Same(c,PlanetName.Saturn,Lord(c,2))||(Find(c,PlanetName.Saturn) is{}s&&Dignity(c,PlanetName.Saturn)=="DEBILITATED"&&Aspects(PlanetName.Saturn,s.Sign,HouseSign(c,2))),
   174=>Find(c,PlanetName.Rahu)?.HouseNumber==2&&Find(c,"Gulika")?.HouseNumber==2,
   180=>Y180(c),181=>Same(c,Lord(c,12),PlanetName.Mars)&&Find(c,PlanetName.Moon)?.HouseNumber==3&&Same(c,PlanetName.Moon,PlanetName.Jupiter)&&!Influenced(c,PlanetName.Moon,PlanetName.Venus),
   184=>d9 is null?null:Dignity(c,Lord(c,3))=="EXALTED"&&M.Any(p=>Same(c,Lord(c,3),p))&&(Movable(Find(c,Lord(c,3))?.Sign)||Movable(Find(d9,Lord(c,3))?.Sign)),
   186=>d9 is null?null:B.Contains(Enum.Parse<PlanetName>(HouseEngine.GetSignLord(HouseEngine.GetHouseSign(c.AscendantSign,3))))&&B.Any(p=>InfluencesSign(c,p,HouseSign(c,3)))&&B.Contains(Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(Find(d9,Lord(c,3))?.Sign??"Aries")))),
   187=>new[]{1,4,5,7,9,10}.Contains(Find(c,Lord(c,4))?.HouseNumber??0)&&B.Any(p=>Same(c,Lord(c,4),p)),
   188=>Same3(c,Lord(c,4),Lord(c,10),PlanetName.Saturn)&&Same(c,Lord(c,4),PlanetName.Mars),
   189=>new[]{1,4}.Contains(Find(c,Lord(c,1))?.HouseNumber??0)&&Same(c,Lord(c,1),Lord(c,7))&&B.Any(p=>Influenced(c,Lord(c,1),p)),
   190=>Kendra(c,Lord(c,9))&&new[]{"OWN","MOOLATRIKONA","EXALTED"}.Contains(Dignity(c,Lord(c,4))),
   191=>Find(c,Lord(c,4))?.HouseNumber==12&&M.Any(p=>Influenced(c,Lord(c,4),p)),
   192=>d9 is null?null:Find(d9,Lord(c,4)) is{}x&&Find(c,Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(x.Sign))))?.HouseNumber==12,
   193=>B.Contains(Lord(c,4))&&B.Any(p=>p!=Lord(c,4)&&Influenced(c,Lord(c,4),p))&&Find(c,PlanetName.Mercury)?.HouseNumber==1,
   194=>Find(c,PlanetName.Jupiter) is{}j&&(j.HouseNumber==4||InfluencesSign(c,PlanetName.Jupiter,HouseSign(c,4))||Same(c,Lord(c,4),PlanetName.Jupiter)),
   195=>M.Any(p=>Same(c,Lord(c,4),p))||new[]{"ENEMY","ADHISATRU","DEBILITATED"}.Contains(Dignity(c,Lord(c,4))),
   198=>Y198(c),199=>d9 is null?null:Y199(c,d9),200=>Y200(c),_=>null
  };
  return v is null?Missing(n,"Required divisional or qualification evidence is unavailable."):Row(n,v.Value);
 }
 private static bool Y154(ChartAnalysisInput c){var l=Lord(c,2);return new[]{1,4,5,7,9,10}.Contains(Find(c,l)?.HouseNumber??0)&&B.Any(p=>Same(c,l,p))||Dignity(c,l)=="EXALTED"&&Same(c,l,PlanetName.Jupiter);}
 private static bool Y157(ChartAnalysisInput c){var x=Find(c,Lord(c,2));if(x is null)return false;var lord=Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(x.Sign)));return lord is PlanetName.Saturn or PlanetName.Mars&&c.Planets.Any(p=>p.HouseNumber is 1 or 4 or 5 or 7 or 9 or 10&&M.Any(m=>m.ToString()==p.Planet));}
 private static bool Y158(ChartAnalysisInput c)=>Find(c,Lord(c,2))?.HouseNumber==10&&M.Any(p=>Same(c,Lord(c,2),p))||Find(c,PlanetName.Sun)?.HouseNumber==2&&Find(c,"Gulika")?.HouseNumber==2;
 private static bool Y161(ChartAnalysisInput c){var allowed=new[]{1,2,4,5,7,9,10};return new[]{PlanetName.Jupiter,PlanetName.Venus,PlanetName.Mercury}.All(p=>allowed.Contains(Find(c,p)?.HouseNumber??0))&&new[]{"OWN","MOOLATRIKONA","EXALTED","FRIEND","ADHIMITRA"}.Contains(Dignity(c,PlanetName.Jupiter));}
 private static bool Y162(ChartAnalysisInput c)=>Find(c,PlanetName.Jupiter)?.HouseNumber==1&&Kendra(c,PlanetName.Moon)&&Rel(c,PlanetName.Moon,PlanetName.Rahu,2)&&Rel(c,PlanetName.Rahu,PlanetName.Sun,3)&&Find(c,PlanetName.Mars)?.Sign==Find(c,PlanetName.Sun)?.Sign;
 private static bool Y180(ChartAnalysisInput c){var l3=Find(c,Lord(c,3));var mars=Find(c,PlanetName.Mars);return Kendra(c,Lord(c,3))&&Dignity(c,PlanetName.Mars)=="EXALTED"&&Same(c,PlanetName.Mars,PlanetName.Jupiter)&&l3 is not null&&mars is not null&&new[]{5,9}.Contains(Distance(l3.Sign,mars.Sign));}
 private static bool Y198(ChartAnalysisInput c){var moon=Find(c,PlanetName.Moon);if(moon is null)return false;var prev=((moon.HouseNumber+10)%12)+1;var next=(moon.HouseNumber%12)+1;return M.Any(p=>Same(c,PlanetName.Moon,p)||Influenced(c,PlanetName.Moon,p))||M.Any(p=>Find(c,p)?.HouseNumber==prev)&&M.Any(p=>Find(c,p)?.HouseNumber==next);}
 private static bool Y199(ChartAnalysisInput c,ChartAnalysisInput d9){var p4=Find(d9,Lord(c,4));if(p4 is null)return false;var a=Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(p4.Sign)));var pa=Find(d9,a);if(pa is null)return false;var b=Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(pa.Sign)));return Find(c,b)?.HouseNumber is 6 or 8 or 12;}
 private static bool Y200(ChartAnalysisInput c)=>new[]{PlanetName.Moon,PlanetName.Venus}.Any(p=>Kendra(c,p)&&M.Any(m=>Influenced(c,p,m)))&&M.Any(p=>Find(c,p)?.HouseNumber==4);
 private static string Gap(int n)=>n switch{155=>"Paramochha, Parvatamsa and Simhasanamsa grades are not implemented.",156 or 167 or 170 or 171=>"Vaiseshikamsa/amsa-grade output is not implemented.",175 or 176 or 182 or 185=>"Source-defined benefic/cruel Navamsa or Shashtiamsa classification is required.",177 or 196 or 197=>"An authoritative source-strength threshold is required.",178 or 179=>"The OCR clause requires visual source adjudication before activation.",183=>"Nested Navamsa and own-varga qualification requires a dedicated verified evaluator.",_=>"Required qualification is unavailable."};
 private static bool Same(ChartAnalysisInput c,PlanetName a,PlanetName b)=>a!=b&&Find(c,a)?.Sign is{}s&&Find(c,b)?.Sign==s;
 private static bool Same(ChartAnalysisInput c,PlanetName a,string b)=>Find(c,a)?.Sign is{}s&&Find(c,b)?.Sign==s;
 private static bool Same3(ChartAnalysisInput c,PlanetName a,PlanetName b,PlanetName d)=>Same(c,a,b)&&Same(c,a,d);
 private static bool Kendra(ChartAnalysisInput c,PlanetName p)=>Find(c,p)?.HouseNumber is 1 or 4 or 7 or 10;
 private static bool Rel(ChartAnalysisInput c,PlanetName a,PlanetName b,int d)=>Find(c,a) is{}x&&Find(c,b) is{}y&&Distance(x.Sign,y.Sign)==d;
 private static bool Influenced(ChartAnalysisInput c,PlanetName target,PlanetName source)=>Find(c,target) is{}x&&Find(c,source) is{}y&&(x.Sign==y.Sign||Aspects(source,y.Sign,x.Sign));
 private static bool InfluencesSign(ChartAnalysisInput c,PlanetName source,string sign)=>Find(c,source) is{}x&&(x.Sign==sign||Aspects(source,x.Sign,sign));
 private static bool Aspects(PlanetName p,string a,string b){var d=Distance(a,b);return d==7||p==PlanetName.Mars&&d is 4 or 8||p==PlanetName.Jupiter&&d is 5 or 9||p==PlanetName.Saturn&&d is 3 or 10;}
 private static int Distance(string? a,string? b)=>a is null||b is null?0:(((int)Enum.Parse<ZodiacName>(b)-(int)Enum.Parse<ZodiacName>(a)+12)%12)+1;
 private static bool Movable(string? s)=>s is not null&&Enum.Parse<ZodiacName>(s) is ZodiacName.Aries or ZodiacName.Cancer or ZodiacName.Libra or ZodiacName.Capricornus;
 private static string HouseSign(ChartAnalysisInput c,int h)=>HouseEngine.GetHouseSign(c.AscendantSign,h).ToString();
 private static string Dignity(ChartAnalysisInput c,PlanetName p){var x=Find(c,p);if(x is null)return"MISSING";var l=x.VargaLongitudeDegrees??x.NirayanaLongitudeDegrees??15;return PvrDignityEvaluator.Evaluate(p,Enum.Parse<ZodiacName>(x.Sign),((l%30)+30)%30).DignityTypeCode;}
 private static PlanetName Lord(ChartAnalysisInput c,int h)=>Enum.Parse<PlanetName>(HouseEngine.GetSignLord(HouseEngine.GetHouseSign(c.AscendantSign,h)));
 private static PlanetPosition? Find(ChartAnalysisInput c,PlanetName p)=>c.Planets.SingleOrDefault(x=>x.Planet==p.ToString());
 private static PlanetPosition? Find(ChartAnalysisInput c,string p)=>c.Planets.SingleOrDefault(x=>x.Planet.Equals(p,StringComparison.OrdinalIgnoreCase));
 private static ContextualYogaResult Row(int n,bool present){var scan=Scan(n);return new(Codes[n],present,"EVALUATED","SRC_RAMAN_300_COMBINATIONS",$"RAMAN_300_{n:000}",$"combination {n}; printed p.{scan-12}; scan p.{scan}");}
 private static ContextualYogaResult Missing(int n,string note){var scan=Scan(n);return new(Codes[n],null,"NOT_EVALUATED","SRC_RAMAN_300_COMBINATIONS",$"RAMAN_300_{n:000}",$"combination {n}; printed p.{scan-12}; scan p.{scan}",note);}
 private static int Scan(int n)=>n switch{<=153=>165,<=155=>166,156=>167,<=158=>169,159=>170,160=>171,161=>172,162=>173,<=164=>174,165=>175,<=167=>176,<=170=>177,171=>178,172=>179,<=174=>180,<=177=>181,178=>183,179=>184,180=>185,<=182=>187,183=>188,<=185=>190,186=>191,187=>192,188=>193,<=190=>195,<=192=>196,<=194=>197,195=>198,<=197=>199,<=199=>200,_=>203};
}
