using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit.Sdk;

namespace Ikiastrro.Web.Tests;

/// <summary>
/// Compares a rendered component's markup against a committed golden file at
/// <c>docs/artifacts/ui/&lt;name&gt;-sample.svg</c>.
///
/// Set env var <c>IKIASTRRO_UPDATE_SNAPSHOTS=1</c> (or just delete the golden file) to
/// (re)write it. On a mismatch the actual markup is written next to the golden as
/// <c>&lt;name&gt;-sample.received.svg</c> for eyeballing / diffing.
///
/// Normalisation strips Blazor's per-build CSS-isolation scope tokens (<c>b-xxxxxxxxxx</c>)
/// and collapses whitespace, so a <c>tokens.css</c> colour change or a scope-hash reshuffle
/// does not churn snapshots — only structural / attribute / text changes do.
/// </summary>
internal static partial class SnapshotAssert
{
    private static readonly string SnapshotDir =
        Path.Combine(RepoRoot(), "docs", "artifacts", "ui");

    public static void MatchesGolden<TComponent>(this IRenderedComponent<TComponent> cut, string name)
        where TComponent : IComponent
    {
        var actual = Normalise(cut.Markup);
        var goldenPath = Path.Combine(SnapshotDir, $"{name}-sample.svg");

        var update = Environment.GetEnvironmentVariable("IKIASTRRO_UPDATE_SNAPSHOTS") == "1";
        if (update || !File.Exists(goldenPath))
        {
            Directory.CreateDirectory(SnapshotDir);
            File.WriteAllText(goldenPath, actual + "\n");
            return;
        }

        var expected = Normalise(File.ReadAllText(goldenPath));
        if (actual == expected)
            return;

        var receivedPath = Path.Combine(SnapshotDir, $"{name}-sample.received.svg");
        File.WriteAllText(receivedPath, actual + "\n");
        throw new XunitException(
            $"Snapshot mismatch for '{name}'.\n" +
            $"  golden  : {goldenPath}\n" +
            $"  received: {receivedPath}\n" +
            $"If the change is intended, re-run with IKIASTRRO_UPDATE_SNAPSHOTS=1 and commit the golden.");
    }

    private static string Normalise(string markup)
    {
        markup = ScopeAttr().Replace(markup, "");
        markup = ScopeClassToken().Replace(markup, "");
        markup = BetweenTags().Replace(markup, "><");
        markup = Whitespace().Replace(markup, " ");
        return markup.Trim();
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Ikiastrro.slnx")))
            dir = dir.Parent;
        return dir?.FullName
               ?? throw new InvalidOperationException("Could not locate repo root (Ikiastrro.slnx).");
    }

    [GeneratedRegex(@"\s+b-[a-z0-9]{6,12}(="""")?(?=[\s>])")]
    private static partial Regex ScopeAttr();

    [GeneratedRegex(@"\s*b-[a-z0-9]{6,12}(?=[""\s])")]
    private static partial Regex ScopeClassToken();

    [GeneratedRegex(@">\s+<")]
    private static partial Regex BetweenTags();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex Whitespace();
}
