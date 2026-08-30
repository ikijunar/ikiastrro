namespace Ikiastrro.Data;

/// <summary>
/// Deletes a BirthDetails record and every chart artifact derived from it — all chart types (D1, D9,
/// and any future divisional chart) at once, since the 4 analytical tables are shared across chart
/// types and already scoped by BirthDetailId. Only the tbl_BirthDetails row itself is removed; nothing
/// about the tables/columns/repositories is touched.
///
/// FK-safe order: the 4 analytical tables (leaves, reference tbl_ChartResults) -> tbl_ChartResults
/// (references tbl_BirthDetails) -> tbl_BirthDetails. Sequential, un-transacted calls — same style as
/// every other multi-step write in this project (e.g. the CLI/Web save flow), not wrapped in an
/// explicit SQL transaction.
/// </summary>
public class BirthDetailDeletionService
{
    private readonly ChartConjunctionsRepository _conjunctionsRepo;
    private readonly ChartAspectsRepository _aspectsRepo;
    private readonly ChartKeyDetailsRepository _keyDetailsRepo;
    private readonly ChartHouseLordsRepository _houseLordsRepo;
    private readonly DashaPeriodsRepository _dashaPeriodsRepo;
    private readonly ChartResultsRepository _chartResultsRepo;
    private readonly BirthDetailsRepository _birthDetailsRepo;

    public BirthDetailDeletionService(
        ChartConjunctionsRepository conjunctionsRepo,
        ChartAspectsRepository aspectsRepo,
        ChartKeyDetailsRepository keyDetailsRepo,
        ChartHouseLordsRepository houseLordsRepo,
        DashaPeriodsRepository dashaPeriodsRepo,
        ChartResultsRepository chartResultsRepo,
        BirthDetailsRepository birthDetailsRepo)
    {
        _conjunctionsRepo = conjunctionsRepo;
        _aspectsRepo = aspectsRepo;
        _keyDetailsRepo = keyDetailsRepo;
        _houseLordsRepo = houseLordsRepo;
        _dashaPeriodsRepo = dashaPeriodsRepo;
        _chartResultsRepo = chartResultsRepo;
        _birthDetailsRepo = birthDetailsRepo;
    }

    public void DeleteBirthDetail(int birthDetailId)
    {
        _conjunctionsRepo.DeleteByBirthDetailId(birthDetailId);
        _aspectsRepo.DeleteByBirthDetailId(birthDetailId);
        _keyDetailsRepo.DeleteByBirthDetailId(birthDetailId);
        _houseLordsRepo.DeleteByBirthDetailId(birthDetailId);
        _dashaPeriodsRepo.DeleteByBirthDetailId(birthDetailId);
        _chartResultsRepo.DeleteByBirthDetailId(birthDetailId);
        _birthDetailsRepo.Delete(birthDetailId);
    }
}
