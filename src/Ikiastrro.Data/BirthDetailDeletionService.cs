namespace Ikiastrro.Data;

/// <summary>
/// Deletes a BirthDetails record and every chart artifact derived from it — all chart types (D1, D9,
/// and any future divisional chart) at once, since the analytical tables are shared across chart
/// types and already scoped by BirthDetailId. Only the tbl_BirthDetails row itself is removed; nothing
/// about the tables/columns/repositories is touched.
///
/// FK-safe order: the analytical tables (leaves, reference tbl_ChartResults) -> tbl_ChartResults
/// (references tbl_BirthDetails) -> tbl_BirthDetails. The multi-graha conjunction groups are deleted
/// AFTER tbl_Chart_Conjunctions (its pair rows carry an FK to the groups); members cascade with the
/// group. Sequential, un-transacted calls — same style as
/// every other multi-step write in this project (e.g. the CLI/Web save flow), not wrapped in an
/// explicit SQL transaction.
/// </summary>
public class BirthDetailDeletionService
{
    private readonly ChartConjunctionsRepository _conjunctionsRepo;
    private readonly ChartMultiGrahaConjunctionRepository _multiGrahaConjunctionsRepo;
    private readonly ChartAspectsRepository _aspectsRepo;
    private readonly ChartKeyDetailsRepository _keyDetailsRepo;
    private readonly ChartHouseLordsRepository _houseLordsRepo;
    private readonly PlanetaryStateRepository _planetaryStateRepo;
    private readonly DashaPeriodsRepository _dashaPeriodsRepo;
    private readonly ChartResultsRepository _chartResultsRepo;
    private readonly BirthDetailsRepository _birthDetailsRepo;

    public BirthDetailDeletionService(
        ChartConjunctionsRepository conjunctionsRepo,
        ChartMultiGrahaConjunctionRepository multiGrahaConjunctionsRepo,
        ChartAspectsRepository aspectsRepo,
        ChartKeyDetailsRepository keyDetailsRepo,
        ChartHouseLordsRepository houseLordsRepo,
        PlanetaryStateRepository planetaryStateRepo,
        DashaPeriodsRepository dashaPeriodsRepo,
        ChartResultsRepository chartResultsRepo,
        BirthDetailsRepository birthDetailsRepo)
    {
        _conjunctionsRepo = conjunctionsRepo;
        _multiGrahaConjunctionsRepo = multiGrahaConjunctionsRepo;
        _aspectsRepo = aspectsRepo;
        _keyDetailsRepo = keyDetailsRepo;
        _houseLordsRepo = houseLordsRepo;
        _planetaryStateRepo = planetaryStateRepo;
        _dashaPeriodsRepo = dashaPeriodsRepo;
        _chartResultsRepo = chartResultsRepo;
        _birthDetailsRepo = birthDetailsRepo;
    }

    public void DeleteBirthDetail(int birthDetailId)
    {
        _conjunctionsRepo.DeleteByBirthDetailId(birthDetailId);
        _multiGrahaConjunctionsRepo.DeleteByBirthDetailId(birthDetailId);  // after the pair rows (they FK the groups)
        _aspectsRepo.DeleteByBirthDetailId(birthDetailId);
        _keyDetailsRepo.DeleteByBirthDetailId(birthDetailId);
        _houseLordsRepo.DeleteByBirthDetailId(birthDetailId);
        _planetaryStateRepo.DeleteByBirthDetailId(birthDetailId);
        _dashaPeriodsRepo.DeleteByBirthDetailId(birthDetailId);
        _chartResultsRepo.DeleteByBirthDetailId(birthDetailId);
        _birthDetailsRepo.Delete(birthDetailId);
    }
}
