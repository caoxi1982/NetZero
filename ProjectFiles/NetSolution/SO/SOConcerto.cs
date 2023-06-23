using Cca.Cgp.Common.Model;
using Cca.Cgp.Common.Model.Interfaces;
using Cca.Cgp.Core.Base;
using Cca.Cgp.Core.Base.Ia;
using Cca.Cgp.Core.Base.Interfaces;
using Cca.Cgp.Core.Base.RequestStrategies;
using Cca.Cgp.Core.Base.ResponseStrategies;
using Cca.Extensions.Common;
using Cca.Extensions.Common.Util;
using FTOptix.HMIProject;
using NetZero.Internal;
using NetZero.SO.Strategies.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using UAManagedCore;

//using EnergyApp.SO.Observers.Response;
//using EnergyApp.SO.Observers.Request;

namespace NetZero.SO
{
    public class SOConcerto : Concerto
    {
        // static singleton instance of this concerto
        private static readonly SOConcerto _instance = new SOConcerto();

        private static readonly object s_lockModel = new object();
        private static readonly Dictionary<string, long> _lastUpdates = new Dictionary<string, long>();

        // keep track of the last time we transmitted each tag id
        private static readonly Dictionary<object, DateTime> _tagLastTransmit = new Dictionary<object, DateTime>();

        private static readonly Dictionary<string, Vqt> _tagLastValue = new Dictionary<string, Vqt>();
        private static readonly Dictionary<string, string> _tagToMimeType = new Dictionary<string, string>();

        // get defaults for looping and sending responses
        private static int _delayMs = CommonConfig.TryGetValue<int>(Constants.OPTION_MAIN_LOOP_INTERVAL, out int delay) ? delay : 1000;

        private static int _chunkIntervalMs = CommonConfig.TryGetValue<int>("_chunkIntervalMs", out int intervalMs) ? intervalMs : 1000;
        private static int _chunkIntervalSize = CommonConfig.TryGetValue<int>("_chunkIntervalSize", out int intervalSize) ? intervalSize : 1000;

        //
        private static System.Timers.Timer _mainTimer = new System.Timers.Timer(500);

        private static bool _first = true;

        private bool _disposed;

        public new static SOConcerto Instance => _instance;

#nullable disable

        /// <summary>
        /// initialize and start the concerto
        /// </summary>
        /// <param name="options"></param>
        /// <param name="audience"></param>
        /// <param name="instrument"></param>
        /// <returns></returns>
        public override async Task Start(IDictionary<string, object> options, ICgpComms audience, ICgpComms instrument, bool loadDefaultStrategies)
        {
            AddResponseStrategy(new NewSmartObjects(this));

            Audience = audience;
            Instrument = instrument;

            if (Audience is { })
            {
                // load default Request strategies
                this.LoadDefaultStrategies();
            }
            if (Instrument is { })
            {
                // load default Response strategies
                ((IResponseHandler)this).LoadDefaultStrategies();
            }

            // main timer to periodically send new values
            _mainTimer.Interval = _delayMs;
            _mainTimer.Elapsed += _mainTimer_Elapsed;
            _mainTimer.Start();

            // call base class startup
            // this will actually connect to the broker/shell
            // and communications will begin
            await base.StartInternal(options, audience, instrument, loadDefaultStrategies).ConfigureAwait(false);
        }

        public void ProcessControllerConfigurations(IUANode owner, string coreDirectory)
        {
            var parent = owner.Owner;

            foreach (var soControllerConfig in parent.Children)
            {
                if (soControllerConfig.ToString() == "SmartObjectImporter")
                {
                    var controllerName = soControllerConfig.BrowseName;
                    var controllerPath = ((SmartObjectImporter)soControllerConfig).IPAddress;
                    var fileCount = 0;

                    var file1 = Path.Combine(coreDirectory, $"{controllerName}1");
                    var file2 = Path.Combine(coreDirectory, $"{controllerName}2");

                    if (System.IO.File.Exists(file1))
                    {
                        fileCount++;
                    }
                    else
                    {
                        Log.Error("Smart Objects", $"Can't find model file for {controllerName}");
                    }

                    if (System.IO.File.Exists(file2))
                    {
                        fileCount++;
                    }
                    else
                    {
                        Log.Error("Smart Objects", $"Can't find data file for {controllerName}");
                    }

                    List<ModelList> list;
                    List<Datatypes> data;

                    if (fileCount == 2)

                    {
                        list = GetModelList(file1, soControllerConfig);
                        if (list.Count > 0)
                        {
                            data = GetDataTypesList(file2);

                            var action = new ActionData();

                            action.Add("#models", ActionType.StartMonitor, $"ra-stx://cgp-so/driver-cip/{controllerPath}", "StartingMonitor").actionOptions = new SOConcertoPayload
                            {
                                modelList = list.ToArray(),
                                datatypes = data.ToArray(),
                                version = new Random().Next(0, 1000000)
                            };

                            Log.Info("Smart Objects", $"Starting monitor for {controllerName}");
                            QueueRequest(action);
                        }
                    }
                }
            }
        }

