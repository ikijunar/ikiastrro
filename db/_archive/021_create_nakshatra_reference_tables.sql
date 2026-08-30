-- 021_create_nakshatra_reference_tables.sql
-- Point 3 of D:\@ClaudeSpace\ikiastrro\docs\vedic-reference-tables.md: tbl_Nakshatras (27),
-- tbl_NakshatraPadas (108), vw_NakshatraPadaDetails, tbl_NakshatraSubLords (243 -- KP
-- sub-lord hierarchy levels 1-2 only, per rammyps's explicit call to stop there).
--
-- RulingPlanetId cross-checked against AstroMath.NakshatraLordOrder (Ketu,Venus,Sun,Moon,Mars,
-- Rahu,Jupiter,Saturn,Mercury cycle) -- exact match, not re-derived.
--
-- Guna/Gana/YoniAnimal/YoniGender/Nadi/Varna/Tatva/Direction are intentionally left NULL for
-- all 27 rows. These are legitimate single-answer classical tables (unlike RisingType), but
-- reproducing all 27 rows correctly from memory without a single cited source risks exactly
-- the kind of subtle, silently-wrong data this project has been bitten by before (VedAstro's
-- ayanamsha/Ketu bugs). Structure is ready; populate via an UPDATE once a specific reference
-- is picked and cross-checked, same discipline as RisingType in tbl_SignAttributes.
--
-- Pada -> Rasi/Navamsa cross-check: Pada slot (0-107) determines both RasiId = floor(slot/9)+1
-- and NavamsaSignId = (slot % 12) + 1. This is the exact algorithm AstroMath.GetNavamsaSign
-- uses (navamsaIndex = (signIndex*9 + segmentIndex) % 12, and slot IS signIndex*9+segmentIndex)
-- -- verified against the code's own worked examples (Aries seg0->Aries, Taurus seg0->Capricorn,
-- Gemini seg0->Libra) before generating all 108 rows.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Nakshatras')
BEGIN
    CREATE TABLE tbl_Nakshatras
    (
        Id                TINYINT      NOT NULL PRIMARY KEY,   -- 1-27, Abhijit excluded
        NakshatraName     VARCHAR(20)  NOT NULL UNIQUE,
        StartDegree       DECIMAL(9,6) NOT NULL,
        EndDegree         DECIMAL(9,6) NOT NULL,
        RulingPlanetId    TINYINT      NOT NULL REFERENCES tbl_Planets(Id),
        SequenceNumber    TINYINT      NOT NULL UNIQUE,
        RulingDeity       VARCHAR(30)  NULL,
        Symbol            VARCHAR(40)  NULL,
        Guna              VARCHAR(10)  NULL CHECK (Guna IN ('Satva','Rajas','Tamas') OR Guna IS NULL),
        Gana              VARCHAR(10)  NULL CHECK (Gana IN ('Deva','Manushya','Rakshasa') OR Gana IS NULL),
        YoniAnimal        VARCHAR(15)  NULL,
        YoniGender        VARCHAR(6)   NULL CHECK (YoniGender IN ('Male','Female') OR YoniGender IS NULL),
        Nadi              VARCHAR(6)   NULL CHECK (Nadi IN ('Vata','Pitta','Kapha') OR Nadi IS NULL),
        Varna             VARCHAR(12)  NULL CHECK (Varna IN ('Brahmin','Kshatriya','Vaishya','Shudra') OR Varna IS NULL),
        Tatva             VARCHAR(10)  NULL CHECK (Tatva IN ('Prithvi','Jal','Agni','Vayu','Akash') OR Tatva IS NULL),
        Direction         VARCHAR(10)  NULL,
        PrimaryRasiId          TINYINT NULL,               -- sign of the nakshatra's midpoint (populated below / migration 033)
        StraddlesSignBoundary  BIT NOT NULL DEFAULT 0      -- 1 for the 9 nakshatras whose padas span two signs
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_Nakshatras)
BEGIN
    -- RulingPlanetId: tbl_Planets.Id -- 1 Sun,2 Moon,3 Mars,4 Mercury,5 Jupiter,6 Venus,7 Saturn,8 Rahu,9 Ketu
    INSERT INTO tbl_Nakshatras (Id, NakshatraName, StartDegree, EndDegree, RulingPlanetId, SequenceNumber, RulingDeity, Symbol)
    VALUES
        (1,  'Ashwini',          0,          13.333333,  9, 1,  'Ashwini Kumaras',  'Horse''s head'),
        (2,  'Bharani',          13.333333,  26.666667,  6, 2,  'Yama',             'Yoni (womb)'),
        (3,  'Krittika',         26.666667,  40,         1, 3,  'Agni',             'Razor / axe'),
        (4,  'Rohini',           40,         53.333333,  2, 4,  'Brahma (Prajapati)', 'Ox-cart / chariot'),
        (5,  'Mrigashira',       53.333333,  66.666667,  3, 5,  'Soma (Chandra)',   'Deer''s head'),
        (6,  'Ardra',            66.666667,  80,         8, 6,  'Rudra',            'Teardrop / gem'),
        (7,  'Punarvasu',        80,         93.333333,  5, 7,  'Aditi',            'Bow and quiver'),
        (8,  'Pushya',           93.333333,  106.666667, 7, 8,  'Brihaspati',       'Cow''s udder / arrow'),
        (9,  'Ashlesha',         106.666667, 120,        4, 9,  'Nagas',            'Coiled serpent'),
        (10, 'Magha',            120,        133.333333, 9, 10, 'Pitrs (ancestors)', 'Royal throne'),
        (11, 'Purva Phalguni',   133.333333, 146.666667, 6, 11, 'Bhaga',            'Front legs of a bed'),
        (12, 'Uttara Phalguni',  146.666667, 160,        1, 12, 'Aryaman',          'Back legs of a bed'),
        (13, 'Hasta',            160,        173.333333, 2, 13, 'Savitar',          'Hand / fist'),
        (14, 'Chitra',           173.333333, 186.666667, 3, 14, 'Tvashta (Vishwakarma)', 'Bright jewel / pearl'),
        (15, 'Swati',            186.666667, 200,        8, 15, 'Vayu',             'Young shoot swaying / coral'),
        (16, 'Vishakha',         200,        213.333333, 5, 16, 'Indra-Agni',       'Decorated archway'),
        (17, 'Anuradha',         213.333333, 226.666667, 7, 17, 'Mitra',            'Lotus'),
        (18, 'Jyeshtha',         226.666667, 240,        4, 18, 'Indra',            'Circular amulet / umbrella'),
        (19, 'Mula',             240,        253.333333, 9, 19, 'Nirriti',          'Bunch of roots / lion''s tail'),
        (20, 'Purva Ashadha',    253.333333, 266.666667, 6, 20, 'Apas (Water)',     'Elephant tusk / fan'),
        (21, 'Uttara Ashadha',   266.666667, 280,        1, 21, 'Vishvedevas',      'Elephant tusk / small bed'),
        (22, 'Shravana',         280,        293.333333, 2, 22, 'Vishnu',           'Ear / three footprints'),
        (23, 'Dhanishta',        293.333333, 306.666667, 3, 23, 'Vasus (8 Vasus)',  'Drum / tabor'),
        (24, 'Shatabhisha',      306.666667, 320,        8, 24, 'Varuna',           'Empty circle / 100 stars'),
        (25, 'Purva Bhadrapada', 320,        333.333333, 5, 25, 'Aja Ekapada',      'Front legs of funeral cot / sword'),
        (26, 'Uttara Bhadrapada',333.333333, 346.666667, 7, 26, 'Ahirbudhnya',      'Back legs of funeral cot'),
        (27, 'Revati',           346.666667, 360,        4, 27, 'Pushan',           'Fish / drum');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_NakshatraPadas')
