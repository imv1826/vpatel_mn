using System;
using LSS.D365.CE.Models;
using LSS.D365.CE.Models.ProxyClasses;
using Microsoft.Xrm.Sdk;

namespace LSS.D365.CE.Services
{
    public class EmplBudgetDetailRollupChangeHandler : IRollupEntityChangeHandler
    {
        public bool HasChanges(ChangeDetectConfig config, Entity preImage, Entity entity)
        {
            throw new NotImplementedException();
        }

        public pics_RecalculateSvcAgrmtDetailSpentToDateRequest BuildRecalcRequest(Entity preImage)
        {
            throw new NotImplementedException();
        }
    }
}