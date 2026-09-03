<#
.SYNOPSIS
    Consolidate everything that changed since the last release into a CHANGELOG
    draft, and (optionally) tag the release.

.DESCRIPTION
    A release is a push to origin/master tagged vX.Y.Z. This script reads the
    commit range <last-tag>..HEAD and produces a Keep-a-Changelog block:

      - Features      : first-parent commit subjects (merge bubbles + direct commits)
      - DB            : migration files added under db/
      - UI            : touched chart components + diffs to the UI/dataviz design docs
      - Features moved : +/- lines mentioning FEAT-<AREA>-<NN> in PRODUCT.md
      - Chart visuals : added/modified golden SVGs under docs/artifacts/ui/

    It prints the draft to stdout. Edit it for readability, then:
      -Apply  replaces the "## [Unreleased]" section of CHANGELOG.md with the
              dated version block (and re-adds an empty Unreleased skeleton)
      -Tag    creates an annotated tag vX.Y.Z whose message is the block

    Nothing is pushed. Review, commit CHANGELOG.md, then:
      git push origin master --follow-tags

.PARAMETER Version
    The release version, e.g. v0.2.0. Required. Must start with 'v'.

.PARAMETER Since
    Ref to diff from. Default: the most recent tag (git describe --tags
    --abbrev=0), or the root commit if there are no tags yet.

.PARAMETER Apply
    Rewrite CHANGELOG.md: promote [Unreleased] to [<version>] - <today> with the
    generated body, then restore an empty [Unreleased] skeleton.

.PARAMETER Tag
    Create the annotated tag from the generated block. Implies the range is final.

.EXAMPLE
    ./scripts/release.ps1 -Version v0.2.0
    Print the draft for review.

.EXAMPLE
    ./scripts/release.ps1 -Version v0.2.0 -Apply -Tag
    Write CHANGELOG.md and create tag v0.2.0. Then: git add CHANGELOG.md &&
    git commit -m "release: v0.2.0" && git push origin master --follow-tags
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidatePattern('^v\d+\.\d+\.\d+$')] [string] $Version,
    [string] $Since,
    [switch] $Apply,
    [switch] $Tag
)

$ErrorActionPreference = 'Stop'
$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

if (-not $Since) {
    $Since = (git describe --tags --abbrev=0 2>$null)
    if (-not $Since) { $Since = (git rev-list --max-parents=0 HEAD | Select-Object -Last 1).Trim() }
}
$range = "$Since..HEAD"
$today = Get-Date -Format 'yyyy-MM-dd'

Write-Host "Range: $range  ($(git rev-list --count $range) commits)" -ForegroundColor Cyan

function Section($title, [string[]]$lines) {
    if (-not $lines -or $lines.Count -eq 0) { return @() }
    return @("### $title") + ($lines | ForEach-Object { "- $_" }) + @('')
}

$features = git log --first-parent --format='%s' $range |
    Where-Object {
        $_ -notmatch '^(chore|docs|style|test|ci)(\(|:)' -and
        $_ -notmatch "^Merge (branch|remote-tracking|pull request)"
    }

$dbAdds = git diff --name-status $range -- 'db/*.sql' |
    Where-Object { $_ -match '^A' } | ForEach-Object { ($_ -split '\t')[1] -replace '^db/','' }

$uiComponents = git diff --name-status $range -- 'src/Ikiastrro.Web/Components/Charts/**' |
    ForEach-Object { $p = ($_ -split '\t')[-1]; [IO.Path]::GetFileName($p) } | Sort-Object -Unique

$uiDocs = git diff --stat $range -- docs/uidesign-specs.md docs/uidesign-dataviz.md |
    Where-Object { $_ -match '\|' } | ForEach-Object { $_.Trim() }

$featMoves = git diff $range -- PRODUCT.md |
    Where-Object { $_ -match '^[+-].*FEAT-[A-Z]+-\d+' -and $_ -notmatch '^[+-]{3}' } |
    ForEach-Object { $_.Trim() }

$chartSvgs = git diff --name-status $range -- 'docs/artifacts/ui/*.svg' |
    ForEach-Object { $s = $_ -split '\t'; "$($s[0])  $([IO.Path]::GetFileName($s[-1]))" }

$block = @("## [$($Version.TrimStart('v'))] - $today", '')
$block += Section 'Added / Changed (first-parent)' $features
$block += Section 'DB — migrations added' $dbAdds
$block += Section 'UI — chart components touched' $uiComponents
$block += Section 'UI — design-doc diffs' $uiDocs
$block += Section 'PRODUCT.md — FEAT lines moved' $featMoves
$block += Section 'Chart golden SVGs' $chartSvgs
$blockText = ($block -join "`n").TrimEnd() + "`n"

Write-Host "`n--- CHANGELOG draft ------------------------------------------------`n" -ForegroundColor Yellow
Write-Output $blockText

if ($Apply) {
    $path = Join-Path $root 'CHANGELOG.md'
    $md = Get-Content $path -Raw
    $skeleton = "## [Unreleased]`n`n_Changes on ``master`` since ``$Version``. Run ``scripts/release.ps1`` to consolidate._`n`n### Added`n### Changed`n### Fixed`n### DB`n### UI`n"
    $md = [regex]::Replace($md, '(?s)## \[Unreleased\].*?(?=\r?\n## \[)', "$skeleton`n$blockText")
    $repo = 'https://github.com/rammyps/ikiastrro'
    $md = $md -replace 'compare/v[\d.]+\.\.\.HEAD', "compare/$Version...HEAD"
    $md += "[$($Version.TrimStart('v'))]: $repo/releases/tag/$Version`n"
    Set-Content $path $md -NoNewline
    Write-Host "CHANGELOG.md updated." -ForegroundColor Green
}

if ($Tag) {
    $tmp = New-TemporaryFile
    Set-Content $tmp $blockText -NoNewline
    git tag -a $Version -F $tmp
    Remove-Item $tmp
    Write-Host "Tagged $Version. Push with: git push origin master --follow-tags" -ForegroundColor Green
}
