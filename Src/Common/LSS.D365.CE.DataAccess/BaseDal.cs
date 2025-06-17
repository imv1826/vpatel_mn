using Microsoft.Xrm.Sdk;

namespace LSS.D365.CE.DataAccess
{
    public abstract class BaseDal
    {
        protected readonly IOrganizationService OrgSvc;
        protected readonly ITracingService TracingSvc;

        public BaseDal(IOrganizationService orgSvc, ITracingService tracingSvc)
        {
            OrgSvc = orgSvc;
            TracingSvc = tracingSvc;
        }
    }
}