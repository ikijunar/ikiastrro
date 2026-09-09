IF OBJECT_ID(N'dbo.tbl_TransitPositionReference', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_TransitPositionReference
    (
        TransitPositionReferenceId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_TransitPositionReference PRIMARY KEY,
        PlanetId TINYINT NOT NULL,
        AsOfUtc DATETIME2(0) NOT NULL,
        SignId TINYINT NOT NULL,
        LongitudeDegrees DECIMAL(12,8) NOT NULL,
        DegreeInSign DECIMAL(10,8) NOT NULL,
        NakshatraId TINYINT NULL,
        Pada TINYINT NULL,
        SpeedDegreesPerDay DECIMAL(12,8) NOT NULL,
        MotionDirection VARCHAR(10) NOT NULL,
        AyanamsaRuleId INT NOT NULL,
        InSignSinceUtc DATETIME2(0) NULL,
        NextChangeUtc DATETIME2(0) NULL,
        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_tbl_TransitPositionReference_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_tbl_TransitPositionReference_Planet_AsOf UNIQUE (PlanetId, AsOfUtc),
        CONSTRAINT CK_tbl_TransitPositionReference_Longitude CHECK (LongitudeDegrees >= 0 AND LongitudeDegrees < 360),
        CONSTRAINT CK_tbl_TransitPositionReference_Sign CHECK (SignId BETWEEN 1 AND 12),
        CONSTRAINT CK_tbl_TransitPositionReference_Degree CHECK (DegreeInSign >= 0 AND DegreeInSign < 30),
        CONSTRAINT CK_tbl_TransitPositionReference_Motion CHECK (MotionDirection IN ('Direct','Retrograde','Stationary')),
        CONSTRAINT FK_tbl_TransitPositionReference_Nakshatra FOREIGN KEY (NakshatraId) REFERENCES dbo.tbl_Nakshatras(Id),
        CONSTRAINT FK_tbl_TransitPositionReference_Ayanamsa FOREIGN KEY (AyanamsaRuleId) REFERENCES dbo.tbl_Rule_Ayanamsa(Id)
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_TransitPositionReference_AsOf' AND object_id = OBJECT_ID(N'dbo.tbl_TransitPositionReference'))
    CREATE INDEX IX_tbl_TransitPositionReference_AsOf ON dbo.tbl_TransitPositionReference (AsOfUtc, PlanetId);
IF OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = N'45_create_transit_position_reference.sql')
    INSERT dbo.SchemaMigrations (ScriptName, Note) VALUES (N'45_create_transit_position_reference.sql', N'Persist exact transit positions calculated for requested timestamps.');
