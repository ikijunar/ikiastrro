using Ikiastrro.Core.Engines.Yoga;
namespace Ikiastrro.Web.Tests;
public sealed class RamanFinalHundredYogaCatalogTests
{
 [Fact] public void Catalog_Contains_Each_Number_201_Through_300_Exactly_Once()
 {
  var rows=RamanFinalHundredCatalog.Entries();
  Assert.Equal(100,rows.Count);
  Assert.Equal(100,rows.Select(x=>x.SourceVariantCode).Distinct().Count());
  Assert.Equal(Enumerable.Range(201,100).Select(n=>$"RAMAN_300_{n:000}"),rows.Select(x=>x.SourceVariantCode));
 }
 [Fact] public void Catalog_Does_Not_Report_Unimplemented_Rules_As_Absent()
 {
  Assert.All(RamanFinalHundredCatalog.Entries(),row=>{Assert.Null(row.Present);Assert.Equal("NOT_EVALUATED",row.EvaluationStatus);});
 }
 [Fact] public void Grouped_Raja_Entries_Retain_Individual_Identity()
 {
  var rows=RamanFinalHundredCatalog.Entries().Where(x=>x.YogaCode=="YOGA_RAJA").ToList();
  Assert.Equal(19,rows.Count);Assert.Equal("RAMAN_300_245",rows.First().SourceVariantCode);Assert.Equal("RAMAN_300_263",rows.Last().SourceVariantCode);
 }
}
