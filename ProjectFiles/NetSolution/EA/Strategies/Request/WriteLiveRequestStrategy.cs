using Cca.Cgp.Core.Base;
using Cca.Cgp.Core.Base.Extensions;
using Cca.Cgp.Core.Base.Ia;
using Cca.Cgp.Core.Base.Interfaces;
using Cca.Extensions.Common;
using FTOptix.HMIProject;
using System;
using System.Collections.Generic;
using UAManagedCore;

namespace NetZero.EA.Strategies.Request
{
    internal struct WritePayload
    {
        public object v { get; set; }
        public string id { get; set; }
    }

    public class WriteLiveRequestStrategy : RequestStrategy
    {
        public WriteLiveRequestStrategy(IRequestHandler requestHandler)
            : base(requestHandler)
        {
            // change priority according to your needs
            Priority = 30;
        }

        /// <summary>
        /// handles an individual request
        /// </summary>
        /// <param name="request"><see cref="DataItemRequest"/> to process</param>
        public override void HandleMessage(DataItemRequest request)
        {
            // this handles messages one by one
            var vqt = Cache.ContainsID(request.id) ? Cache.GetLatestVqt(request.id) : null;
            if (vqt is { })
            {
                // send the last cached value
                RequestHandler.QueueResponse(request.GetReactionFromVqt(vqt, MimeTypeExtensions.DOUBLE_TAG_TYPE));
            }
            else
            {
                // send an error response
                RequestHandler.QueueResponse(request.GetErrorReaction($"No cached values for {request.id}"));
            }
        }

        /// <summary>
        /// accepts all incoming messages that are handled
        /// by this strategy, and then passes them
        /// one-by-one to the HandleMessage method
        /// </summary>
        /// <param name="requests"><see cref="IEnumerable{DataItemRequest}"/> to check</param>
        public override void HandleMessages(IEnumerable<DataItemRequest> requests)
        {
            foreach (var request in requests)
            {
                try
                {
                    var payload = System.Text.Json.JsonSerializer.Deserialize<WritePayload>(request.actionOptions.ToString());
                    var nodeIDParts = payload.id.Split('/');
                    var targetVariable = InformationModel.GetVariable(new NodeId(Convert.ToInt32(nodeIDParts[0]), new Guid(nodeIDParts[1])));

                    if (targetVariable is { })
                    {
                        switch ((uint)targetVariable.ActualDataType.Id)
                        {
                            case 12:
                                {
                                    targetVariable.DataValue = new DataValue(new UAValue(payload.v.ToString()), 192, DateTime.UtcNow);
                                    break;
                                }
                            default:
                                {
                                    targetVariable.DataValue = new DataValue(new UAValue(Convert.ToDouble(payload.v.ToString())), 192, DateTime.UtcNow);
                                    break;
                                }
                        }

                        RequestHandler.QueueResponse(request.GetAckReaction());
                    }
                    else
                    {
                        RequestHandler.QueueResponse(request.GetErrorReaction($"Can't fild {request.id}"));
                    }
                }
                catch
                {
                    RequestHandler.QueueResponse(request.GetErrorReaction($"Fail to write {request.id}"));
                }
            }
        }

        public override bool Handles(DataItemRequest request)
        {
            if (request.actionType == ActionType.Write)
            {
                return true;
            }

            return false;
        }
    }
}
