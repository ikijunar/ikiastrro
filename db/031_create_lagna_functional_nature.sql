-- 031_create_lagna_functional_nature.sql
-- tbl_Dim_LagnaFunctionalNature — B.V. Raman's functional benefic/malefic classification per Lagna,
-- transcribed verbatim from "How to Judge a Horoscope" Vol. 1, p.16-18 ("Benefics and Malefics for
-- each Lagna") + the Yogakarakas section (p.9-10). 84 rows = 12 Lagnas x 7 classical planets.
-- Rahu/Ketu are not classified by this source (no sign rulership) and are not seeded.
-- Three rows the book never classifies are seeded FunctionalNature = NULL, not guessed:
--   Aries -> Moon, Gemini -> Saturn, Aquarius -> Saturn.
-- This is a cross-check reference mirror; the engine computes functional nature in
-- LagnaFunctionalNature.cs (a heuristic that will diverge from this table for mixed-lordship planets).
-- Migration 030 stays reserved (unapplied) for house significations.

IF OBJECT_ID('dbo.tbl_Dim_LagnaFunctionalNature', 'U') IS NOT NULL
    DROP TABLE dbo.tbl_Dim_LagnaFunctionalNature;
GO

CREATE TABLE dbo.tbl_Dim_LagnaFunctionalNature
(
    Id               TINYINT      NOT NULL IDENTITY(1,1) PRIMARY KEY,
    LagnaSignId      TINYINT      NOT NULL REFERENCES dbo.tbl_SignAttributes(Id),
    PlanetId         TINYINT      NOT NULL REFERENCES dbo.tbl_Planets(Id),
    FunctionalNature VARCHAR(12)  NULL CHECK (FunctionalNature IN ('Benefic','Malefic','Neutral','Yogakaraka')),
    Rank             TINYINT      NULL,   -- 1 = "best benefic" / "worst malefic" within (Lagna, nature) per the book's phrasing
    Notes            VARCHAR(120) NULL,
    CONSTRAINT UQ_LagnaFunctionalNature UNIQUE (LagnaSignId, PlanetId)
);
GO

-- PlanetId: Sun=1 Moon=2 Mars=3 Mercury=4 Jupiter=5 Venus=6 Saturn=7
-- LagnaSignId: Aries=1 Taurus=2 Gemini=3 Cancer=4 Leo=5 Virgo=6 Libra=7 Scorpio=8 Sagittarius=9 Capricorn=10 Aquarius=11 Pisces=12
INSERT INTO dbo.tbl_Dim_LagnaFunctionalNature (LagnaSignId, PlanetId, FunctionalNature, Rank, Notes) VALUES
-- Aries
(1,5,'Benefic',1,'Best benefic'),(1,3,'Benefic',2,'Next benefic'),(1,1,'Benefic',3,NULL),
(1,4,'Malefic',1,'Greatest malefic — lord of 3rd & 6th'),(1,7,'Malefic',2,NULL),(1,6,'Malefic',3,NULL),
(1,2,NULL,NULL,'Not classified in source (How to Judge a Horoscope, Raman, p.16-18)'),
-- Taurus
(2,7,'Yogakaraka',1,'Best benefic — owns 9th & 10th'),(2,4,'Benefic',NULL,NULL),(2,3,'Benefic',NULL,NULL),(2,1,'Benefic',NULL,NULL),
(2,5,'Malefic',NULL,'Evil'),(2,2,'Malefic',NULL,'Evil'),(2,6,'Neutral',NULL,'Lagna lord'),
-- Gemini
(3,6,'Benefic',1,'Most beneficial'),(3,3,'Malefic',1,'Most malefic — lord of 6th & 11th'),
(3,5,'Malefic',NULL,'Evil'),(3,1,'Malefic',NULL,'Evil'),(3,2,'Neutral',NULL,NULL),(3,4,'Neutral',NULL,NULL),
(3,7,NULL,NULL,'Not classified in source (How to Judge a Horoscope, Raman, p.16-18)'),
-- Cancer
(4,3,'Yogakaraka',1,'Lord of 5th & 10th'),(4,5,'Benefic',NULL,NULL),
(4,6,'Malefic',NULL,'Evil'),(4,4,'Malefic',NULL,'Evil'),(4,7,'Neutral',NULL,NULL),(4,2,'Neutral',NULL,NULL),(4,1,'Neutral',NULL,NULL),
-- Leo
(5,3,'Yogakaraka',1,'Most auspicious; lord of 4th & 9th (Yogakarakas section)'),(5,1,'Benefic',NULL,NULL),
(5,4,'Malefic',NULL,NULL),(5,6,'Malefic',NULL,NULL),(5,5,'Neutral',NULL,NULL),(5,2,'Neutral',NULL,NULL),(5,7,'Neutral',NULL,NULL),
-- Virgo
(6,6,'Benefic',1,'Best benefic'),
(6,2,'Malefic',NULL,'Evil'),(6,3,'Malefic',NULL,'Evil'),(6,5,'Malefic',NULL,'Evil'),
(6,7,'Neutral',NULL,NULL),(6,1,'Neutral',NULL,NULL),(6,4,'Neutral',NULL,NULL),
-- Libra
(7,7,'Yogakaraka',1,'Best benefic — lord of 4th & 5th'),(7,4,'Benefic',NULL,NULL),(7,6,'Benefic',NULL,NULL),
(7,3,'Benefic',NULL,'Feeble benefic'),(7,1,'Malefic',NULL,NULL),(7,5,'Malefic',NULL,NULL),(7,2,'Malefic',NULL,NULL),
-- Scorpio
(8,2,'Benefic',1,'Best benefic'),(8,5,'Benefic',NULL,NULL),(8,1,'Benefic',NULL,NULL),
(8,4,'Malefic',NULL,'Evil'),(8,6,'Malefic',NULL,'Evil'),(8,3,'Neutral',NULL,NULL),(8,7,'Neutral',NULL,NULL),
-- Sagittarius
(9,3,'Benefic',NULL,NULL),(9,1,'Benefic',NULL,NULL),
(9,6,'Malefic',NULL,'Evil'),(9,7,'Malefic',NULL,'Evil'),(9,4,'Malefic',NULL,'Evil'),(9,5,'Neutral',NULL,NULL),(9,2,'Neutral',NULL,NULL),
-- Capricorn
(10,6,'Yogakaraka',1,'Most powerful benefic — lord of 5th & 10th'),(10,4,'Benefic',NULL,NULL),(10,7,'Benefic',NULL,NULL),
(10,3,'Malefic',1,'Worst'),(10,5,'Malefic',NULL,'Evil'),(10,2,'Malefic',NULL,'Evil'),(10,1,'Neutral',NULL,'8th lord — becomes neutral'),
-- Aquarius
(11,6,'Yogakaraka',1,'Lord of 4th & 9th'),(11,1,'Benefic',NULL,NULL),(11,3,'Benefic',NULL,NULL),
(11,5,'Malefic',NULL,NULL),(11,2,'Malefic',NULL,NULL),(11,4,'Neutral',NULL,NULL),
(11,7,NULL,NULL,'Not classified in source (How to Judge a Horoscope, Raman, p.16-18)'),
-- Pisces
(12,2,'Benefic',NULL,NULL),(12,3,'Benefic',NULL,NULL),
(12,7,'Malefic',NULL,NULL),(12,1,'Malefic',NULL,NULL),(12,6,'Malefic',NULL,NULL),(12,4,'Malefic',NULL,NULL),(12,5,'Neutral',NULL,NULL);
GO
