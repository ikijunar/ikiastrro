-- =====================================================================
-- 182 - Normalized hierarchical tags for the 4,900 historic horoscopes.
--
-- AreaCode/SubCategoryCode/GeoAreaCode remain the compatible primary
-- classification. Tags add multi-axis discovery without duplicating text
-- on every figure. CategoryLevel and parent IDs make the hierarchy explicit.
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.tbl_Dim_HistoricTagCategories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_HistoricTagCategories (
        Id               INT IDENTITY(1,1) CONSTRAINT PK_Dim_HistoricTagCategories PRIMARY KEY,
        Code             VARCHAR(32) NOT NULL CONSTRAINT UQ_Dim_HistoricTagCategories_Code UNIQUE,
        Name             NVARCHAR(80) NOT NULL,
        CategoryLevel    TINYINT NOT NULL,
        ParentCategoryId INT NULL CONSTRAINT FK_Dim_HistoricTagCategories_Parent
                             REFERENCES dbo.tbl_Dim_HistoricTagCategories (Id),
        DisplayOrder     SMALLINT NOT NULL,
        IsActive         BIT NOT NULL CONSTRAINT DF_Dim_HistoricTagCategories_Active DEFAULT 1,
        CONSTRAINT CK_Dim_HistoricTagCategories_Level CHECK (CategoryLevel BETWEEN 1 AND 4)
    );
END;
GO

