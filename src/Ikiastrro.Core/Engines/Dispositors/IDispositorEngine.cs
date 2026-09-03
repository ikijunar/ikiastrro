namespace Ikiastrro.Core.Engines.Dispositors;
using Ikiastrro.Core.Pipeline;

/// <summary>Sign-lord traversal, final dispositor, mutual reception. Built in Plan 2.</summary>
public interface IDispositorEngine
{
    IReadOnlyList<DispositorChain> Compute(ChartAnalysisInput d1);
}
