using Cca.Cgp.Core.Base;
using NetZero.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NetZero.EA
{
    public class SubscribedTagsManager
    {
        #region Private Fields

        private readonly ConcurrentDictionary<string, DateTime> _lastUpdateTime = new();
        private readonly ConcurrentDictionary<string, List<DataItemRequest>> _pausedTags = new();
        private readonly ConcurrentDictionary<string, List<DataItemRequest>> _subscribedTags = new();

        #endregion Private Fields

        #region Public Methods

        /// <summary>
        /// Indicates if there are any paused subscriptions for the tagId
        /// </summary>
        /// <param name="tagId"><see cref="string"/> tagId to check.</param>
        /// <returns>
        /// <see langword="true"/> if there are any paused <see cref="DataItemRequest"/> for the tagId
        /// </returns>
        public bool IsPaused(string tagId)
        {
            return _pausedTags.ContainsKey(tagId);
        }

        /// <summary>
        /// Indicates if this <see cref="DataItemRequest"/> has a subscription which has been paused.
        /// </summary>
        /// <param name="request"><see cref="DataItemRequest"/> to check</param>
        /// <returns><see langword="true"/> if this <see cref="DataItemRequest"/> has been paused</returns>
        public bool IsPaused(DataItemRequest request)
        {
            return IsPaused(request.id) && _pausedTags[request.id].Contains(request);
        }

        /// <summary>
        /// Indicates if there is are any active subscriptions for the tagId
        /// </summary>
        /// <param name="tagId"><see cref="string"/> tagId as FQN</param>
        /// <returns><see langword="true"/> If there are active subscriptions for the tagId</returns>
        public bool IsSubscribed(string tagId)
        {
            return _subscribedTags.ContainsKey(tagId);
        }

        /// <summary>
        /// Indicates if this specific <see cref="DataItemRequest"/> has an active subscription
        /// </summary>
        /// <param name="request"></param>
        /// <returns>
        /// <see langword="true"/> if there is an acrive subscription for this <see cref="DataItemRequest"/>
        /// </returns>
        public bool IsSubscribed(DataItemRequest request)
        {
            return IsSubscribed(request.id) && _subscribedTags[request.id].Contains(request);
        }

        /// <summary>
        /// If there are active subscriptions for a tag id, return them as an IEnumerable.
        /// </summary>
        /// <param name="tagId"><see cref="string"/> tag FQN to check</param>
        /// <param name="requests">
        /// <see cref="IEnumerable{DataItemRequest}"/><see langword="out"/> parameter of active
        /// requests for the tagId
        /// </param>
        /// <returns><see langword="true"/> if there were active subscriptions for the tagId</returns>
        public bool TryGetActiveRequests(string tagId, out IEnumerable<DataItemRequest> requests)
        {
            requests = new List<DataItemRequest>();
            // if we have subscriptions
            if (IsSubscribed(tagId))
            {
                // if any are paused
                if (IsPaused(tagId))
                {
                    // return the active requests that have not been paused
                    requests = _subscribedTags[tagId].Where(r => !_pausedTags[tagId].Contains(r));
                }
                else
                {
                    // return all subscriptions
                    requests = _subscribedTags[tagId];
                }
            }
            return false;
        }

        /// <summary>
        /// Given a tagId and a comparison date, find any requests for that tagid which have exceeded the update rate ms
        /// since the last time a response was sent
        /// </summary>
        /// <param name="tagId"><see cref="string"/> tag FQN to check</param>
        /// <param name="compareTime"><see cref="DateTime"/> to compare the last update time  against</param>
        /// <param name="requests"><see langword="out"/> parameter of <see cref="IEnumerable{DataItemRequest}"/></param>
        /// <returns><see langword="true"/> if there are any expired requests</returns>
        public bool TryGetExpiredRequests(string tagId, DateTime compareTime, out IEnumerable<DataItemRequest> requests)
        {
            requests = new List<DataItemRequest>();
            var expiredRequests = new List<DataItemRequest>();
            if (TryGetActiveRequests(tagId, out IEnumerable<DataItemRequest> activeRequests))
            {
                // find active requests that are ready to be updated
                foreach (var request in activeRequests)
                {
                    var trackingId = request.trackingId.ToString();
                    if (string.IsNullOrEmpty(trackingId))
                    {
                        continue;
                    }
                    // try to get the last update time for this request
                    if (!_lastUpdateTime.TryGetValue(trackingId, out DateTime lastUpdateTime))
                    {
                        // if we have never sent a response for this tag
                        // indicate with a very old lastUpdate time
                        lastUpdateTime = DateTime.UtcNow.AddDays(-5);
                    }
                    if (request.IsUpdateRateExpired(lastUpdateTime, compareTime))
                    {
                        expiredRequests.Add(request);
                        _lastUpdateTime[trackingId] = compareTime;
                    }
                }
            }
            // were any requests added to the list?
            return requests.Any();
        }

        /// <summary>
        /// Try to pause an existing subscription.
        /// </summary>
        /// <param name="request"><see cref="DataItemRequest"/> to pause</param>
        /// <returns>
        /// <see langword="true"/> if the request was moved to the paused list or <see
        /// langword="false"/> if the request was not already subscribed
        /// </returns>
        public bool TryPauseMonitor(DataItemRequest request)
        {
            // if we haven't subscribed, we can't pause it
            if (!IsSubscribed(request))
            {
                return false;
            }
            // if this is the first time the id was paused, create the new entry
            if (_pausedTags.TryAdd(request.id, new List<DataItemRequest> { request }))
            {
                return true;
            }
            // if this is a new request for a given id
            if (!_pausedTags[request.id].Contains(request))
            {
                // add the request to the existing subscriptions
                _pausedTags[request.id].Add(request);
                return true;
            }
            // something went wrong
            return false;
        }

        /// <summary>
        /// Tries to resume a previsouly paused request
        /// </summary>
        /// <param name="request"><see cref="DataItemRequest"/> to resume</param>
        /// <returns><see langword="true"/> if the request was unpaused</returns>
        public bool TryResumeMonitor(DataItemRequest request)
        {
            // is this request paused?
            if (!IsPaused(request))
            {
                return false;
            }
            // remove it from the paused list
            if (_pausedTags.TryRemove(request.id, out _))
            {
                // any more paused requests for this id?
                if (_pausedTags[request.id].Count == 0)
                {
                    // remove the id key from the paused tags map
                    return _pausedTags.Remove(request.id, out _);
                }
                // still other paused requests for this id
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a <see cref="DataItemRequest"/> to the list of subscribed tags if it does not already exist.
        /// </summary>
        /// <param name="request"><see cref="DataItemRequest"/> to try to add</param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="DataItemRequest"/> was added as a subscription to
        /// the tagId
        /// </returns>
        public bool TryStartMonitor(DataItemRequest request)
        {
            // if this is the first time the id was subscribed, create the new entry
            if (_subscribedTags.TryAdd(request.id, new List<DataItemRequest> { request }))
            {
                return true;
            }
            // if this is a new request for a given id
            if (!_subscribedTags[request.id].Contains(request))
            {
                // add the request to the existing subscriptions
                _subscribedTags[request.id].Add(request);
                return true;
            }
            // something went wrong
            return false;
        }

        /// <summary>
        /// Unsubscribes the request
        /// </summary>
        /// <param name="request"><see cref="DataItemRequest"/> to StopMonitor</param>
        /// <returns><see langword="true"/> if the Stop monitor request succeeds</returns>
        public bool TryStopMonitor(DataItemRequest request)
        {
            // if we aren't subscribed then there is nothing to do
            if (!IsSubscribed(request))
            {
                return false;
            }
            // remove this request from subscribed tags
            if (_subscribedTags[request.id].Remove(request))
            {
                // remove from the paused list after removing from the subscribed list so that there are
                // no transient responses sent for this tag between removals
                _ = TryResumeMonitor(request);
                // any more requests for this id?
                if (_subscribedTags[request.id].Count == 0)
                {
                    // remove id key from map
                    return _subscribedTags.Remove(request.id, out _);
                }
                return true;
            }
            return false;
        }

        #endregion Public Methods
    }
}