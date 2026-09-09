[CmdletBinding()]
param(
    [Parameter(Mandatory, Position=0)][ValidateSet('inspect','import','export')][string]$Command,
    [Parameter(Mandatory, Position=1)][string]$Path,
    [string]$SqlServer='localhost\SQLSERVER2025',
    [string]$Database='ikiastrro',
    [switch]$Force
)
$ErrorActionPreference='Stop'

# Jet 4 is installed only in-process at 32 bit on this machine.
if ([Environment]::Is64BitProcess) {
    $host32="$env:WINDIR\SysWOW64\WindowsPowerShell\v1.0\powershell.exe"
    $a=@('-NoProfile','-ExecutionPolicy','Bypass','-File',$PSCommandPath,$Command,$Path,
        '-SqlServer',$SqlServer,'-Database',$Database)
    if($Force){$a+='-Force'}
    & $host32 @a
    exit $LASTEXITCODE
}

function Open-Jkd($p){
    $c=New-Object System.Data.OleDb.OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$p")
    $c.Open(); $c
}
function Open-Sql {
    $c=New-Object System.Data.SqlClient.SqlConnection(
        "Server=$SqlServer;Database=$Database;Integrated Security=True;TrustServerCertificate=True")
    $c.Open(); $c
}
function Read-Jkd($c){
    $q=$c.CreateCommand(); $q.CommandText='SELECT * FROM Person'
    $da=New-Object System.Data.OleDb.OleDbDataAdapter($q); $dt=New-Object System.Data.DataTable
    [void]$da.Fill($dt); $dt.Rows
}
function Int($v,$field,$name){
    $n=0; if(-not [int]::TryParse(([string]$v).Trim(),[ref]$n)){throw "Invalid $field '$v' for '$name'."}; $n
}
function Coord($deg,$min,$dir,$name){
    $d=Int $deg 'coordinate degrees' $name; $m=Int $min 'coordinate minutes' $name
    if($m -notin 0..59){throw "Invalid coordinate minutes for '$name'."}
    $v=$d+$m/60.0; if(([string]$dir).Trim().ToUpperInvariant() -in @('S','W')){$v=-$v}; $v
}
function Offset($v,$name){
    $s=([string]$v).Trim(); if($s -notmatch '^([+-])(\d{1,2})[\.:](\d{2})$'){throw "Invalid GMT offset '$s' for '$name'."}
    '{0}{1:00}:{2:00}:00' -f $Matches[1],[int]$Matches[2],[int]$Matches[3]
}
function Person($r){
    $n=([string]$r.Name).Trim(); if(!$n){throw 'A JKD record has an empty name.'}
    $y=Int $r.Year year $n; $m=Int $r.Month month $n; $d=Int $r.Day day $n
    $h=Int $r.Hour hour $n; $mi=Int $r.Minutes minutes $n; $s=Int $r.Seconds seconds $n
    [pscustomobject]@{Name=$n;Date=[DateTime]::new($y,$m,$d);Time=[TimeSpan]::new($h,$mi,$s)
        City=([string]$r.City).Trim();Country=([string]$r.Country).Trim()
        Lat=Coord $r.LatDeg $r.LatMT $r.TextNS $n;Lon=Coord $r.LongDeg $r.LongMT $r.TextEW $n
        UtcOffset=Offset $r.GMTDIFF $n}
}
function Param($q,$n,$v){[void]$q.Parameters.AddWithValue($n,$v)}
function Parts([double]$v,$pos,$neg){
    $a=[Math]::Abs($v);$d=[Math]::Floor($a);$m=[Math]::Round(($a-$d)*60)
    if($m -eq 60){$d++;$m=0}; [pscustomobject]@{D=[int]$d;M=[int]$m;Dir=$(if($v-lt 0){$neg}else{$pos})}
}

