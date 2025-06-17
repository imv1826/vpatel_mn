using LSS.D365.CE.DataAccess;
using LSS.D365.CE.Models.ProxyClasses;
using Microsoft.Xrm.Sdk.Query;
using Xunit.Abstractions;

namespace LSS.D365.CE.Tests.IntegrationTests;

[Collection("Integration")]
public class SalesOrderDetailDalTests : BaseD365IntegrationTest
{
    private readonly SalesOrderDetail _orderDetail;

    public SalesOrderDetailDalTests(ITestOutputHelper output) : base(output)
    {
        _orderDetail = OrgSvc.Retrieve(
                               SalesOrderDetail.EntityLogicalName,
                               Guid.Parse("c2f2bc2d-7b56-4a8e-83fa-6215b1246da7"),
                               new ColumnSet(
                                   SalesOrderDetail.Fields.SalesOrderDetailId,
                                   SalesOrderDetail.Fields.SalesOrderId,
                                   SalesOrderDetail.Fields.ProductId,
                                   SalesOrderDetail.Fields.new_DateofServiceorExpense
                               )
                           )
                           ?.ToEntity<SalesOrderDetail>() ??
                       throw new InvalidOperationException("Failed to retrieve Service Agreement Detail");

        TracingSvc.Trace(
            $"SalesOrderDetail Target:\n\tID: {_orderDetail.Id}\n\tSalesOrderId: {_orderDetail.SalesOrderId?.Id}\n\tProductId: {_orderDetail.ProductId?.Id}\n\tnew_DateofServiceorExpense: {_orderDetail.new_DateofServiceorExpense}\n"
        );
    }

    [Fact]
    public void SalesOrderDetailDal_ShouldRetrieveSADetails()
    {
        var dal = new SalesOrderDetailDal(OrgSvc, TracingSvc);

        var res = dal.GetRelatedServiceAgreementDetails(
            _orderDetail.SalesOrderId?.Id,
            _orderDetail.ProductId?.Id,
            _orderDetail.new_DateofServiceorExpense
        );

        Assert.NotNull(res);

        var before1900 = res.Where(e => e.new_StartDate.Value.Year < 1900 || e.new_EndDate.Value.Year < 1900).ToArray();

        Assert.Empty(before1900);
    }
}