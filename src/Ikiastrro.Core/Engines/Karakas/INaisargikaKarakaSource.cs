namespace Ikiastrro.Core.Engines.Karakas;

/// <summary>Naisargika (natural) Karaka ordering (Sapta / Ashta). Built in Plan 2.</summary>
public interface INaisargikaKarakaSource
{
    IReadOnlyList<string> NaturalKarakaOrder();
}
