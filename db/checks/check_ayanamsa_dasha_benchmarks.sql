-- Read-only inspection of migration 46 benchmark data.
SELECT c.Code, c.PersonName, c.BirthDate, c.BirthTime, c.UtcOffset,
       c.LatitudeDegrees, c.LongitudeDegrees, c.ReferenceAyanamsaDegrees,
       c.VimshottariDaysPerYear, c.ReferenceSourceRefCode
FROM dbo.tbl_Dim_AyanamsaBenchmarkCases c
ORDER BY c.Code;

SELECT c.Code AS BenchmarkCode, p.BodyCode, p.SiderealLongitudeDegrees
FROM dbo.tbl_Dim_AyanamsaBenchmarkPositions p
JOIN dbo.tbl_Dim_AyanamsaBenchmarkCases c ON c.Id = p.AyanamsaBenchmarkCaseId
ORDER BY c.Code, p.Id;

SELECT Code, DisplayName, Family, IsConditional, PrimaryUse, ImplementationStatus, SourceRefCode
FROM dbo.tbl_Dim_DashaSystems
ORDER BY Id;

SELECT c.Code AS BenchmarkCode, s.Code AS DashaSystemCode, p.LevelNumber,
       p.SequenceNumber, p.LordCode, p.StartDate
FROM dbo.tbl_Dim_DashaBenchmarkPeriods p
JOIN dbo.tbl_Dim_AyanamsaBenchmarkCases c ON c.Id = p.AyanamsaBenchmarkCaseId
JOIN dbo.tbl_Dim_DashaSystems s ON s.Id = p.DashaSystemId
ORDER BY c.Code, s.Code, p.LevelNumber, p.SequenceNumber;
