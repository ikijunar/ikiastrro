using Dapper;
using Ikiastrro.Core.Reference;

namespace Ikiastrro.Data;

/// <summary>
/// Read-only access to the bilingual concept catalogue
/// (<c>dbo.tbl_Astro_Terminology</c> + <c>dbo.tbl_Astro_TerminologyText</c>).
/// </summary>
public sealed class TerminologyRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public TerminologyRepository(SqlConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public List<TerminologyRow> GetTerminology()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<TerminologyRow>(
            @"SELECT TerminologyId, Category, Code, ParentCode, EngineCode, NumericKey,
                     FormulaSummary, DisplayOrder, IsActive
              FROM dbo.tbl_Astro_Terminology
              ORDER BY DisplayOrder, Code").ToList();
    }

    public List<TerminologyTextRow> GetTerminologyText()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<TerminologyTextRow>(
            @"SELECT tx.TerminologyId, tx.LanguageCode, tx.Script, tx.Name, tx.TraditionalName,
                     tx.ShortDescription, tx.TechnicalDefinition, tx.CalculationMethod, tx.SourceRefCode
              FROM dbo.tbl_Astro_TerminologyText tx
              JOIN dbo.tbl_Astro_Terminology t ON t.TerminologyId = tx.TerminologyId
              ORDER BY tx.TerminologyId, tx.LanguageCode, tx.Script").ToList();
    }

    public TerminologyCatalog BuildCatalog() => new(GetTerminology(), GetTerminologyText());
}