        private List<ModelList> GetModelList(string fileName, IUANode controller)
        {
            var fileRead = new StreamReader(fileName);
            var fileData = fileRead.ReadToEnd();
            fileRead.Dispose();

            fileData = fileData[16..^2];

            var controllerModel = JsonSerializer.Deserialize<ControllerCOA>(fileData);
            var list = new List<ModelList>();

            foreach (var modelItem in controllerModel.items)
            {
                var modelConfiguration = controller.GetVariable($"{Common.ModelNamePrefix}{modelItem.name}");

                if (modelConfiguration != null)
                {
                    if (modelConfiguration.Value != null)
                    {
                        var targetNode = InformationModel.GetObject(modelConfiguration.Value);

                        if (targetNode != null)
                        {
                            list.AddRange(ProcessModelItem(modelItem, "", targetNode));
                        }
                    }
                }
            }

            return list;
        }

        private List<ModelList> ProcessModelItem(COAModel modelItem, string parentFQN, IUANode parentNode)
        {
            int p1;
            var list = new List<ModelList>();

            switch (modelItem.mimeType)
            {
                case "x-ra/clx/raC_UDT_SO_Node":
                case "x-ra/clx/models":
                    {
                        foreach (var child in modelItem.items)
                        {
                            var itemNode = parentNode.GetObject(modelItem.name);
                            if (itemNode != null)
                            {
                                list.AddRange(ProcessModelItem(
                                    child,
                                    $"{parentFQN}{modelItem.name}.",
                                    itemNode));
                            }
                            else
                            {
                                Log.Warning("Smart Objects", $"Could not find {parentFQN}{modelItem.name} in model");
                            }
                        }

                        break;
                    }
                default:
                    {
                        var itemNode = parentNode.GetVariable(modelItem.name);

                        if (itemNode != null)
                        {
                            list.Add(new ModelList
                            {
                                fqn = $"{parentFQN}{modelItem.name}",
                                dsId = itemNode.NodeId.ToString(),
                                ns = itemNode.NodeId.NamespaceIndex,
                                guids = itemNode.NodeId.Id.ToString(),
                                typeid = (uint)itemNode.ActualDataType.Id
                            });
                        }
                        else
                        {
                            Log.Warning("Smart Objects", $"Could not find {parentFQN}{modelItem.name} in model");
                        }

                        break;
                    }
            }

            return list;
        }

        private List<Datatypes> GetDataTypesList(string fileName)
        {
            var fileRead = new StreamReader(fileName);
            var fileData = fileRead.ReadToEnd();
            fileRead.Dispose();

            var controllerDataTypes = JsonSerializer.Deserialize<ControllerDataType>(fileData);
            var list = new List<Datatypes>();

            foreach (var item in controllerDataTypes.collections)
            {
                if (item.name == "Data Types")
                {
                    foreach (var dataType in item.items)
                    {
                        if (!dataType.name.Contains(':'))
                        {
                            list.Add(dataType);
                        }
                    }
                }
            }

            return list;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                if (disposing)
                {
                    if (_mainTimer is { })
                    {
                        _mainTimer.Stop();
                        _mainTimer.Dispose();
                    }
                }
                _disposed = true;

                base.Dispose(disposing);
            }
            finally
            {
                _disposed = true;
            }
        }

        /// <summary>
        /// Fires at the mainLoopInterval
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
#nullable enable

        private void _mainTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!SubscribedTagsMap.IsEmpty)
            {
                ConcertoMain();
            }
        }

#nullable disable
    }
}