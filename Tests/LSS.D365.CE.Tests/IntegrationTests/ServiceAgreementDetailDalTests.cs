using System.Diagnostics;
using LSS.D365.CE.DataAccess;
using LSS.D365.CE.Models.ProxyClasses;
using Microsoft.Xrm.Sdk.Query;
using Xunit.Abstractions;

namespace LSS.D365.CE.Tests.IntegrationTests;

[Collection("Integration")]
public class ServiceAgreementDetailDalTests : BaseD365IntegrationTest
{
    private readonly new_serviceagreementdetail _svcAgrmtDetailTarget;

    public ServiceAgreementDetailDalTests(ITestOutputHelper output) : base(output)
    {
        // Service Agreement Detail IDs used for testing:
        // 340a01e3-64b2-ef11-a8c2-00155d015159 - Teetzel, Colton - DSS and Transportation
        // b959c9d4-1a6a-ef11-a8bf-00155d015159 - Jackson, Ezalea - (OPTED OUT Eff. 10.20.24) PTO - 4.65%
        // 53316bd4-7c32-f011-8c4d-6045bd059d1e - Teetzel, Colton - Boots

        _svcAgrmtDetailTarget = OrgSvc.Retrieve(
                                        new_serviceagreementdetail.EntityLogicalName,
                                        Guid.Parse("53316bd4-7c32-f011-8c4d-6045bd059d1e"),
                                        new ColumnSet(
                                            new_serviceagreementdetail.Fields.new_serviceagreementid,
                                            new_serviceagreementdetail.Fields.new_serviceexpenseid,
                                            new_serviceagreementdetail.Fields.new_StartDate,
                                            new_serviceagreementdetail.Fields.new_EndDate
                                        )
                                    )
                                    ?.ToEntity<new_serviceagreementdetail>() ??
                                throw new InvalidOperationException("Failed to retrieve Service Agreement Detail");

        TracingSvc.Trace(
            $"Service Agreement Detail Target:\n\tID: {_svcAgrmtDetailTarget.Id}\n\tProcedure Code Id: {_svcAgrmtDetailTarget.new_serviceexpenseid?.Id}\n\tStart Date: {_svcAgrmtDetailTarget.new_StartDate}\n\tEnd Date: {_svcAgrmtDetailTarget.new_EndDate}\n"
        );
    }

    [Fact]
    public void ServiceAgreementDetailDal_ShouldParseOrderDetailsTotal()
    {
        var dal = new ServiceAgreementDetailDal(OrgSvc, TracingSvc);

        var orderDetailsSum = dal.GetRelatedOrderDetailsAmountsSum(_svcAgrmtDetailTarget);
        TracingSvc.Trace($"Related Details Sum: {orderDetailsSum}");

        Assert.NotEqual(0, orderDetailsSum);
    }

    [Fact]
    public void ServiceAgreementDetailDal_ShouldParseExpensesTotal()
    {
        var dal = new ServiceAgreementDetailDal(OrgSvc, TracingSvc);

        var orderDetailsSum = dal.GetRelatedExpenseAmountsSum(_svcAgrmtDetailTarget);
        TracingSvc.Trace($"Related expenses Sum: {orderDetailsSum}");

        Assert.NotEqual(0, orderDetailsSum);
    }

    [Fact]
    public void ServiceAgreementDetailDal_ShouldParseTotalSpentAmount()
    {
        var dal = new ServiceAgreementDetailDal(OrgSvc, TracingSvc);

        var timer = Stopwatch.StartNew();
        var totalAmount = dal.GetTotalSpentRollupAmount(_svcAgrmtDetailTarget);
        timer.Stop();
        TracingSvc.Trace($"Total spent amount: {totalAmount}");
        TracingSvc.Trace($"Elapsed time: {timer.Elapsed}");
    }

    [Fact]
    public void ServiceAgreementDetailDal_ShouldParse100()
    {
        var dal = new ServiceAgreementDetailDal(OrgSvc, TracingSvc);

        var latest100Modified = GetLatest100Modified();

        var rootTimer = Stopwatch.StartNew();

        foreach (var item in latest100Modified)
        {
            var itemTimer = Stopwatch.StartNew();
            var totalAmount = dal.GetTotalSpentRollupAmount(item);
            itemTimer.Stop();
            TracingSvc.Trace(
                $"Service Agreement Detail ID: {item.Id}, Total Spent Amount: {totalAmount}, Elapsed Time: {itemTimer.Elapsed}"
            );
        }

        rootTimer.Stop();
        TracingSvc.Trace($"Total Elapsed Time for 100 items: {rootTimer.Elapsed}");
    }

    private new_serviceagreementdetail[] GetLatest100Modified()
    {
        var query = new QueryExpression(new_serviceagreementdetail.EntityLogicalName)
        {
            TopCount = 100,
            ColumnSet = new ColumnSet(
                new_serviceagreementdetail.Fields.new_EndDate,
                new_serviceagreementdetail.Fields.new_serviceagreementdetailId,
                new_serviceagreementdetail.Fields.new_serviceexpenseid,
                new_serviceagreementdetail.Fields.new_StartDate
            ),
            Orders =
            {
                new OrderExpression(new_serviceagreementdetail.Fields.ModifiedOn, OrderType.Descending)
            }
        };

        return OrgSvc.RetrieveMultiple(query)
            .Entities
            .Select(e => e.ToEntity<new_serviceagreementdetail>())
            .ToArray();
    }
}