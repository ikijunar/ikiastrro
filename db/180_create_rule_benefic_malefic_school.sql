-- 180 - Planet benefic/malefic by school (Astro Facts "Benefic / malefic").
-- docs/research/domain/planet-benefic-malefic-perspectives.md sections 2 and 3: the schools disagree on what a kendra
-- lordship does and on where a benefic or malefic does well, so each school's rule is a row, cited, not a code branch.
--
--   tbl_Dim_BeneficMaleficSchools    - the schools shown as columns (5 rows).
--   tbl_Rule_BeneficMaleficSchool    - LORDSHIP rows (functional nature a lord of house H takes; BPHS ch.34, Raman) and
--                                      PLACEMENT rows (how a planet of a given nature does in house H; PVR, Saravali, BPHS,
--                                      Phaladeepika). A missing row means the school states nothing - never "neutral".
--   PVR's lordship verdict is NOT here: it is Table 30, read by Core LagnaFunctionalNature.
--
-- Rows read from the local library (not the web), page/verse in each Note. Not seeded on purpose: the web reading of BPHS
-- 74.25 that benefics in the 6th/12th are good - it contradicts Saravali and the Sanskrit compound is ambiguous
-- (research note section 3); add it only after checking Santhanam.
-- Apply with: sqlcmd -S "localhost\SQLSERVER2025" -E -d ikiastrro -f 65001 -i db/180_create_rule_benefic_malefic_school.sql
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Saravali is cited by 24 rows below; it had no registry row.
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_Source WHERE Code = 'SRC_SARAVALI')
    INSERT dbo.tbl_Dim_Source (Code, Title, Author, Edition, Tradition, Notes, IsActive)
    VALUES ('SRC_SARAVALI', 'Saravali', 'Kalyana Varma', NULL, 'classical',
           'Planetary nature (ch.3 v.9) and effects of planets in bhavas (ch.30 v.86-87: malefics harm every house except the 6th, 8th and 12th, where they are auspicious; benefics the reverse). Local: `D:\Vedic Astrology\Vedic Astology Books\Kalyana Varmas Saravali JYOTISH VEDIC ASTROLOGY.pdf` (English translation, text layer present). Registered 2026-10-13 (migration 180).',
           1);

IF OBJECT_ID('dbo.tbl_Dim_BeneficMaleficSchools','U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_BeneficMaleficSchools (
        Id TINYINT NOT NULL CONSTRAINT PK_Dim_BeneficMaleficSchools PRIMARY KEY,
        Code VARCHAR(20) NOT NULL,
        Name VARCHAR(80) NOT NULL,
        ShortName VARCHAR(20) NOT NULL,
        Basis VARCHAR(12) NOT NULL,
        SourceRefCode VARCHAR(40) NOT NULL,
        DisplayOrder TINYINT NOT NULL,
        Notes NVARCHAR(500) NULL,
        CONSTRAINT UQ_Dim_BeneficMaleficSchools_Code UNIQUE (Code),
        CONSTRAINT CK_Dim_BeneficMaleficSchools_Basis CHECK (Basis IN ('NATURAL','FUNCTIONAL'))
    );
END;

IF OBJECT_ID('dbo.tbl_Rule_BeneficMaleficSchool','U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_BeneficMaleficSchool (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Rule_BeneficMaleficSchool PRIMARY KEY,
        RuleSetId TINYINT NOT NULL,
        SchoolId TINYINT NOT NULL,
        Topic VARCHAR(10) NOT NULL,
        NatureClass VARCHAR(20) NOT NULL,
        PlanetId TINYINT NULL,
        HouseNumber TINYINT NOT NULL,
        Result VARCHAR(12) NOT NULL,
        Note NVARCHAR(400) NULL,
        SourceRefCode VARCHAR(40) NOT NULL,
        CONSTRAINT FK_Rule_BeneficMaleficSchool_RuleSet FOREIGN KEY (RuleSetId) REFERENCES dbo.tbl_Rule_Sets(Id),
        CONSTRAINT FK_Rule_BeneficMaleficSchool_School FOREIGN KEY (SchoolId) REFERENCES dbo.tbl_Dim_BeneficMaleficSchools(Id),
        CONSTRAINT FK_Rule_BeneficMaleficSchool_Planet FOREIGN KEY (PlanetId) REFERENCES dbo.tbl_Planets(Id),
        CONSTRAINT CK_Rule_BeneficMaleficSchool_Topic CHECK (Topic IN ('LORDSHIP','PLACEMENT')),
        CONSTRAINT CK_Rule_BeneficMaleficSchool_Class CHECK (NatureClass IN
            ('ANY','NATURAL_BENEFIC','NATURAL_MALEFIC','FUNCTIONAL_BENEFIC','FUNCTIONAL_MALEFIC')),
        CONSTRAINT CK_Rule_BeneficMaleficSchool_House CHECK (HouseNumber BETWEEN 1 AND 12),
        CONSTRAINT CK_Rule_BeneficMaleficSchool_Result CHECK (
            (Topic = 'LORDSHIP' AND Result IN ('BENEFIC','MALEFIC','NEUTRAL')) OR
            (Topic = 'PLACEMENT' AND Result IN ('GOOD','CHALLENGING','MIXED'))),
        CONSTRAINT UQ_Rule_BeneficMaleficSchool UNIQUE (RuleSetId, SchoolId, Topic, NatureClass, PlanetId, HouseNumber)
    );