BEGIN
    CREATE TABLE tbl_NakshatraPadas
    (
        Id             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        NakshatraId    TINYINT      NOT NULL REFERENCES tbl_Nakshatras(Id),
        PadaNumber     TINYINT      NOT NULL CHECK (PadaNumber BETWEEN 1 AND 4),
        StartDegree    DECIMAL(9,6) NOT NULL,
        EndDegree      DECIMAL(9,6) NOT NULL,
        RasiId         TINYINT      NOT NULL REFERENCES tbl_SignAttributes(Id),
        NavamsaSignId  TINYINT      NOT NULL REFERENCES tbl_SignAttributes(Id),
        CONSTRAINT UQ_NakshatraPadas_Nakshatra_Pada UNIQUE (NakshatraId, PadaNumber)
    );

    CREATE INDEX IX_NakshatraPadas_StartDegree ON tbl_NakshatraPadas (StartDegree);
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_NakshatraPadas)
BEGIN
    INSERT INTO tbl_NakshatraPadas (NakshatraId, PadaNumber, StartDegree, EndDegree, RasiId, NavamsaSignId)
    VALUES
    (1, 1, 0, 3.333333, 1, 1),
    (1, 2, 3.333333, 6.666666, 1, 2),
    (1, 3, 6.666667, 10, 1, 3),
    (1, 4, 10, 13.333333, 1, 4),
    (2, 1, 13.333333, 16.666666, 1, 5),
    (2, 2, 16.666667, 20, 1, 6),
    (2, 3, 20, 23.333333, 1, 7),
    (2, 4, 23.333333, 26.666666, 1, 8),
    (3, 1, 26.666667, 30, 1, 9),
    (3, 2, 30, 33.333333, 2, 10),
    (3, 3, 33.333333, 36.666666, 2, 11),
    (3, 4, 36.666667, 40, 2, 12),
    (4, 1, 40, 43.333333, 2, 1),
    (4, 2, 43.333333, 46.666666, 2, 2),
    (4, 3, 46.666667, 50, 2, 3),
    (4, 4, 50, 53.333333, 2, 4),
    (5, 1, 53.333333, 56.666666, 2, 5),
    (5, 2, 56.666667, 60, 2, 6),
    (5, 3, 60, 63.333333, 3, 7),
    (5, 4, 63.333333, 66.666666, 3, 8),
    (6, 1, 66.666667, 70, 3, 9),
    (6, 2, 70, 73.333333, 3, 10),
    (6, 3, 73.333333, 76.666666, 3, 11),
    (6, 4, 76.666667, 80, 3, 12),
    (7, 1, 80, 83.333333, 3, 1),
    (7, 2, 83.333333, 86.666666, 3, 2),
    (7, 3, 86.666667, 90, 3, 3),
    (7, 4, 90, 93.333333, 4, 4),
    (8, 1, 93.333333, 96.666666, 4, 5),
    (8, 2, 96.666667, 100, 4, 6),
    (8, 3, 100, 103.333333, 4, 7),
    (8, 4, 103.333333, 106.666666, 4, 8),
    (9, 1, 106.666667, 110, 4, 9),
    (9, 2, 110, 113.333333, 4, 10),
    (9, 3, 113.333333, 116.666666, 4, 11),
    (9, 4, 116.666667, 120, 4, 12),
    (10, 1, 120, 123.333333, 5, 1),
    (10, 2, 123.333333, 126.666666, 5, 2),
    (10, 3, 126.666667, 130, 5, 3),
    (10, 4, 130, 133.333333, 5, 4),
    (11, 1, 133.333333, 136.666666, 5, 5),
    (11, 2, 136.666667, 140, 5, 6),
    (11, 3, 140, 143.333333, 5, 7),
    (11, 4, 143.333333, 146.666666, 5, 8),
    (12, 1, 146.666667, 150, 5, 9),
    (12, 2, 150, 153.333333, 6, 10),
    (12, 3, 153.333333, 156.666666, 6, 11),
    (12, 4, 156.666667, 160, 6, 12),
    (13, 1, 160, 163.333333, 6, 1),
    (13, 2, 163.333333, 166.666666, 6, 2),
    (13, 3, 166.666667, 170, 6, 3),
    (13, 4, 170, 173.333333, 6, 4),
    (14, 1, 173.333333, 176.666666, 6, 5),
    (14, 2, 176.666667, 180, 6, 6),
    (14, 3, 180, 183.333333, 7, 7),
    (14, 4, 183.333333, 186.666666, 7, 8),
    (15, 1, 186.666667, 190, 7, 9),
    (15, 2, 190, 193.333333, 7, 10),
    (15, 3, 193.333333, 196.666666, 7, 11),
    (15, 4, 196.666667, 200, 7, 12),
    (16, 1, 200, 203.333333, 7, 1),
    (16, 2, 203.333333, 206.666666, 7, 2),
    (16, 3, 206.666667, 210, 7, 3),
    (16, 4, 210, 213.333333, 8, 4),
    (17, 1, 213.333333, 216.666666, 8, 5),
    (17, 2, 216.666667, 220, 8, 6),
    (17, 3, 220, 223.333333, 8, 7),
    (17, 4, 223.333333, 226.666666, 8, 8),
    (18, 1, 226.666667, 230, 8, 9),
    (18, 2, 230, 233.333333, 8, 10),
    (18, 3, 233.333333, 236.666666, 8, 11),
    (18, 4, 236.666667, 240, 8, 12),
    (19, 1, 240, 243.333333, 9, 1),
    (19, 2, 243.333333, 246.666666, 9, 2),
    (19, 3, 246.666667, 250, 9, 3),
    (19, 4, 250, 253.333333, 9, 4),
    (20, 1, 253.333333, 256.666666, 9, 5),
    (20, 2, 256.666667, 260, 9, 6),
    (20, 3, 260, 263.333333, 9, 7),
    (20, 4, 263.333333, 266.666666, 9, 8),
    (21, 1, 266.666667, 270, 9, 9),
    (21, 2, 270, 273.333333, 10, 10),
    (21, 3, 273.333333, 276.666666, 10, 11),
    (21, 4, 276.666667, 280, 10, 12),
    (22, 1, 280, 283.333333, 10, 1),
    (22, 2, 283.333333, 286.666666, 10, 2),
    (22, 3, 286.666667, 290, 10, 3),
    (22, 4, 290, 293.333333, 10, 4),
    (23, 1, 293.333333, 296.666666, 10, 5),
    (23, 2, 296.666667, 300, 10, 6),
    (23, 3, 300, 303.333333, 11, 7),
    (23, 4, 303.333333, 306.666666, 11, 8),
    (24, 1, 306.666667, 310, 11, 9),
    (24, 2, 310, 313.333333, 11, 10),
    (24, 3, 313.333333, 316.666666, 11, 11),
    (24, 4, 316.666667, 320, 11, 12),
    (25, 1, 320, 323.333333, 11, 1),
    (25, 2, 323.333333, 326.666666, 11, 2),
    (25, 3, 326.666667, 330, 11, 3),
    (25, 4, 330, 333.333333, 12, 4),
    (26, 1, 333.333333, 336.666666, 12, 5),
    (26, 2, 336.666667, 340, 12, 6),
    (26, 3, 340, 343.333333, 12, 7),
    (26, 4, 343.333333, 346.666666, 12, 8),
    (27, 1, 346.666667, 350, 12, 9),
    (27, 2, 350, 353.333333, 12, 10),
    (27, 3, 353.333333, 356.666666, 12, 11),
    (27, 4, 356.666667, 360, 12, 12);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_NakshatraPadaDetails')
    EXEC('
    CREATE VIEW vw_NakshatraPadaDetails AS
    SELECT
        pada.Id,
        nak.Id AS NakshatraId, nak.NakshatraName,
        pada.PadaNumber, pada.StartDegree, pada.EndDegree,
        lord.Id AS NakshatraLordId, lord.PlanetName AS NakshatraLordName,
        rasi.Id AS RasiId, rasi.SignName AS RasiName,
        navamsa.Id AS NavamsaSignId, navamsa.SignName AS NavamsaSignName
    FROM tbl_NakshatraPadas pada
    JOIN tbl_Nakshatras nak ON nak.Id = pada.NakshatraId
    JOIN tbl_Planets lord ON lord.Id = nak.RulingPlanetId
    JOIN tbl_SignAttributes rasi ON rasi.Id = pada.RasiId
    JOIN tbl_SignAttributes navamsa ON navamsa.Id = pada.NavamsaSignId
    ');
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_NakshatraSubLords')
BEGIN
    CREATE TABLE tbl_NakshatraSubLords
    (
        Id                 INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        NakshatraId        TINYINT      NOT NULL REFERENCES tbl_Nakshatras(Id),
        SubSequenceNumber  TINYINT      NOT NULL CHECK (SubSequenceNumber BETWEEN 1 AND 9),
        SubLordId          TINYINT      NOT NULL REFERENCES tbl_Planets(Id),  -- all 9 planets participate, not just the 7 sign-rulers
        StartDegree        DECIMAL(9,6) NOT NULL,
        EndDegree          DECIMAL(9,6) NOT NULL,
        CONSTRAINT UQ_NakshatraSubLords_Nakshatra_Seq UNIQUE (NakshatraId, SubSequenceNumber)
    );

    CREATE INDEX IX_NakshatraSubLords_StartDegree ON tbl_NakshatraSubLords (StartDegree);
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_NakshatraSubLords)
BEGIN
    -- Widths are proportional to VimshottariYears/120 of the 13.333 nakshatra span (= Years/9
    -- degrees exactly), sequence starting from each nakshatra's own lord and cycling through
    -- the fixed Ketu->Venus->Sun->Moon->Mars->Rahu->Jupiter->Saturn->Mercury order.
    INSERT INTO tbl_NakshatraSubLords (NakshatraId, SubSequenceNumber, SubLordId, StartDegree, EndDegree)
    VALUES
    (1, 1, 9, 0, 0.777778),
    (1, 2, 6, 0.777778, 3),
    (1, 3, 1, 3, 3.666667),
    (1, 4, 2, 3.666667, 4.777778),
    (1, 5, 3, 4.777778, 5.555556),
    (1, 6, 8, 5.555556, 7.555556),
    (1, 7, 5, 7.555556, 9.333333),
    (1, 8, 7, 9.333333, 11.444444),
    (1, 9, 4, 11.444444, 13.333333),
    (2, 1, 6, 13.333333, 15.555555),
    (2, 2, 1, 15.555555, 16.222222),
    (2, 3, 2, 16.222222, 17.333333),
    (2, 4, 3, 17.333333, 18.111111),
    (2, 5, 8, 18.111111, 20.111111),
    (2, 6, 5, 20.111111, 21.888889),
    (2, 7, 7, 21.888889, 24),
    (2, 8, 4, 24, 25.888889),
    (2, 9, 9, 25.888889, 26.666666),
    (3, 1, 1, 26.666667, 27.333334),
    (3, 2, 2, 27.333334, 28.444445),
    (3, 3, 3, 28.444445, 29.222223),
    (3, 4, 8, 29.222223, 31.222223),
    (3, 5, 5, 31.222223, 33),
    (3, 6, 7, 33, 35.111111),
    (3, 7, 4, 35.111111, 37),
    (3, 8, 9, 37, 37.777778),
    (3, 9, 6, 37.777778, 40),
    (4, 1, 2, 40, 41.111111),
    (4, 2, 3, 41.111111, 41.888889),
    (4, 3, 8, 41.888889, 43.888889),
    (4, 4, 5, 43.888889, 45.666667),
    (4, 5, 7, 45.666667, 47.777778),
    (4, 6, 4, 47.777778, 49.666667),
    (4, 7, 9, 49.666667, 50.444444),
    (4, 8, 6, 50.444444, 52.666667),
    (4, 9, 1, 52.666667, 53.333333),
    (5, 1, 3, 53.333333, 54.111111),
    (5, 2, 8, 54.111111, 56.111111),
    (5, 3, 5, 56.111111, 57.888889),
    (5, 4, 7, 57.888889, 60),
    (5, 5, 4, 60, 61.888889),
    (5, 6, 9, 61.888889, 62.666666),
    (5, 7, 6, 62.666666, 64.888889),
    (5, 8, 1, 64.888889, 65.555555),
    (5, 9, 2, 65.555555, 66.666666),
    (6, 1, 8, 66.666667, 68.666667),
    (6, 2, 5, 68.666667, 70.444445),
    (6, 3, 7, 70.444445, 72.555556),
    (6, 4, 4, 72.555556, 74.444445),
    (6, 5, 9, 74.444445, 75.222223),
    (6, 6, 6, 75.222223, 77.444445),
    (6, 7, 1, 77.444445, 78.111111),
    (6, 8, 2, 78.111111, 79.222223),
    (6, 9, 3, 79.222223, 80),
    (7, 1, 5, 80, 81.777778),
    (7, 2, 7, 81.777778, 83.888889),
    (7, 3, 4, 83.888889, 85.777778),
    (7, 4, 9, 85.777778, 86.555556),
    (7, 5, 6, 86.555556, 88.777778),
    (7, 6, 1, 88.777778, 89.444444),
    (7, 7, 2, 89.444444, 90.555556),
    (7, 8, 3, 90.555556, 91.333333),
    (7, 9, 8, 91.333333, 93.333333),
    (8, 1, 7, 93.333333, 95.444444),
    (8, 2, 4, 95.444444, 97.333333),
    (8, 3, 9, 97.333333, 98.111111),
    (8, 4, 6, 98.111111, 100.333333),
    (8, 5, 1, 100.333333, 101),
    (8, 6, 2, 101, 102.111111),
    (8, 7, 3, 102.111111, 102.888889),
    (8, 8, 8, 102.888889, 104.888889),
    (8, 9, 5, 104.888889, 106.666666),
    (9, 1, 4, 106.666667, 108.555556),
    (9, 2, 9, 108.555556, 109.333334),
    (9, 3, 6, 109.333334, 111.555556),
    (9, 4, 1, 111.555556, 112.222223),
    (9, 5, 2, 112.222223, 113.333334),
    (9, 6, 3, 113.333334, 114.111111),
    (9, 7, 8, 114.111111, 116.111111),
    (9, 8, 5, 116.111111, 117.888889),
    (9, 9, 7, 117.888889, 120),
    (10, 1, 9, 120, 120.777778),
    (10, 2, 6, 120.777778, 123),
    (10, 3, 1, 123, 123.666667),
    (10, 4, 2, 123.666667, 124.777778),
    (10, 5, 3, 124.777778, 125.555556),
    (10, 6, 8, 125.555556, 127.555556),
    (10, 7, 5, 127.555556, 129.333333),
    (10, 8, 7, 129.333333, 131.444444),
    (10, 9, 4, 131.444444, 133.333333),
    (11, 1, 6, 133.333333, 135.555555),
    (11, 2, 1, 135.555555, 136.222222),
    (11, 3, 2, 136.222222, 137.333333),
    (11, 4, 3, 137.333333, 138.111111),
    (11, 5, 8, 138.111111, 140.111111),
    (11, 6, 5, 140.111111, 141.888889),
    (11, 7, 7, 141.888889, 144),
    (11, 8, 4, 144, 145.888889),
    (11, 9, 9, 145.888889, 146.666666),
    (12, 1, 1, 146.666667, 147.333334),
    (12, 2, 2, 147.333334, 148.444445),
    (12, 3, 3, 148.444445, 149.222223),
    (12, 4, 8, 149.222223, 151.222223),
    (12, 5, 5, 151.222223, 153),
    (12, 6, 7, 153, 155.111111),
    (12, 7, 4, 155.111111, 157),
    (12, 8, 9, 157, 157.777778),
    (12, 9, 6, 157.777778, 160),
    (13, 1, 2, 160, 161.111111),
    (13, 2, 3, 161.111111, 161.888889),
    (13, 3, 8, 161.888889, 163.888889),
    (13, 4, 5, 163.888889, 165.666667),
    (13, 5, 7, 165.666667, 167.777778),
    (13, 6, 4, 167.777778, 169.666667),
    (13, 7, 9, 169.666667, 170.444444),
    (13, 8, 6, 170.444444, 172.666667),
    (13, 9, 1, 172.666667, 173.333333),
    (14, 1, 3, 173.333333, 174.111111),
    (14, 2, 8, 174.111111, 176.111111),
    (14, 3, 5, 176.111111, 177.888889),
    (14, 4, 7, 177.888889, 180),
    (14, 5, 4, 180, 181.888889),
    (14, 6, 9, 181.888889, 182.666666),
    (14, 7, 6, 182.666666, 184.888889),
    (14, 8, 1, 184.888889, 185.555555),
    (14, 9, 2, 185.555555, 186.666666),
    (15, 1, 8, 186.666667, 188.666667),
    (15, 2, 5, 188.666667, 190.444445),
    (15, 3, 7, 190.444445, 192.555556),
    (15, 4, 4, 192.555556, 194.444445),
    (15, 5, 9, 194.444445, 195.222223),
    (15, 6, 6, 195.222223, 197.444445),
    (15, 7, 1, 197.444445, 198.111111),
    (15, 8, 2, 198.111111, 199.222223),
    (15, 9, 3, 199.222223, 200),
    (16, 1, 5, 200, 201.777778),
    (16, 2, 7, 201.777778, 203.888889),
    (16, 3, 4, 203.888889, 205.777778),
    (16, 4, 9, 205.777778, 206.555556),
    (16, 5, 6, 206.555556, 208.777778),
    (16, 6, 1, 208.777778, 209.444444),
    (16, 7, 2, 209.444444, 210.555556),
    (16, 8, 3, 210.555556, 211.333333),
    (16, 9, 8, 211.333333, 213.333333),
    (17, 1, 7, 213.333333, 215.444444),
    (17, 2, 4, 215.444444, 217.333333),
    (17, 3, 9, 217.333333, 218.111111),
    (17, 4, 6, 218.111111, 220.333333),
    (17, 5, 1, 220.333333, 221),
    (17, 6, 2, 221, 222.111111),
    (17, 7, 3, 222.111111, 222.888889),
    (17, 8, 8, 222.888889, 224.888889),
    (17, 9, 5, 224.888889, 226.666666),
    (18, 1, 4, 226.666667, 228.555556),
    (18, 2, 9, 228.555556, 229.333334),
    (18, 3, 6, 229.333334, 231.555556),
    (18, 4, 1, 231.555556, 232.222223),
    (18, 5, 2, 232.222223, 233.333334),
    (18, 6, 3, 233.333334, 234.111111),
    (18, 7, 8, 234.111111, 236.111111),
    (18, 8, 5, 236.111111, 237.888889),
    (18, 9, 7, 237.888889, 240),
    (19, 1, 9, 240, 240.777778),
    (19, 2, 6, 240.777778, 243),
    (19, 3, 1, 243, 243.666667),
    (19, 4, 2, 243.666667, 244.777778),
    (19, 5, 3, 244.777778, 245.555556),
    (19, 6, 8, 245.555556, 247.555556),
    (19, 7, 5, 247.555556, 249.333333),
    (19, 8, 7, 249.333333, 251.444444),
    (19, 9, 4, 251.444444, 253.333333),
    (20, 1, 6, 253.333333, 255.555555),
    (20, 2, 1, 255.555555, 256.222222),
    (20, 3, 2, 256.222222, 257.333333),
    (20, 4, 3, 257.333333, 258.111111),
    (20, 5, 8, 258.111111, 260.111111),
    (20, 6, 5, 260.111111, 261.888889),
    (20, 7, 7, 261.888889, 264),
    (20, 8, 4, 264, 265.888889),
    (20, 9, 9, 265.888889, 266.666666),
    (21, 1, 1, 266.666667, 267.333334),
    (21, 2, 2, 267.333334, 268.444445),
    (21, 3, 3, 268.444445, 269.222223),
    (21, 4, 8, 269.222223, 271.222223),
    (21, 5, 5, 271.222223, 273),
    (21, 6, 7, 273, 275.111111),
    (21, 7, 4, 275.111111, 277),
    (21, 8, 9, 277, 277.777778),
    (21, 9, 6, 277.777778, 280),
    (22, 1, 2, 280, 281.111111),
    (22, 2, 3, 281.111111, 281.888889),
    (22, 3, 8, 281.888889, 283.888889),
    (22, 4, 5, 283.888889, 285.666667),
    (22, 5, 7, 285.666667, 287.777778),
    (22, 6, 4, 287.777778, 289.666667),
    (22, 7, 9, 289.666667, 290.444444),
    (22, 8, 6, 290.444444, 292.666667),
    (22, 9, 1, 292.666667, 293.333333),
    (23, 1, 3, 293.333333, 294.111111),
    (23, 2, 8, 294.111111, 296.111111),
    (23, 3, 5, 296.111111, 297.888889),
    (23, 4, 7, 297.888889, 300),
    (23, 5, 4, 300, 301.888889),
    (23, 6, 9, 301.888889, 302.666666),
    (23, 7, 6, 302.666666, 304.888889),
    (23, 8, 1, 304.888889, 305.555555),
    (23, 9, 2, 305.555555, 306.666666),
    (24, 1, 8, 306.666667, 308.666667),
    (24, 2, 5, 308.666667, 310.444445),
    (24, 3, 7, 310.444445, 312.555556),
    (24, 4, 4, 312.555556, 314.444445),
    (24, 5, 9, 314.444445, 315.222223),
    (24, 6, 6, 315.222223, 317.444445),
    (24, 7, 1, 317.444445, 318.111111),
    (24, 8, 2, 318.111111, 319.222223),
    (24, 9, 3, 319.222223, 320),
    (25, 1, 5, 320, 321.777778),
    (25, 2, 7, 321.777778, 323.888889),
    (25, 3, 4, 323.888889, 325.777778),
    (25, 4, 9, 325.777778, 326.555556),
    (25, 5, 6, 326.555556, 328.777778),
    (25, 6, 1, 328.777778, 329.444444),
    (25, 7, 2, 329.444444, 330.555556),
    (25, 8, 3, 330.555556, 331.333333),
    (25, 9, 8, 331.333333, 333.333333),
    (26, 1, 7, 333.333333, 335.444444),
    (26, 2, 4, 335.444444, 337.333333),
    (26, 3, 9, 337.333333, 338.111111),
    (26, 4, 6, 338.111111, 340.333333),
    (26, 5, 1, 340.333333, 341),
    (26, 6, 2, 341, 342.111111),
    (26, 7, 3, 342.111111, 342.888889),
    (26, 8, 8, 342.888889, 344.888889),
    (26, 9, 5, 344.888889, 346.666666),
    (27, 1, 4, 346.666667, 348.555556),
    (27, 2, 9, 348.555556, 349.333334),
    (27, 3, 6, 349.333334, 351.555556),
    (27, 4, 1, 351.555556, 352.222223),
    (27, 5, 2, 352.222223, 353.333334),
    (27, 6, 3, 353.333334, 354.111111),
    (27, 7, 8, 354.111111, 356.111111),
    (27, 8, 5, 356.111111, 357.888889),
    (27, 9, 7, 357.888889, 360);
