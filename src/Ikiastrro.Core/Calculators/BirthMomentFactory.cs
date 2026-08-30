namespace Ikiastrro.Core.Calculators;

using Ikiastrro.Core.Models;

/// <summary>Resolves a BirthDetails row into the local-offset DateTimeOffset the astro engine needs.</summary>
internal static class BirthMomentFactory
{
    public static DateTimeOffset Create(BirthDetails birthDetails)
    {
        if (!TimeSpan.TryParse(birthDetails.UtcOffset, out var offset))
        {
            throw new InvalidOperationException(
                $"BirthDetails.UtcOffset \"{birthDetails.UtcOffset}\" is not a valid offset (expected e.g. \"05:30\" or \"-08:00\").");
        }

        var localDateTime = birthDetails.DateOfBirth.ToDateTime(birthDetails.EffectiveTimeOfBirth);
        return new DateTimeOffset(localDateTime, offset);
    }
}