END;

DELETE FROM dbo.tbl_Rule_BeneficMaleficSchool WHERE RuleSetId = 1;
DELETE FROM dbo.tbl_Dim_BeneficMaleficSchools;

INSERT dbo.tbl_Dim_BeneficMaleficSchools (Id, Code, Name, ShortName, Basis, SourceRefCode, DisplayOrder, Notes) VALUES
 (1, 'PVR',          'P.V.R. Narasimha Rao - Vedic Astrology: An Integrated Approach', 'PVR',          'FUNCTIONAL', 'SRC_PVR_INTEGRATED', 1,
    N'Functional nature is Table 30 (Core LagnaFunctionalNature); placement rules are written for functional benefics and malefics.'),
 (2, 'BPHS',         'Parasara - Brihat Parashara Hora Shastra',                       'BPHS',         'NATURAL',    'SRC_BPHS',           2,
    N'Lordship from ch.34 (Sarajit Poddar / SJC translation, local file Yogakarakas.pdf); placement from the verse that malefics in the 3rd, 6th and 11th give good results. Verse numbering differs between editions.'),
 (3, 'RAMAN',        'B.V. Raman - How to Judge a Horoscope',                          'Raman',        'NATURAL',    'SRC_RAMAN_HTJH',     3,
    N'Lordship only (ch.1, benefic / malefic / neutral lords). Gives no rule for a planet owning two houses of different kinds, and none for placement.'),
 (4, 'SARAVALI',     'Kalyana Varma - Saravali',                                       'Saravali',     'NATURAL',    'SRC_SARAVALI',       4,
    N'Placement only (ch.30 v.86-87).'),
 (5, 'PHALADEEPIKA', 'Mantreswara - Phaladeepika',                                     'Phaladeepika', 'NATURAL',    'SRC_PHALADEEPIKA',   5,
    N'Placement only (ch.6 translator notes and v.24).');

