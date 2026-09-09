-- 41 - Explicit D1/D9 Vargottama facts.
USE [ikiastrro];
GO
IF OBJECT_ID('dbo.tbl_Fact_Vargottama', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Fact_Vargottama (
        Id            INT IDENTITY(1,1) CONSTRAINT PK_Fact_Vargottama PRIMARY KEY,
        ChartResultId INT NOT NULL CONSTRAINT FK_Fact_Vargottama_ChartResult REFERENCES dbo.tbl_ChartResults(Id),
        PlanetId      TINYINT NULL CONSTRAINT FK_Fact_Vargottama_Planet REFERENCES dbo.tbl_Planets(Id),
        PlanetCode    VARCHAR(20) NOT NULL,
        D1Sign        VARCHAR(20) NOT NULL,
        D9Sign        VARCHAR(20) NOT NULL,
        IsVargottama  BIT NOT NULL,
        RuleSetId     TINYINT NOT NULL CONSTRAINT FK_Fact_Vargottama_RuleSet REFERENCES dbo.tbl_Rule_Sets(Id),
        SourceRefCode VARCHAR(40) NULL,
        CONSTRAINT CK_Fact_Vargottama_Source CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Fact_Vargottama UNIQUE (ChartResultId, PlanetCode)
    );
    CREATE INDEX IX_Fact_Vargottama_Chart ON dbo.tbl_Fact_Vargottama (ChartResultId, IsVargottama);
END
GO

MERGE dbo.tbl_Astro_Terminology AS tgt
USING (VALUES ('Concept','VARGOTTAMA',CONVERT(VARCHAR(40),NULL),'STRENGTH',CONVERT(INT,NULL),975))
       src (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder)
ON tgt.Code = src.Code
WHEN MATCHED THEN UPDATE SET Category=src.Category, EngineCode=src.EngineCode, DisplayOrder=src.DisplayOrder, IsActive=1
WHEN NOT MATCHED THEN INSERT (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder, IsActive)
    VALUES (src.Category, src.Code, src.ParentCode, src.EngineCode, src.NumericKey, src.DisplayOrder, 1);
GO
MERGE dbo.tbl_Astro_TerminologyText AS tgt
USING (SELECT t.TerminologyId, v.LanguageCode, v.Script, v.Name, v.TraditionalName, v.ShortDescription
       FROM (VALUES
         ('VARGOTTAMA','sa','Latn',N'Vargottama',N'Vargottama',N'D1 and D9 occupy the same sign.'),
         ('VARGOTTAMA','en','Latn',N'Vargottama',NULL,N'The D1 and D9 signs are identical, indicating reinforced varga expression.')
       ) v(Code, LanguageCode, Script, Name, TraditionalName, ShortDescription)
       JOIN dbo.tbl_Astro_Terminology t ON t.Code=v.Code) src
ON tgt.TerminologyId=src.TerminologyId AND tgt.LanguageCode=src.LanguageCode AND tgt.Script=src.Script
WHEN MATCHED THEN UPDATE SET Name=src.Name, TraditionalName=src.TraditionalName, ShortDescription=src.ShortDescription
WHEN NOT MATCHED THEN INSERT (TerminologyId, LanguageCode, Script, Name, TraditionalName, ShortDescription)
    VALUES (src.TerminologyId, src.LanguageCode, src.Script, src.Name, src.TraditionalName, src.ShortDescription);
GO
INSERT dbo.SchemaMigrations (ScriptName, AppliedAtUtc, Note)
SELECT '41_add_vargottama_facts.sql', SYSUTCDATETIME(), 'Stores explicit D1/D9 same-sign Vargottama facts for planets and Lagna.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName='41_add_vargottama_facts.sql');
GO
