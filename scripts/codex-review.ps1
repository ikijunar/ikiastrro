<#
.SYNOPSIS
    Ask OpenAI Codex CLI (ChatGPT) for a read-only advisory review of ikiastrro.

.DESCRIPTION
    Claude Code stays the build engine. This script bundles the repo's source,
    SQL and design docs into one prompt and pipes it to `codex exec`. Codex never
    touches the filesystem - it only reads the piped text and replies.

    Output is saved to  D:\@ChatGPT\ikiastrro\  (the global default ChatGPT
    folder, per ~/.claude/CLAUDE.md). The filename is STABLE and overwritten on
    each run - no timestamp - unless -Versioned is passed.

    Why a bundle instead of --sandbox read-only file access: on native Windows the
    Codex sandbox blocks all shell command execution, so Codex cannot explore a
    repo itself. Feeding it the context directly is reliable and genuinely
    read-only. (Run under WSL2 if you later want interactive repo exploration.)

.PARAMETER Focus
    Extra text appended to the review instructions, e.g.
    "the D9 / D10 varga math in DivisionalChartCalculator". When set, the output
    file is codex-review-<focus-slug>.md so a focused pass doesn't clobber the
    full review.

.PARAMETER Paths
    Top-level folders (relative to repo root) to bundle. Default: src, db, docs,
    decisions. Narrow this (e.g. -Paths src) if the bundle gets too big.

.PARAMETER Model
    Optional Codex model. Leave empty to use the account default (recommended -
    named models like gpt-5.x-codex are rejected on a ChatGPT sign-in).

.PARAMETER OutName
    Override the output file name (with or without .md). Still saved under
    D:\@ChatGPT\ikiastrro\.

.PARAMETER Versioned
    Append -yyyy-MM-dd-HHmm to the file name instead of overwriting. Use when you
    want to keep a history of a particular review.

.EXAMPLE
    .\scripts\codex-review.ps1                       # -> codex-review.md (overwrites)

.EXAMPLE
    .\scripts\codex-review.ps1 -Focus "nakshatra boundary rounding and ayanamsa"

.EXAMPLE
    .\scripts\codex-review.ps1 -Versioned            # -> codex-review-2026-08-30-1452.md
#>
[CmdletBinding()]
param(
    [string]   $Focus = "",
    [string[]] $Paths = @('src', 'db', 'docs', 'decisions'),
    [string]   $Model = "",
    [string]   $OutName = "",
    [switch]   $Versioned
)

$ErrorActionPreference = "Stop"
# Keep em-dashes / Unicode intact through the pipe into the saved file.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

# Global default ChatGPT folder (see ~/.claude/CLAUDE.md), per-project subfolder.
$outDir = "D:\@ChatGPT\ikiastrro"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

if ($OutName) {
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($OutName)
} elseif ($Focus) {
    $slug = ($Focus.ToLower() -replace '[^a-z0-9]+', '-').Trim('-')
    if ($slug.Length -gt 40) { $slug = $slug.Substring(0, 40).Trim('-') }
    $baseName = "codex-review-$slug"
} else {
    $baseName = "codex-review"
}
if ($Versioned) { $baseName += '-' + (Get-Date -Format 'yyyy-MM-dd-HHmm') }
$outFile = Join-Path $outDir "$baseName.md"

# ---- collect files ----------------------------------------------------------
# Walk the repo once; keep source / SQL / docs, drop build + vendor dirs.
$skip = '[\\/](bin|obj|\.vs|\.git|_research|\.superpowers|node_modules)[\\/]'
$wantExt = @('.cs', '.csproj', '.slnx', '.sql', '.md')
$pathRx = '[\\/](' + (($Paths | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')[\\/]'
$files = Get-ChildItem -Path $repoRoot -Recurse -File |
    Where-Object {
        $wantExt -contains $_.Extension -and
        $_.FullName -notmatch $skip -and
        ($_.FullName -match $pathRx -or $_.Name -in @('README.md','Ikiastrro.slnx'))
    } |
    Sort-Object FullName

if (-not $files) { throw "No files matched. Check -Paths." }

# ---- build bundle ------------------------------------------------------------
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("PROJECT FILE TREE")
[void]$sb.AppendLine("=================")
foreach ($f in $files) {
    [void]$sb.AppendLine(($f.FullName.Substring($repoRoot.Length + 1) -replace '\\','/'))
}
[void]$sb.AppendLine()
[void]$sb.AppendLine("FILE CONTENTS")
[void]$sb.AppendLine("=============")
foreach ($f in $files) {
    $rel = ($f.FullName.Substring($repoRoot.Length + 1) -replace '\\','/')
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("----- BEGIN $rel -----")
    [void]$sb.AppendLine((Get-Content -LiteralPath $f.FullName -Raw))
    [void]$sb.AppendLine("----- END $rel -----")
}
$bundle = $sb.ToString()
$kb = [math]::Round($bundle.Length / 1KB)

$instr = @"
You are an external technical reviewer. Everything you need is in the <stdin>
text below - a .NET (C#) + SQL Server Vedic astrology horoscope generator.
Projects: Ikiastrro.Core (calculations), .Data (persistence), .Cli, .Web.

Do NOT run any commands or ask for files. Review from the bundle only.

Produce well-structured markdown covering, with file references:
1. Architecture and layering - is Core free of infra concerns, boundaries clean.
2. Calculation-correctness risks - ayanamsa, house systems, varga divisions,
   nakshatra boundaries, rounding, timezone / DST, ephemeris assumptions.
3. SQL schema and query design - normalization, indexing, the star-schema
   rules engine, migration hygiene.
4. Testability and test-coverage gaps.
5. 3-5 prioritised feature / refactor suggestions, ICE-scored (Impact,
   Confidence, Ease each 1-10) and sorted by score.
Be specific and critical; skip praise.
"@
if ($Focus) { $instr += "`nExtra focus this pass: $Focus`n" }

Write-Host "Bundle: $($files.Count) files, ~$kb KB" -ForegroundColor DarkGray
if ($kb -gt 400) {
    Write-Host "WARNING: large bundle - may exceed free-tier context. Narrow with -Paths." -ForegroundColor Yellow
}
Write-Host "Codex review -> $outFile" -ForegroundColor Cyan

$modelArgs = @()
if ($Model) { $modelArgs = @('--model', $Model) }

$bundle | & codex exec --sandbox read-only @modelArgs -C $repoRoot $instr |
    Tee-Object -FilePath $outFile -Encoding utf8

Write-Host "`nSaved: $outFile" -ForegroundColor Green
Write-Host "Next: in Claude Code, ask it to triage $outFile" -ForegroundColor Green
