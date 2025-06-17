using LSS.D365.CE.Models.ProxyClasses;
using Microsoft.Xrm.Sdk;

namespace LSS.D365.CE.Models
{
    public interface IRollupEntityChangeHandler
    {
        /// <summary>
        /// Detects changes in the monitored attributes of the target entity.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="preImage">The pre-image entity to compare against.</param>
        /// <param name="messageTarget">The message target entity.</param>
        /// <returns>True if changes are detected, otherwise false.</returns>
        bool HasChanges(ChangeDetectConfig config, Entity preImage, Entity messageTarget);

        /// <summary>
        /// Sends the change request for the target entity.
        /// </summary>
        /// <param name="preImage">The pre-image entity to compare against.</param>
        /// <param name="entity">The message target entity.</param>
        /// <remarks>
        /// This method is typically used to initiate a rollup calculation or update based on detected changes.
        /// It should be called after changes have been detected by the <see cref="HasChanges"/> method.
        /// </remarks>
        pics_RecalculateSvcAgrmtDetailSpentToDateRequest BuildRecalcRequest(Entity preImage);
    }
}