namespace Ikiastrro.Core.Models;

/// <summary>One row of dbo.tbl_SignAttributes (migration 019) — static rasi reference.
/// HouseElement / HouseModality come from the snake_case columns type_house_element /
/// type_house_keyattri, which are SELECT-aliased in SignAttributesRepository. Read-only.</summary>
public record SignAttributeReference(
    byte Id, string SignName, string SignNameSanskrit, string ZodiacEnumValue, byte RulingPlanetId,
    string Gender, string Direction, string? RisingType, string SymbolAnimalType, string SymbolDescription,
    string KalapurushaBodyPart, byte? ExaltedPlanetId, decimal? ExaltedDegree, byte? DebilitatedPlanetId,
    decimal? DebilitatedDegree, byte? MooltrikonaPlanetId, decimal? MooltrikonaRangeStart, decimal? MooltrikonaRangeEnd,
    string HouseElement, string HouseModality);
