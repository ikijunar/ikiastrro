---
last_updated: 2026-09-08
---

# JKD interchange utility

Standalone Windows utility for moving birth inputs between Horoscope Explorer Pro `.JKD`
files and ikiastrro. It is intentionally outside the solution until a later product feature
integrates the workflow into the application.

Only these inputs are mapped: name, date/time of birth, city/country, latitude/longitude,
and GMT/UTC offset. Horoscope Explorer calculations and settings are not imported. Its sex
field is not imported because `tbl_BirthDetails` does not currently have an equivalent.

The legacy file uses Microsoft Jet and must be opened by a 32-bit process. The script relaunches
itself under 32-bit Windows PowerShell automatically.

```powershell
.\tools\jkd-interchange\jkd-interchange.ps1 inspect "D:\path\people.JKD"
.\tools\jkd-interchange\jkd-interchange.ps1 import "D:\path\people.JKD"
.\tools\jkd-interchange\jkd-interchange.ps1 export "D:\path\ikiastrro-export.JKD"
```

Use `-SqlServer` or `-Database` to target another SQL Server/database. Export refuses to
replace a file unless `-Force` is supplied. Import is transactional and does not generate
charts; chart generation remains an explicit ikiastrro operation.
