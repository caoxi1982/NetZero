using Cca.Cgp.Core.Base;
using Cca.Cgp.Core.Base.Extensions;
using Cca.Cgp.Core.Base.Ia;
using System;

namespace NetZero.Extensions
{
    public static class DataItemRequestEx
    {
        #region Public Methods

        /// <summary>
        /// Given a <see cref="DataItemRequest"/>, the last update time and a time to compare,
        /// Determine if the updateRateMs of the request has expired.
        /// </summary>
        /// <param name="request"><see cref="DataItemRequest"/> to check</param>
        /// <param name="lastUpdateTime"><see cref="DateTime"/> the last time a response was sent for this request</param>
        /// <param name="compareTime"><see cref="DateTime"/> to use as a comparison</param>
        /// <returns><see langword="true"/> if the request's updateRateMs has been exceeded.</returns>
        public static bool IsUpdateRateExpired(this DataItemRequest request, DateTime lastUpdateTime, DateTime compareTime)
        {
            // sanity check
            if (request == null)
            {
                return false;
            }
            // if there is no update type or no update rate send immediately
            if (request.metadata.updateRateMs == null || request.metadata.updateRateMs == 0)
            {
                return true;
            }
            // if the compare time is older than the last update
            if (compareTime < lastUpdateTime)
            {
                return true;
            }
            // calculate time since last update
            var timeSinceLastUpdate = compareTime - lastUpdateTime;
            // have we exceeded the update rate?
            return timeSinceLastUpdate.TotalMilliseconds >= request.metadata.updateRateMs;
        }

        /// <summary>
        /// Given a <see cref="DataItemRequest"/>, the last update time and a time to compare,
        /// Determine if the updateRateMs of the request has expired.
        /// </summary>
        /// <param name="request"><see cref="DataItemRequest"/> to check</param>
        /// <param name="lastUpdateTime"><see cref="DateTime"/> the last time a response was sent for this request</param>
        /// <param name="vqt"><see cref="Vqt"/> from which to get the timestamp for comparison</param>
        /// <returns><see langword="true"/> if the request's updateRateMs has been exceeded.</returns>
        public static bool IsUpdateRateExpired(this DataItemRequest request, DateTime lastUpdateTime, Vqt vqt)
        {
            return request.IsUpdateRateExpired(lastUpdateTime, vqt.DateTime());
        }

        #endregion Public Methods
    }
}