;WITH r(School, Topic, Class, Planet, House, Result, Note, Src) AS (
    SELECT * FROM (VALUES
     -- ---- PVR placement (section 13.2, p.167): functional nature
     ('PVR','PLACEMENT','FUNCTIONAL_BENEFIC',NULL,1,'GOOD','A functional benefic in a quadrant or trine brings good results (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_BENEFIC',NULL,4,'GOOD','A functional benefic in a quadrant or trine brings good results (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_BENEFIC',NULL,5,'GOOD','A functional benefic in a quadrant or trine brings good results (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_BENEFIC',NULL,7,'GOOD','A functional benefic in a quadrant or trine brings good results (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_BENEFIC',NULL,9,'GOOD','A functional benefic in a quadrant or trine brings good results (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_BENEFIC',NULL,10,'GOOD','A functional benefic in a quadrant or trine brings good results (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_MALEFIC',NULL,1,'CHALLENGING','A functional malefic there is not good unless very strong (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_MALEFIC',NULL,4,'CHALLENGING','A functional malefic there is not good unless very strong (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_MALEFIC',NULL,5,'CHALLENGING','A functional malefic there is not good unless very strong (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_MALEFIC',NULL,7,'CHALLENGING','A functional malefic there is not good unless very strong (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_MALEFIC',NULL,9,'CHALLENGING','A functional malefic there is not good unless very strong (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_MALEFIC',NULL,10,'CHALLENGING','A functional malefic there is not good unless very strong (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_MALEFIC',NULL,3,'GOOD','A functional malefic in the 3rd or a dusthana gives good results by spoiling the significations of the bad house (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_MALEFIC',NULL,6,'GOOD','A functional malefic in the 3rd or a dusthana gives good results by spoiling the significations of the bad house (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_MALEFIC',NULL,8,'GOOD','A functional malefic in the 3rd or a dusthana gives good results by spoiling the significations of the bad house (PVR 13.2)','SRC_PVR_INTEGRATED'),
     ('PVR','PLACEMENT','FUNCTIONAL_MALEFIC',NULL,12,'GOOD','A functional malefic in the 3rd or a dusthana gives good results by spoiling the significations of the bad house (PVR 13.2)','SRC_PVR_INTEGRATED'),

     -- ---- Saravali placement (ch.30 v.86-87): natural nature
     ('SARAVALI','PLACEMENT','NATURAL_MALEFIC',NULL,1,'CHALLENGING','Malefics in houses other than the 6th, 8th and 12th bring harm to the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_MALEFIC',NULL,2,'CHALLENGING','Malefics in houses other than the 6th, 8th and 12th bring harm to the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_MALEFIC',NULL,3,'CHALLENGING','Malefics in houses other than the 6th, 8th and 12th bring harm to the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_MALEFIC',NULL,4,'CHALLENGING','Malefics in houses other than the 6th, 8th and 12th bring harm to the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_MALEFIC',NULL,5,'CHALLENGING','Malefics in houses other than the 6th, 8th and 12th bring harm to the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_MALEFIC',NULL,7,'CHALLENGING','Malefics in houses other than the 6th, 8th and 12th bring harm to the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_MALEFIC',NULL,9,'CHALLENGING','Malefics in houses other than the 6th, 8th and 12th bring harm to the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_MALEFIC',NULL,10,'CHALLENGING','Malefics in houses other than the 6th, 8th and 12th bring harm to the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_MALEFIC',NULL,11,'CHALLENGING','Malefics in houses other than the 6th, 8th and 12th bring harm to the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_MALEFIC',NULL,6,'GOOD','Malefics are auspicious in the evil houses, the 6th, 8th and 12th (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_MALEFIC',NULL,8,'GOOD','Malefics are auspicious in the evil houses, the 6th, 8th and 12th (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_MALEFIC',NULL,12,'GOOD','Malefics are auspicious in the evil houses, the 6th, 8th and 12th (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_BENEFIC',NULL,1,'GOOD','Benefics increase the potency of the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_BENEFIC',NULL,2,'GOOD','Benefics increase the potency of the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_BENEFIC',NULL,3,'GOOD','Benefics increase the potency of the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_BENEFIC',NULL,4,'GOOD','Benefics increase the potency of the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_BENEFIC',NULL,5,'GOOD','Benefics increase the potency of the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_BENEFIC',NULL,7,'GOOD','Benefics increase the potency of the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_BENEFIC',NULL,9,'GOOD','Benefics increase the potency of the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_BENEFIC',NULL,10,'GOOD','Benefics increase the potency of the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_BENEFIC',NULL,11,'GOOD','Benefics increase the potency of the bhava (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_BENEFIC',NULL,6,'CHALLENGING','Benefics prove adverse in the 6th, 8th and 12th (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_BENEFIC',NULL,8,'CHALLENGING','Benefics prove adverse in the 6th, 8th and 12th (Saravali 30.86-87)','SRC_SARAVALI'),
     ('SARAVALI','PLACEMENT','NATURAL_BENEFIC',NULL,12,'CHALLENGING','Benefics prove adverse in the 6th, 8th and 12th (Saravali 30.86-87)','SRC_SARAVALI'),

     -- ---- BPHS placement: malefics in 3, 6, 11 (verse 74.25 in the online edition; checked against Phaladeepika's agreeing note)
     ('BPHS','PLACEMENT','NATURAL_MALEFIC',NULL,3,'GOOD','Malefics in the 3rd, 6th and 11th give auspicious results (BPHS)','SRC_BPHS'),
     ('BPHS','PLACEMENT','NATURAL_MALEFIC',NULL,6,'GOOD','Malefics in the 3rd, 6th and 11th give auspicious results (BPHS)','SRC_BPHS'),
     ('BPHS','PLACEMENT','NATURAL_MALEFIC',NULL,11,'GOOD','Malefics in the 3rd, 6th and 11th give auspicious results (BPHS)','SRC_BPHS'),

     -- ---- Phaladeepika placement (ch.6 translator notes; v.24 lists malefics in 3, 6, 11 as a raja yoga)
     ('PHALADEEPIKA','PLACEMENT','NATURAL_MALEFIC',NULL,3,'GOOD','Mars, Saturn and Rahu are good in the 3rd, 6th and 11th: the malefic destroys enemies and diseases (Phaladeepika ch.6 notes)','SRC_PHALADEEPIKA'),
     ('PHALADEEPIKA','PLACEMENT','NATURAL_MALEFIC',NULL,6,'GOOD','Mars, Saturn and Rahu are good in the 3rd, 6th and 11th: the malefic destroys enemies and diseases (Phaladeepika ch.6 notes)','SRC_PHALADEEPIKA'),
     ('PHALADEEPIKA','PLACEMENT','NATURAL_MALEFIC',NULL,11,'GOOD','Mars, Saturn and Rahu are good in the 3rd, 6th and 11th: the malefic destroys enemies and diseases (Phaladeepika ch.6 notes)','SRC_PHALADEEPIKA'),

     -- ---- BPHS ch.34 lordship (Poddar/SJC translation): class does not matter; a kendra neutralises either kind
     ('BPHS','LORDSHIP','ANY',NULL,1,'BENEFIC','Lord of the Lagna is especially auspicious: the Lagna is a kendra and a kona (BPHS ch.34, Poddar/SJC tr.)','SRC_BPHS'),
     ('BPHS','LORDSHIP','ANY',NULL,2,'NEUTRAL','2nd lord takes its nature from the other house it owns or from association (BPHS ch.34, Poddar/SJC tr.)','SRC_BPHS'),
     ('BPHS','LORDSHIP','ANY',NULL,3,'MALEFIC','Lords of the 3rd, 6th and 11th (trishadaya) are inauspicious (BPHS ch.34, Poddar/SJC tr.)','SRC_BPHS'),
     ('BPHS','LORDSHIP','ANY',NULL,4,'NEUTRAL','A benefic owning a kendra loses its beneficence and a malefic its malfeasance: neutral, not reversed (BPHS ch.34, Poddar/SJC tr.)','SRC_BPHS'),
     ('BPHS','LORDSHIP','ANY',NULL,5,'BENEFIC','Lords of the konas (5th, 9th) are auspicious (BPHS ch.34, Poddar/SJC tr.)','SRC_BPHS'),
     ('BPHS','LORDSHIP','ANY',NULL,6,'MALEFIC','Lords of the 3rd, 6th and 11th (trishadaya) are inauspicious (BPHS ch.34, Poddar/SJC tr.)','SRC_BPHS'),
     ('BPHS','LORDSHIP','ANY',NULL,7,'NEUTRAL','A benefic owning a kendra loses its beneficence and a malefic its malfeasance: neutral, not reversed (BPHS ch.34, Poddar/SJC tr.)','SRC_BPHS'),
     ('BPHS','LORDSHIP','ANY',NULL,8,'NEUTRAL','8th lord is neutral but never auspicious: it is the 12th from the 9th (BPHS ch.34, Poddar/SJC tr.)','SRC_BPHS'),
     ('BPHS','LORDSHIP','ANY',NULL,9,'BENEFIC','Lords of the konas (5th, 9th) are auspicious (BPHS ch.34, Poddar/SJC tr.)','SRC_BPHS'),
     ('BPHS','LORDSHIP','ANY',NULL,10,'NEUTRAL','A benefic owning a kendra loses its beneficence and a malefic its malfeasance: neutral, not reversed (BPHS ch.34, Poddar/SJC tr.)','SRC_BPHS'),
     ('BPHS','LORDSHIP','ANY',NULL,11,'MALEFIC','Lords of the 3rd, 6th and 11th (trishadaya) are inauspicious (BPHS ch.34, Poddar/SJC tr.)','SRC_BPHS'),
     ('BPHS','LORDSHIP','ANY',NULL,12,'NEUTRAL','12th lord takes its nature from the other house it owns or from association (BPHS ch.34, Poddar/SJC tr.)','SRC_BPHS'),

     -- ---- Raman, How to Judge a Horoscope ch.1: benefic / malefic / neutral lords
     ('RAMAN','LORDSHIP','ANY',NULL,1,'BENEFIC','Lord of the 1st, other than the Moon, is a benefic lord (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','ANY','Moon',1,'NEUTRAL','The Moon as lord of the Lagna is neutral (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','ANY',NULL,5,'BENEFIC','Lords of the 5th and 9th (trikonadhipati) are benefic; the 9th more than the 5th (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','ANY',NULL,9,'BENEFIC','Lords of the 5th and 9th (trikonadhipati) are benefic; the 9th more than the 5th (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','ANY',NULL,3,'MALEFIC','Lords of the 3rd, 6th and 11th are malefic: the 11th worst, the 6th less, the 3rd least (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','ANY',NULL,6,'MALEFIC','Lords of the 3rd, 6th and 11th are malefic: the 11th worst, the 6th less, the 3rd least (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','ANY',NULL,11,'MALEFIC','Lords of the 3rd, 6th and 11th are malefic: the 11th worst, the 6th less, the 3rd least (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','NATURAL_BENEFIC',NULL,4,'MALEFIC','Kendradhipati: a natural benefic owning a kendra is a malefic lord (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','NATURAL_BENEFIC',NULL,7,'MALEFIC','Kendradhipati: a natural benefic owning a kendra is a malefic lord (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','NATURAL_BENEFIC',NULL,10,'MALEFIC','Kendradhipati: a natural benefic owning a kendra is a malefic lord (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','NATURAL_MALEFIC',NULL,4,'BENEFIC','A natural malefic owning a kendra becomes a temporary benefic (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','NATURAL_MALEFIC',NULL,7,'BENEFIC','A natural malefic owning a kendra becomes a temporary benefic (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','NATURAL_MALEFIC',NULL,10,'BENEFIC','A natural malefic owning a kendra becomes a temporary benefic (Raman HTJH ch.1)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','ANY','Sun',2,'NEUTRAL','The Sun or Moon as lord of the 2nd, 8th or 12th is neutral (Raman HTJH ch.1; the extract drops the third house number, read as 12th)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','ANY','Sun',8,'NEUTRAL','The Sun or Moon as lord of the 2nd, 8th or 12th is neutral (Raman HTJH ch.1; the extract drops the third house number, read as 12th)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','ANY','Sun',12,'NEUTRAL','The Sun or Moon as lord of the 2nd, 8th or 12th is neutral (Raman HTJH ch.1; the extract drops the third house number, read as 12th)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','ANY','Moon',2,'NEUTRAL','The Sun or Moon as lord of the 2nd, 8th or 12th is neutral (Raman HTJH ch.1; the extract drops the third house number, read as 12th)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','ANY','Moon',8,'NEUTRAL','The Sun or Moon as lord of the 2nd, 8th or 12th is neutral (Raman HTJH ch.1; the extract drops the third house number, read as 12th)','SRC_RAMAN_HTJH'),
     ('RAMAN','LORDSHIP','ANY','Moon',12,'NEUTRAL','The Sun or Moon as lord of the 2nd, 8th or 12th is neutral (Raman HTJH ch.1; the extract drops the third house number, read as 12th)','SRC_RAMAN_HTJH')
    ) v(School, Topic, Class, Planet, House, Result, Note, Src)
)
INSERT dbo.tbl_Rule_BeneficMaleficSchool (RuleSetId, SchoolId, Topic, NatureClass, PlanetId, HouseNumber, Result, Note, SourceRefCode)
SELECT 1, s.Id, r.Topic, r.Class, p.Id, r.House, r.Result, r.Note, r.Src
FROM r
JOIN dbo.tbl_Dim_BeneficMaleficSchools s ON s.Code = r.School
LEFT JOIN dbo.tbl_Planets p ON p.PlanetName = r.Planet;

IF (SELECT COUNT(*) FROM dbo.tbl_Rule_BeneficMaleficSchool WHERE RuleSetId = 1) <> 77
    THROW 51001, 'Expected 77 benefic/malefic school rules (PVR 16, Saravali 24, BPHS 3+12, Phaladeepika 3, Raman 19).', 1;

IF EXISTS (SELECT 1 FROM dbo.tbl_Rule_BeneficMaleficSchool r
           LEFT JOIN dbo.tbl_Dim_Source d ON d.Code = r.SourceRefCode WHERE d.Code IS NULL)
    THROW 51002, 'A benefic/malefic school rule cites an unregistered SRC_* code.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '180_create_rule_benefic_malefic_school.sql')
    INSERT dbo.SchemaMigrations (ScriptName, Note)
    VALUES ('180_create_rule_benefic_malefic_school.sql',
            'Benefic/malefic by school: 5 schools, 77 cited lordship and placement rules (PVR, BPHS ch.34, Raman, Saravali, Phaladeepika); registers SRC_SARAVALI');

COMMIT TRANSACTION;
PRINT '180 applied: 5 schools, 77 benefic/malefic school rules.';
