-- =====================================================================
-- 11 — tbl_Rule_VargaScheme: how each varga chart type derives a planet's
-- varga sign, per rule-set. Read by C# (VargaSignRuleFactory) and, later,
-- by the Python comparison layer. D1 is the identity rasi and is NOT here.
-- SignRuleKey names the C# IVargaSignRule; the l-part formulae are in the
-- Plan-A spec 3.2 (traced to PyJHora horoscope/chart/charts.py).
-- Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/11_create_rule_vargascheme.sql
-- =====================================================================
USE [ikiastrro];
GO
IF OBJECT_ID('dbo.tbl_Rule_VargaScheme', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_VargaScheme (
    Id             TINYINT      NOT NULL CONSTRAINT PK_Rule_VargaScheme PRIMARY KEY,
    RuleSetId      TINYINT      NOT NULL CONSTRAINT FK_Rule_VargaScheme_RuleSet   FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
    ChartTypeId    TINYINT      NOT NULL CONSTRAINT FK_Rule_VargaScheme_ChartType FOREIGN KEY REFERENCES dbo.tbl_Dim_ChartType (Id),
    DivisionFactor TINYINT      NOT NULL,
    MethodCode     VARCHAR(40)  NOT NULL,
    MethodSource   VARCHAR(200) NOT NULL,
    SignRuleKind   VARCHAR(10)  NOT NULL,
    SignRuleKey    VARCHAR(40)  NOT NULL,
    CONSTRAINT UQ_Rule_VargaScheme UNIQUE (RuleSetId, ChartTypeId)
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_VargaScheme)
INSERT dbo.tbl_Rule_VargaScheme
    (Id, RuleSetId, ChartTypeId, DivisionFactor, MethodCode, MethodSource, SignRuleKind, SignRuleKey)
SELECT v.Id, 1, ct.Id, v.N, v.MethodCode, v.MethodSource, v.Kind, v.SignRuleKey
FROM (VALUES
    ( 1, 'D2',    2,  'ClassicalTwoSign',    'BPHS two-sign Cn/Le; AstroMath.GetHoraSign',                       'Special', 'HoraD2Classic'),
    ( 2, 'D2-US', 2,  'UmaShambu',           'Parasara Uma Shambu; PyJHora hora_chart d2 default (closest)',     'Special', 'HoraD2UmaShambu'),
    ( 3, 'D3',    3,  'ParasaraTraditional', 'BPHS Drekkana 1/5/9; PyJHora _drekkana_chart_parasara',            'Linear',  'DrekkanaD3'),
    ( 4, 'D4',    4,  'ParasaraTraditional', 'BPHS Chaturthamsa; PyJHora _chaturthamsa_parasara',                'Linear',  'ChaturthamsaD4'),
    ( 5, 'D5',    5,  'ParasaraTraditional', 'BPHS Panchamsa; PyJHora panchamsa_chart method 1',                 'Special', 'PanchamsaD5'),
    ( 6, 'D6',    6,  'ParasaraTraditional', 'BPHS Shashtamsa; AstroMath.GetShashtamsaSign',                     'Special', 'ShashtamsaD6'),
    ( 7, 'D7',    7,  'ParasaraTraditional', 'BPHS Saptamsa odd-self/even-7th; PyJHora saptamsa_chart method 1', 'Special', 'SaptamsaD7'),
    ( 8, 'D8',    8,  'ParasaraTraditional', 'BPHS Ashtamsa; PyJHora ashtamsa_chart method 1',                   'Special', 'AshtamsaD8'),
    ( 9, 'D9',    9,  'ParasaraTraditional', 'BPHS Navamsa; AstroMath.GetNavamsaSign',                           'Special', 'NavamsaD9'),
    (10, 'D10',   10, 'ParasaraTraditional', 'BPHS Dasamsa odd-self/even-9th; AstroMath.GetDasamsaSign',         'Special', 'DasamsaD10'),
    (11, 'D11',   11, 'SanjayRath',          'Sanjay Rath Rudramsa; AstroMath.GetRudramsaSign',                  'Special', 'RudramsaD11'),
    (12, 'D12',   12, 'ParasaraTraditional', 'BPHS Dwadasamsa 12-from-self; PyJHora dwadasamsa_chart method 1',  'Linear',  'DwadasamsaD12'),
    (13, 'D16',   16, 'ParasaraTraditional', 'BPHS Shodasamsa; PyJHora shodasamsa_chart method 1',               'Special', 'ShodasamsaD16'),
    (14, 'D20',   20, 'ParasaraTraditional', 'BPHS Vimsamsa; PyJHora vimsamsa_chart method 1',                   'Special', 'VimsamsaD20'),
    (15, 'D24',   24, 'ParasaraTraditional', 'BPHS Siddhamsa odd-Le/even-Cn; PyJHora chaturvimsamsa_chart m1',   'Special', 'SiddhamsaD24'),
    (16, 'D27',   27, 'ParasaraTraditional', 'BPHS Nakshatramsa by element; PyJHora nakshatramsa_chart m1',      'Special', 'NakshatramsaD27'),
    (17, 'D30',   30, 'ParasaraTraditional', 'BPHS Trimsamsa unequal 5-part; PyJHora trimsamsa_chart method 1',  'Special', 'TrimsamsaD30'),
    (18, 'D40',   40, 'ParasaraTraditional', 'BPHS Khavedamsa odd-Ar/even-Li; PyJHora khavedamsa_chart m1',      'Special', 'KhavedamsaD40'),
    (19, 'D45',   45, 'ParasaraTraditional', 'BPHS Akshavedamsa; PyJHora akshavedamsa_chart method 1',           'Special', 'AkshavedamsaD45'),
    (20, 'D60',   60, 'ParasaraTraditional', 'BPHS Shashtyamsa from-sign; PyJHora shashtyamsa_chart method 1',   'Linear',  'ShashtyamsaD60')
) AS v(Id, Code, N, MethodCode, MethodSource, Kind, SignRuleKey)
JOIN dbo.tbl_Dim_ChartType ct ON ct.Code = v.Code;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '11_create_rule_vargascheme.sql', 'tbl_Rule_VargaScheme + 20-row seed (RuleSetId 1)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '11_create_rule_vargascheme.sql');
GO
PRINT '11 applied: tbl_Rule_VargaScheme ready.';
GO