IF OBJECT_ID('dbo.tbl_Dim_HistoricTags', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_HistoricTags (
        Id          INT IDENTITY(1,1) CONSTRAINT PK_Dim_HistoricTags PRIMARY KEY,
        CategoryId  INT NOT NULL CONSTRAINT FK_Dim_HistoricTags_Category
                        REFERENCES dbo.tbl_Dim_HistoricTagCategories (Id),
        Code        VARCHAR(80) NOT NULL CONSTRAINT UQ_Dim_HistoricTags_Code UNIQUE,
        Name        NVARCHAR(120) NOT NULL,
        ParentTagId INT NULL CONSTRAINT FK_Dim_HistoricTags_Parent REFERENCES dbo.tbl_Dim_HistoricTags (Id),
        DisplayOrder SMALLINT NOT NULL,
        IsActive    BIT NOT NULL CONSTRAINT DF_Dim_HistoricTags_Active DEFAULT 1
    );
END;
GO

IF OBJECT_ID('dbo.tbl_Dim_HistoricFigureTags', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_HistoricFigureTags (
        Id               INT IDENTITY(1,1) CONSTRAINT PK_Dim_HistoricFigureTags PRIMARY KEY,
        HistoricFigureId INT NOT NULL CONSTRAINT FK_Dim_HistoricFigureTags_Figure
                             REFERENCES dbo.tbl_Ref_HistoricFigure (Id) ON DELETE CASCADE,
        HistoricTagId    INT NOT NULL CONSTRAINT FK_Dim_HistoricFigureTags_Tag
                             REFERENCES dbo.tbl_Dim_HistoricTags (Id),
        AssignmentMethod VARCHAR(12) NOT NULL,
        SourceProperty   VARCHAR(32) NULL,
        CONSTRAINT UQ_Dim_HistoricFigureTags_FigureTag UNIQUE (HistoricFigureId, HistoricTagId),
        CONSTRAINT CK_Dim_HistoricFigureTags_Method CHECK (AssignmentMethod IN ('DERIVED','WIKIDATA','MANUAL'))
    );
END;
GO

;WITH CategorySeed (Code, Name, CategoryLevel, ParentCode, DisplayOrder) AS (
    SELECT * FROM (VALUES
        ('DOMAIN', N'Domain', 1, CAST(NULL AS VARCHAR(32)), 10),
        ('SPECIALTY', N'Specialty', 2, 'DOMAIN', 20),
        ('REACH', N'Geographic reach', 1, NULL, 30),
        ('DEMOGRAPHIC', N'Demographic', 1, NULL, 40),
        ('SEX', N'Sex', 2, 'DEMOGRAPHIC', 41),
        ('TIME', N'Time', 1, NULL, 50),
        ('BIRTH_ERA', N'Birth era', 2, 'TIME', 51),
        ('PROMINENCE', N'Prominence', 1, NULL, 60),
        ('NOTABILITY', N'Wikipedia notability', 2, 'PROMINENCE', 61),
        ('PROVENANCE', N'Provenance', 1, NULL, 70),
        ('SOURCE', N'Source', 2, 'PROVENANCE', 71),
        ('DATA_QUALITY', N'Data quality', 1, NULL, 80),
        ('BIRTH_TIME', N'Birth-time quality', 2, 'DATA_QUALITY', 81),
        ('GEO_DATA', N'Birthplace data', 2, 'DATA_QUALITY', 82),
        ('REVIEW', N'Review status', 2, 'DATA_QUALITY', 83),
        ('CALENDAR', N'Original calendar', 2, 'DATA_QUALITY', 84)
    ) v (Code, Name, CategoryLevel, ParentCode, DisplayOrder)
)
MERGE dbo.tbl_Dim_HistoricTagCategories AS t
USING CategorySeed AS s ON t.Code = s.Code
WHEN MATCHED THEN UPDATE SET Name=s.Name, CategoryLevel=s.CategoryLevel, DisplayOrder=s.DisplayOrder, IsActive=1
WHEN NOT MATCHED THEN INSERT (Code, Name, CategoryLevel, DisplayOrder) VALUES (s.Code,s.Name,s.CategoryLevel,s.DisplayOrder);
GO

UPDATE c SET ParentCategoryId=p.Id
FROM dbo.tbl_Dim_HistoricTagCategories c
JOIN (VALUES ('SPECIALTY','DOMAIN'),('SEX','DEMOGRAPHIC'),('BIRTH_ERA','TIME'),('NOTABILITY','PROMINENCE'),
             ('SOURCE','PROVENANCE'),('BIRTH_TIME','DATA_QUALITY'),('GEO_DATA','DATA_QUALITY'),
             ('REVIEW','DATA_QUALITY'),('CALENDAR','DATA_QUALITY')) x (ChildCode, ParentCode) ON x.ChildCode=c.Code
JOIN dbo.tbl_Dim_HistoricTagCategories p ON p.Code=x.ParentCode;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RefreshHistoricFigureTags
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Seed TABLE (CategoryCode VARCHAR(32), Code VARCHAR(80), Name NVARCHAR(120), ParentCode VARCHAR(80), DisplayOrder SMALLINT);
    INSERT @Seed
    SELECT 'DOMAIN', 'DOMAIN_' + a.AreaCode, a.AreaName, NULL, a.DisplayOrder FROM dbo.tbl_Dim_HistoricArea a
    UNION ALL
    SELECT 'SPECIALTY', 'SPECIALTY_' + s.AreaCode + '_' + s.SubCategoryCode, s.SubCategoryName,
           'DOMAIN_' + s.AreaCode, s.DisplayOrder FROM dbo.tbl_Dim_HistoricSubCategory s
    UNION ALL
    SELECT 'REACH', 'REACH_' + g.GeoAreaCode, g.GeoAreaName, NULL, g.DisplayOrder FROM dbo.tbl_Dim_HistoricGeoArea g
    UNION ALL SELECT * FROM (VALUES
      ('SEX','SEX_FEMALE',N'Female',NULL,1),('SEX','SEX_MALE',N'Male',NULL,2),('SEX','SEX_OTHER',N'Other / unspecified',NULL,3),
      ('BIRTH_ERA','ERA_PRE_1500',N'Born before 1500',NULL,1),('BIRTH_ERA','ERA_1500_1799',N'Born 1500-1799',NULL,2),
      ('BIRTH_ERA','ERA_1800_1899',N'Born 1800-1899',NULL,3),('BIRTH_ERA','ERA_1900_1949',N'Born 1900-1949',NULL,4),
      ('BIRTH_ERA','ERA_1950_1999',N'Born 1950-1999',NULL,5),('BIRTH_ERA','ERA_2000_PLUS',N'Born 2000 or later',NULL,6),
      ('NOTABILITY','NOTABILITY_GLOBAL',N'Global (120+ sitelinks)',NULL,1),('NOTABILITY','NOTABILITY_HIGH',N'High (60-119 sitelinks)',NULL,2),
      ('NOTABILITY','NOTABILITY_ESTABLISHED',N'Established (20-59 sitelinks)',NULL,3),('NOTABILITY','NOTABILITY_DOCUMENTED',N'Documented (6-19 sitelinks)',NULL,4),
      ('SOURCE','SOURCE_WIKIDATA',N'Wikidata',NULL,1),
      ('BIRTH_TIME','TIME_RECORDED',N'Recorded birth time',NULL,1),('BIRTH_TIME','TIME_APPROXIMATE',N'Approximate birth time',NULL,2),
      ('BIRTH_TIME','TIME_RECTIFIED',N'Rectified birth time',NULL,3),('BIRTH_TIME','TIME_UNKNOWN',N'Unknown birth time',NULL,4),
      ('GEO_DATA','GEO_COORDINATES_COMPLETE',N'Birthplace coordinates complete',NULL,1),('GEO_DATA','GEO_COORDINATES_MISSING',N'Birthplace coordinates missing',NULL,2),
      ('REVIEW','REVIEW_VERIFIED',N'Verified',NULL,1),('REVIEW','REVIEW_DISPUTED',N'Disputed',NULL,2),('REVIEW','REVIEW_UNREVIEWED',N'Unreviewed',NULL,3),
      ('CALENDAR','CALENDAR_GREGORIAN',N'Gregorian source date',NULL,1),('CALENDAR','CALENDAR_JULIAN',N'Julian source date',NULL,2)
    ) v(CategoryCode,Code,Name,ParentCode,DisplayOrder);

    MERGE dbo.tbl_Dim_HistoricTags AS t
    USING (SELECT c.Id CategoryId,s.Code,s.Name,s.ParentCode,s.DisplayOrder FROM @Seed s JOIN dbo.tbl_Dim_HistoricTagCategories c ON c.Code=s.CategoryCode) s
       ON t.Code=s.Code
    WHEN MATCHED THEN UPDATE SET CategoryId=s.CategoryId,Name=s.Name,DisplayOrder=s.DisplayOrder,IsActive=1
    WHEN NOT MATCHED THEN INSERT(CategoryId,Code,Name,DisplayOrder) VALUES(s.CategoryId,s.Code,s.Name,s.DisplayOrder);

    UPDATE t SET ParentTagId=p.Id FROM dbo.tbl_Dim_HistoricTags t JOIN @Seed s ON s.Code=t.Code JOIN dbo.tbl_Dim_HistoricTags p ON p.Code=s.ParentCode;
    DELETE FROM dbo.tbl_Dim_HistoricFigureTags WHERE AssignmentMethod='DERIVED';

    ;WITH Assignments AS (
      SELECT f.Id FigureId,'DOMAIN_'+f.AreaCode TagCode,'AreaCode' Prop FROM dbo.tbl_Ref_HistoricFigure f UNION ALL
      SELECT f.Id,'SPECIALTY_'+f.AreaCode+'_'+f.SubCategoryCode,'SubCategoryCode' FROM dbo.tbl_Ref_HistoricFigure f UNION ALL
      SELECT f.Id,'REACH_'+f.GeoAreaCode,'GeoAreaCode' FROM dbo.tbl_Ref_HistoricFigure f UNION ALL
      SELECT f.Id,CASE WHEN LOWER(f.Sex) IN ('female','woman') THEN 'SEX_FEMALE' WHEN LOWER(f.Sex) IN ('male','man') THEN 'SEX_MALE' ELSE 'SEX_OTHER' END,'Sex' FROM dbo.tbl_Ref_HistoricFigure f UNION ALL
      SELECT f.Id,CASE WHEN YEAR(f.DateOfBirth)<1500 THEN 'ERA_PRE_1500' WHEN YEAR(f.DateOfBirth)<1800 THEN 'ERA_1500_1799' WHEN YEAR(f.DateOfBirth)<1900 THEN 'ERA_1800_1899' WHEN YEAR(f.DateOfBirth)<1950 THEN 'ERA_1900_1949' WHEN YEAR(f.DateOfBirth)<2000 THEN 'ERA_1950_1999' ELSE 'ERA_2000_PLUS' END,'DateOfBirth' FROM dbo.tbl_Ref_HistoricFigure f UNION ALL
      SELECT f.Id,CASE WHEN f.SiteLinkCount>=120 THEN 'NOTABILITY_GLOBAL' WHEN f.SiteLinkCount>=60 THEN 'NOTABILITY_HIGH' WHEN f.SiteLinkCount>=20 THEN 'NOTABILITY_ESTABLISHED' ELSE 'NOTABILITY_DOCUMENTED' END,'SiteLinkCount' FROM dbo.tbl_Ref_HistoricFigure f UNION ALL
      SELECT f.Id,'SOURCE_WIKIDATA','SourceCode' FROM dbo.tbl_Ref_HistoricFigure f WHERE f.SourceCode='WIKIDATA' UNION ALL
      SELECT f.Id,'TIME_'+f.TimeQuality,'TimeQuality' FROM dbo.tbl_Ref_HistoricFigure f UNION ALL
      SELECT f.Id,CASE WHEN f.Latitude IS NULL THEN 'GEO_COORDINATES_MISSING' ELSE 'GEO_COORDINATES_COMPLETE' END,'Latitude' FROM dbo.tbl_Ref_HistoricFigure f UNION ALL
      SELECT f.Id,'REVIEW_'+f.ReviewStatus,'ReviewStatus' FROM dbo.tbl_Ref_HistoricFigure f UNION ALL
      SELECT f.Id,'CALENDAR_'+f.OriginalCalendar,'OriginalCalendar' FROM dbo.tbl_Ref_HistoricFigure f
    )
    INSERT dbo.tbl_Dim_HistoricFigureTags(HistoricFigureId,HistoricTagId,AssignmentMethod,SourceProperty)
    SELECT a.FigureId,t.Id,'DERIVED',a.Prop FROM Assignments a JOIN dbo.tbl_Dim_HistoricTags t ON t.Code=a.TagCode;
END;
GO

EXEC dbo.usp_RefreshHistoricFigureTags;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName='182_add_historic_figure_tags.sql')
    INSERT dbo.SchemaMigrations(ScriptName,AppliedAtUtc,Note) VALUES('182_add_historic_figure_tags.sql',SYSUTCDATETIME(),'Adds hierarchical multi-category tags and refreshes tags for all historic figures.');
GO
