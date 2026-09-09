SET NOCOUNT ON;
SELECT BirthDetailId, TithiNumber, PakshaCode, ElongationDegrees, IsNightBirth FROM dbo.vw_ChartMoonContext ORDER BY BirthDetailId;
SELECT BirthDetailId, ChartType, COUNT(*) AS Rows FROM dbo.vw_ChartPlanetEvidence GROUP BY BirthDetailId, ChartType ORDER BY BirthDetailId, ChartType;
SELECT BirthDetailId, COUNT(*) AS Planets FROM dbo.vw_ChartShadbala GROUP BY BirthDetailId ORDER BY BirthDetailId;
SELECT BirthDetailId, COUNT(*) AS Houses FROM dbo.vw_ChartBhavaBala GROUP BY BirthDetailId ORDER BY BirthDetailId;
SELECT BirthDetailId, EvaluationStatus, COUNT(*) AS Yogas FROM dbo.vw_ChartYogaEvaluations GROUP BY BirthDetailId, EvaluationStatus ORDER BY BirthDetailId, EvaluationStatus;
