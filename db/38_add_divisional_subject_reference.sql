USE [ikiastrro];
GO

/*
  Divisional-chart subject reference (Raman/PVR reading workflow).
  This is catalog metadata only: it does not create chart facts or change
  varga calculations.
*/
IF OBJECT_ID('dbo.tbl_Dim_DivisionalSubject', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_DivisionalSubject (
        SubjectCode                   VARCHAR(40)   NOT NULL CONSTRAINT PK_Dim_DivisionalSubject PRIMARY KEY,
        SubjectName                   NVARCHAR(80)  NOT NULL CONSTRAINT UQ_Dim_DivisionalSubject_Name UNIQUE,
        D1Foundation                  NVARCHAR(500) NOT NULL,
        PrimaryConfirmationChartId    TINYINT       NOT NULL CONSTRAINT FK_Dim_DivisionalSubject_Chart FOREIGN KEY REFERENCES dbo.tbl_Dim_ChartType (Id),
        ConfirmationAdds              NVARCHAR(500) NOT NULL,
        SortOrder                     TINYINT       NOT NULL,
        SourceRefCode                 VARCHAR(40)   NULL,
        IsActive                      BIT           NOT NULL CONSTRAINT DF_Dim_DivisionalSubject_IsActive DEFAULT 1,
        CONSTRAINT CK_Dim_DivisionalSubject_Source CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_DivisionalSubject)
INSERT dbo.tbl_Dim_DivisionalSubject
    (SubjectCode, SubjectName, D1Foundation, PrimaryConfirmationChartId, ConfirmationAdds, SortOrder, SourceRefCode)
SELECT v.SubjectCode, v.SubjectName, v.D1Foundation, ct.Id, v.ConfirmationAdds, v.SortOrder, 'SRC_PVR_INTEGRATED'
FROM (VALUES
    ('OVERALL_STRENGTH_DHARMA', N'Overall strength and dharma', N'Lagna, Lagna lord, Sun, 9th house and their dignity, aspects and lordship.', 'D9', N'Planetary maturity, inner strength, dharma and the deeper expression of the D1 promise.', 1),
    ('WEALTH', N'Wealth', N'2nd and 11th houses, their lords, Jupiter, Venus and links to income or assets.', 'D2', N'Capacity to accumulate, preserve and use money and resources.', 2),
    ('SIBLINGS_COURAGE', N'Siblings and courage', N'3rd house, 3rd lord, Mars and effort-related combinations.', 'D3', N'Siblings, co-born relationships, courage, initiative and sustained effort.', 3),
    ('PROPERTY_RESIDENCE', N'Property and residence', N'4th house, 4th lord, Moon and fixed-asset combinations.', 'D4', N'Residence, houses, land, property ownership and fortune connected with assets.', 4),
    ('CHILDREN_PROGENY', N'Children and progeny', N'5th house, 5th lord, Jupiter and progeny combinations.', 'D7', N'Children, fertility, progeny and the relationship with children.', 5),
    ('MOTHER_PARENTS', N'Mother and parental lineage', N'4th house and Moon for mother; 9th/10th and family factors for parents.', 'D12', N'Parents, ancestry and inherited family patterns; read with the D1 4th and 9th houses.', 6),
    ('MARRIAGE_RELATIONSHIPS', N'Marriage and relationships', N'7th house, 7th lord, Venus and partnership combinations.', 'D9', N'Spouse, marriage quality, relationship dharma and long-term partnership.', 7),
    ('CAREER_STATUS', N'Career and status', N'10th house, 10th lord, Sun, Saturn and public-action combinations.', 'D10', N'Profession, authority, achievements, recognition and activity in society.', 8),
    ('VEHICLES_COMFORTS', N'Vehicles and comforts', N'4th house, Venus, Moon and comfort-related combinations.', 'D16', N'Vehicles, pleasures, comforts and the ability to enjoy material conveniences.', 9),
    ('EDUCATION_LEARNING', N'Education and learning', N'4th, 5th and 9th houses; Mercury, Jupiter and knowledge combinations.', 'D24', N'Formal education, learning, scholarship, examinations and mastery.', 10),
    ('KARMIC_ROOTS', N'Karmic roots', N'D1 promise, major life indicators and the condition of the relevant lords and karakas.', 'D60', N'Deep karmic causes and subtle confirmation of major life patterns; birth-time sensitive.', 11)
) v (SubjectCode, SubjectName, D1Foundation, ChartCode, ConfirmationAdds, SortOrder)
JOIN dbo.tbl_Dim_ChartType ct ON ct.Code = v.ChartCode;
GO

IF OBJECT_ID('dbo.tbl_Dim_InterpretationDimension', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_InterpretationDimension (
        DimensionCode VARCHAR(32)   NOT NULL CONSTRAINT PK_Dim_InterpretationDimension PRIMARY KEY,
        DimensionName NVARCHAR(80)  NOT NULL CONSTRAINT UQ_Dim_InterpretationDimension_Name UNIQUE,
        Description   NVARCHAR(400) NOT NULL,
        SortOrder     TINYINT       NOT NULL,
        SourceRefCode VARCHAR(40)   NULL,
        IsActive      BIT           NOT NULL CONSTRAINT DF_Dim_InterpretationDimension_IsActive DEFAULT 1,
        CONSTRAINT CK_Dim_InterpretationDimension_Source CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_InterpretationDimension)
INSERT dbo.tbl_Dim_InterpretationDimension
    (DimensionCode, DimensionName, Description, SortOrder, SourceRefCode)
VALUES
    ('PLANETARY_DIGNITY',       N'Planetary dignity',       N'Assess exaltation, debilitation, own sign, moolatrikona, friendship, combustion and other strength indicators in the chart under review.', 1, 'SRC_PVR_INTEGRATED'),
    ('DISPOSITOR_RELATION',     N'Dispositor relation',     N'Follow the result through the lord of the sign occupied by the planet and evaluate that dispositor''s house, sign, dignity and relationships.', 2, 'SRC_PVR_INTEGRATED'),
    ('HOUSE_LORD_COMBINATION',  N'House-lord combination',  N'Combine every house owned by a planet, then assess conjunction, aspect, exchange, kendra-trikona links and functional nature.', 3, 'SRC_PVR_INTEGRATED'),
    ('DIVISIONAL_CONFIRMATION', N'Divisional confirmation', N'Confirm the D1 promise in the primary subject Varga by reading that Varga''s Lagna, houses, lords, karakas and planetary dignity.', 4, 'SRC_PVR_INTEGRATED');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Dim_DivisionalSubject')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Dim_DivisionalSubject', 'VARGA_REFERENCE', 'SUBJECT_TO_VARGA', 'Reference mapping from a life subject to its D1 foundation and primary confirmation divisional chart.', '38_add_divisional_subject_reference.sql');
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Dim_InterpretationDimension')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Dim_InterpretationDimension', 'INTERPRETATION', 'DIMENSION_CATALOG', 'Shared interpretation dimensions applied to house-lord and divisional-chart readings.', '38_add_divisional_subject_reference.sql');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '38_add_divisional_subject_reference.sql')
    INSERT dbo.SchemaMigrations (ScriptName, AppliedAtUtc, Note)
    VALUES ('38_add_divisional_subject_reference.sql', SYSUTCDATETIME(), 'Adds D1 foundation, primary confirmation Varga and confirmation-adds metadata, plus the four interpretation dimensions.');
GO
