using Cca.Cgp.Common.Model;
using Cca.Cgp.Core.Base.Ia;
using Cca.Cgp.Core.ZmqConductor;
using Cca.Extensions.Common.Logging;
using Cca.Extensions.Common.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UAManagedCore;

namespace NetZero.EA
{
    internal class StartFunctions
    {
        public static Dictionary<string, object> GetNetLogicConfig()
        {
            var options = new Dictionary<string, object>()
            {
                { "ip", "127.0.0.1"},
                { "port", 60000},
                { "name", "ftoptix-01"},
                { "mainLoopInterval", 1000},
                { "loggingLevel", 1 }
            };

            return options;
        }

        public void InitialzeConcerto()
        {
            Log.Info(nameof(StartFunctions), "InitialzeConcerto - enter");
            CommonConfig.Load(GetNetLogicConfig());
            Logging.SetupStaticLogger(CommonConfig.Options, debug: true);

            //var FTOptixConcerto.Instance = FTOptixConcerto.Instance;
            var audience = new ZmqConcertoComms(CommonConfig.Options);
            var instrument = audience;
            Log.Info(nameof(StartFunctions), "InitialzeConcerto - before FtoStart");
            FTOptixConcerto.Instance.FtoStart(CommonConfig.Options, audience, instrument);
            Log.Info(nameof(StartFunctions), "InitialzeConcerto - after FtoStart");
            Log.Info(nameof(StartFunctions), "InitialzeConcerto - exit");
        }

        public void SendModelOnce(Node root, Node types)
        {
            CommonConfig.Load(GetNetLogicConfig());
            Logging.SetupStaticLogger(CommonConfig.Options, debug: true);
            var finished = false;
            Task.Factory.StartNew(async () =>
                        {
                            using (var concerto = new ZmqConcerto())
                            {
                                try
                                {
                                    var options = CommonConfig.Options;
                                    var name = options["name"];
                                    var actionData = new ActionData();
                                    await concerto.StartApplication(options, false).ConfigureAwait(false);
                                    actionData.Add("#model", ActionType.Write, "ctx://manager-svc").actionOptions = root;
                                    actionData.Add("#types", ActionType.Write, "ctx://model-svc").actionOptions = types;
                                    var response = await concerto.GetResponseAsync(actionData, 10000).ConfigureAwait(false);
                                    Log.Info(nameof(SendModelOnce), $"Send model {(response is { } ? "Success" : "Fail")}");
                                }
                                catch (Exception ex)
                                {
                                    Log.Info(nameof(SendModelOnce), ex.Message);
                                }
                                finally
                                {
                                    finished = true;
                                }
                            }
                        }, TaskCreationOptions.PreferFairness);

            while (!finished)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