$full=[IO.Path]::GetFullPath($Path)
if($Command -eq 'inspect'){
    $j=Open-Jkd $full; try{$rows=@(Read-Jkd $j);$ok=0;$bad=0
        foreach($r in $rows){try{[void](Person $r);$ok++}catch{$bad++;Write-Warning $_.Exception.Message}}
        "Records: $($rows.Count); valid: $ok; invalid: $bad"}finally{$j.Close()};return
}
if($Command -eq 'import'){
    if(!(Test-Path -LiteralPath $full)){throw "JKD file not found: $full"}
    $j=Open-Jkd $full;$db=Open-Sql;$tx=$db.BeginTransaction();$added=0;$skipped=0
    try{foreach($r in @(Read-Jkd $j)){$p=Person $r;$q=$db.CreateCommand();$q.Transaction=$tx
            $q.CommandText='SELECT COUNT(1) FROM dbo.tbl_BirthDetails WHERE Name=@Name';Param $q '@Name' $p.Name
            if([int]$q.ExecuteScalar()-gt 0){$skipped++;continue}
            $q=$db.CreateCommand();$q.Transaction=$tx;$q.CommandText=@'
INSERT INTO dbo.tbl_BirthDetails
(Name,DateOfBirth,TimeOfBirth,PlaceCity,PlaceCountry,Latitude,Longitude,UtcOffset,IanaTimeZoneId,CreatedAt)
VALUES(@Name,@Date,@Time,@City,@Country,@Lat,@Lon,@Offset,NULL,SYSUTCDATETIME())
'@
            Param $q '@Name' $p.Name;Param $q '@Date' $p.Date;Param $q '@Time' $p.Time
            Param $q '@City' $p.City;Param $q '@Country' $p.Country;Param $q '@Lat' $p.Lat
            Param $q '@Lon' $p.Lon;Param $q '@Offset' $p.UtcOffset;[void]$q.ExecuteNonQuery();$added++}
        $tx.Commit();"Imported $added record(s); skipped $skipped existing name(s)."
    }catch{$tx.Rollback();throw}finally{$j.Close();$db.Close()};return
}

if(Test-Path -LiteralPath $full){if(!$Force){throw "Target exists: $full (use -Force to replace)."};Remove-Item -LiteralPath $full -Force}
$db=Open-Sql;$j=$null
try{$q=$db.CreateCommand();$q.CommandText='SELECT Name,DateOfBirth,TimeOfBirth,PlaceCity,PlaceCountry,Latitude,Longitude,UtcOffset FROM dbo.tbl_BirthDetails ORDER BY Name'
    $da=New-Object System.Data.SqlClient.SqlDataAdapter($q);$dt=New-Object System.Data.DataTable;[void]$da.Fill($dt)
    $cat=New-Object -ComObject ADOX.Catalog;$cat.Create("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$full;Jet OLEDB:Engine Type=5")
    [void][Runtime.InteropServices.Marshal]::ReleaseComObject($cat);$j=Open-Jkd $full;$q=$j.CreateCommand();$q.CommandText=@'
CREATE TABLE Person ([Name] TEXT(100),[Sex] TEXT(2),[Day] TEXT(2),[Month] TEXT(2),[Year] TEXT(4),[Hour] TEXT(2),[Minutes] TEXT(2),[Seconds] TEXT(2),[AMPM] TEXT(2),[Country] TEXT(100),[LatDeg] TEXT(3),[LatMT] TEXT(2),[TextNS] TEXT(1),[LongDeg] TEXT(3),[LongMT] TEXT(2),[TextEW] TEXT(1),[City] TEXT(150),[GMTDIFF] TEXT(6))
'@;[void]$q.ExecuteNonQuery()
    foreach($p in $dt.Rows){$lat=Parts ([double]$p.Latitude) N S;$lon=Parts ([double]$p.Longitude) E W
        $date=[DateTime]$p.DateOfBirth;$time=[TimeSpan]$p.TimeOfBirth;$off=([string]$p.UtcOffset)-replace ':','.';$off=$off.Substring(0,[Math]::Min(6,$off.Length))
        $q=$j.CreateCommand();$q.CommandText='INSERT INTO Person VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)'
        foreach($v in @([string]$p.Name,'',$date.ToString('dd'),$date.ToString('MM'),$date.ToString('yyyy'),
            ('{0:00}'-f $time.Hours),('{0:00}'-f $time.Minutes),('{0:00}'-f $time.Seconds),$(if($time.Hours-lt 12){'1'}else{'2'}),
            [string]$p.PlaceCountry,('{0:000}'-f $lat.D),('{0:00}'-f $lat.M),$lat.Dir,
            ('{0:000}'-f $lon.D),('{0:00}'-f $lon.M),$lon.Dir,[string]$p.PlaceCity,$off)){Param $q '@p' $v};[void]$q.ExecuteNonQuery()}
    "Exported $($dt.Rows.Count) record(s) to $full"
}finally{if($j){$j.Close()};$db.Close()}
