namespace Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Pipeline;

/// <summary>Yoga detection (Pañcha Mahāpuruṣa, Rāja, Dhana, Nābhasa, Neechabhaṅga, Parivartana…). Built in Plan 4.</summary>
public interface IYogaEngine
{
    IReadOnlyList<YogaResult> Detect(ChartBundle bundle);
}
