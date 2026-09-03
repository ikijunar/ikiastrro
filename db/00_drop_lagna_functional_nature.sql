-- =====================================================================
-- One-time: drop tbl_Dim_LagnaFunctionalNature (was migration 031).
--
-- The B.V. Raman "Benefics and Malefics for each Lagna" reference mirror
-- has been removed from the project. Functional benefic/malefic/neutral/
-- yogakaraka is now produced solely by the computed classifier
-- Ikiastrro.Core.Calculators.LagnaFunctionalNature (no DB table involved).
--
-- Run this against an existing [ikiastrro] database that was built before
-- the removal. A brand-new machine building from db/ikiastrro.sql never
-- creates the table, so this file is a no-op there.
--
-- Safe to drop: no other table has a foreign key TO this table (its own
-- FKs point at tbl_SignAttributes / tbl_Planets, which are unaffected),
-- and no application code references it any more.
-- =====================================================================
USE [ikiastrro];
GO

IF OBJECT_ID('dbo.tbl_Dim_LagnaFunctionalNature', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.tbl_Dim_LagnaFunctionalNature;
    PRINT 'Dropped tbl_Dim_LagnaFunctionalNature.';
END
ELSE
    PRINT 'tbl_Dim_LagnaFunctionalNature does not exist - nothing to do.';
GO
