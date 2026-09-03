<#
.SYNOPSIS
    Show what a chart component rendered like at an earlier release, without
    checking anything out.

.DESCRIPTION
    Every release commits one golden SVG per chart component under
    docs/artifacts/ui/<Chart>-sample.svg, rendered from a fixed fixture. This
    script pulls that file at a given tag and opens it in the browser, so you can
    eyeball an old design before deciding whether to revert to it.

    See docs/uidesign-dataviz.md section 6 for the revert procedure itself.

.PARAMETER Chart
    Component name, e.g. PolarWheel, SouthIndianGrid, MiniGrid.

.PARAMETER Tag
    Release tag to read from, e.g. v0.5.3. Default: the previous tag.

.PARAMETER History
    Instead of opening a file, list the commits that touched this chart's
    component + isolation CSS, newest first, with the tag each falls under.

.EXAMPLE
    ./scripts/show-chart-at.ps1 -Chart PolarWheel -Tag v0.5.3

.EXAMPLE
    ./scripts/show-chart-at.ps1 -Chart PolarWheel -History
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Chart,
    [string] $Tag,
    [switch] $History
)

$ErrorActionPreference = 'Stop'
$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

$componentGlob = "src/Ikiastrro.Web/Components/Charts/$Chart.razor*"

if ($History) {
    git log --follow --format='%h %ad %s' --date=short -- $componentGlob |
        ForEach-Object {
            $sha = ($_ -split ' ')[0]
            $tag = (git describe --tags --contains $sha 2>$null)
            "{0,-24} {1}" -f ($(if ($tag) { $tag.Trim() } else { '(after last tag)' }), $_)
        }
    return
}

if (-not $Tag) {
    $tags = git tag --sort=-creatordate
    $Tag = ($tags | Select-Object -First 2 | Select-Object -Last 1)
    if (-not $Tag) { throw "No tags yet. Pass -Tag once a release is tagged." }
}

$svgPath = "docs/artifacts/ui/$Chart-sample.svg"
$exists = git cat-file -e "${Tag}:${svgPath}" 2>$null; $ok = $?
if (-not $ok) {
    Write-Warning "No golden SVG for '$Chart' at $Tag ($svgPath not in that tree)."
    Write-Host "Charts with a golden SVG at ${Tag}:" -ForegroundColor Cyan
    git ls-tree -r --name-only $Tag -- docs/artifacts/ui/ | Where-Object { $_ -match '\.svg$' }
    return
}

$out = Join-Path ([IO.Path]::GetTempPath()) "$Chart-$Tag.svg"
git show "${Tag}:${svgPath}" | Set-Content $out -NoNewline
Write-Host "Wrote $out" -ForegroundColor Green
Start-Process $out