END
GO

-- tbl_Nakshatras sign columns (also shipped as migration 033 for already-built DBs). Depends on
-- the pada seed above, so it runs here at end-of-file. Idempotent.
IF EXISTS (SELECT 1 FROM tbl_Nakshatras WHERE PrimaryRasiId IS NULL)
BEGIN
    UPDATE n
    SET PrimaryRasiId = FLOOR(((n.StartDegree + n.EndDegree) / 2.0) / 30.0) + 1
    FROM tbl_Nakshatras n;

    UPDATE n
    SET StraddlesSignBoundary = CASE WHEN p1.RasiId <> p4.RasiId THEN 1 ELSE 0 END
    FROM tbl_Nakshatras n
    JOIN tbl_NakshatraPadas p1 ON p1.NakshatraId = n.Id AND p1.PadaNumber = 1
    JOIN tbl_NakshatraPadas p4 ON p4.NakshatraId = n.Id AND p4.PadaNumber = 4;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Nakshatras') AND name = 'PrimaryRasiId' AND is_nullable = 1)
    ALTER TABLE dbo.tbl_Nakshatras ALTER COLUMN PrimaryRasiId TINYINT NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Nakshatras_PrimaryRasi')
    ALTER TABLE dbo.tbl_Nakshatras
        ADD CONSTRAINT FK_Nakshatras_PrimaryRasi
        FOREIGN KEY (PrimaryRasiId) REFERENCES dbo.tbl_SignAttributes(Id);
GO
