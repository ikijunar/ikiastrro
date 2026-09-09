[CmdletBinding()]
param(
    [Parameter(Mandatory,Position=0)][ValidateSet('export','import')][string]$Command,
    [Parameter(Mandatory,Position=1)][string]$SourcePath,
    [Parameter(Mandatory,Position=2)][string]$DestinationPath,
    [switch]$Force
)
$ErrorActionPreference='Stop'

if([Environment]::Is64BitProcess){
    $host32="$env:WINDIR\SysWOW64\WindowsPowerShell\v1.0\powershell.exe"
    $arguments=@('-NoProfile','-ExecutionPolicy','Bypass','-File',$PSCommandPath,$Command,$SourcePath,$DestinationPath)
    if($Force){$arguments+='-Force'}
    & $host32 @arguments
    exit $LASTEXITCODE
}

# FUNCTIONS
