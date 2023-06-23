#region Using directives

using Cca.Cgp.Common.Model;
using Cca.Cgp.Core.Base;
using Cca.Cgp.Core.Base.Extensions;
using Cca.Cgp.Core.Base.Ia;
using Cca.Extensions.Common.Util;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Retentivity;
using NetZero.EA;
using NetZero.Extensions;
using System.Collections.Generic;
using System.Text;
using UAManagedCore;
using FTOptix.NativeUI;
using FTOptix.UI;
using FTOptix.OPCUAServer;
using FTOptix.WebUI;
using FTOptix.S7TCP;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using Newtonsoft.Json;

#endregion

public class EA_Runtime : BaseNetLogic
{
    public static EA_Runtime Instance => _instance;
    public RemoteVariableSynchronizer _synchronizer;

    private DelayedTask _startupTask;

    #region Private Fields

    private static EA_Runtime _instance;

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Formatting = Formatting.Indented,
        MaxDepth = null,
        NullValueHandling = NullValueHandling.Ignore
    };

    private static readonly JsonSerializerSettings _jsonSettingsValue = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Formatting = Formatting.None,
        MaxDepth = null,
        NullValueHandling = NullValueHandling.Ignore
    };

    private static readonly object _lock = new();

    // the Concerto
    //private static FTOptixConcerto FTOptixConcerto.Instance;

    #endregion Private Fields

    #region Public Methods

    public void Variable_VariableChange(object sender, VariableChangeEventArgs e)
    {
        var variable = e.Variable;
        var vqt = variable.GetVqt(variable.DataType.GetSimpleDataType());
        if (vqt is { })
        {
            //Cache.UpdateValue(variable.NodeId.Id.ToString(), vqt, variable.DataType.GetSimpleDataType(), false, true);
            var fqn = FqnLookup.GetFqnFromId(e.Variable.NodeId.ToString());
            if (!FTOptixConcerto.Instance.SubscribedTagsMap.ContainsKey(fqn))
            {
                return;
            }
            if (!string.IsNullOrEmpty(fqn) && FTOptixConcerto.Instance.SubscribedTagsMap.TryGetValue(fqn, out List<DataItemRequest> requests))
            {
                foreach (var request in requests)
                {
                    var responseData = new ReactionData();
                    responseData.Add(request.GetResponseFromVqt(vqt));
                    FTOptixConcerto.Instance.QueueResponse(responseData);
                }
            }
        }
    }

    public void GetAllChildren(IUANode node, JsonTextWriter writer, ref int depth)
    {
        depth++;

        if (node is IUAMethod)
        {
            return;
        }

        writer.WriteStartObject(); // node
        if (node.BrowseName == "Root")
        {
            var rootVar = node as IUAVariable;
            if (rootVar is { })
            {
                var tagRoot = InformationModel.Get(rootVar.Value);
                var tags = OptixHelpers.GetNodesIntoFolder<FTOptix.CommunicationDriver.Tag>(tagRoot);
                writer.WritePropertyName("browseName");
                writer.WriteValue(node.BrowseName);
                writer.WritePropertyName("dotnetType");
                writer.WriteValue($"{node.GetType().FullName}");
                writer.WritePropertyName("nodeId");
                TrySerialize(node.NodeId as NodeId, writer);
                writer.WritePropertyName("nodeClass");
                TrySerialize(node.NodeClass, writer);
                writer.WritePropertyName("qualifiedBrowseName");
                TrySerialize(node.QualifiedBrowseName, writer);

                writer.WritePropertyName("tags");
                writer.WriteStartArray();
                foreach (FTOptix.CommunicationDriver.Tag tag in tags)
                {
                    TrySerialize(tag, writer);
                }
                writer.WriteEndArray();
            }
        }
        else
        {
            writer.WritePropertyName("browseName");
            writer.WriteValue(node.BrowseName);
            writer.WritePropertyName("dotnetType");
            writer.WriteValue($"{node.GetType().FullName}");

            //writer.WritePropertyName("description");
            //TrySerialize(node.Description, writer);
            //writer.WritePropertyName("displayName");
            //TrySerialize(node.DisplayName, writer);
            writer.WritePropertyName("nodeId");
            TrySerialize(node.NodeId as NodeId, writer);

            //writer.WritePropertyName("owner");
            //TrySerialize(node.Owner, writer);
            writer.WritePropertyName("nodeClass");
            TrySerialize(node.NodeClass, writer);
            writer.WritePropertyName("isValid");
            writer.WriteValue(node.IsValid);

            writer.WritePropertyName("qualifiedBrowseName");
            TrySerialize(node.QualifiedBrowseName, writer);

            if (node is IUAVariable variable)
            {
                writer.WritePropertyName("variableData");
                TrySerialize(variable, writer);
            }

            if (node is IUAMethod method)
            {
                writer.WritePropertyName("methodInfo");
                TrySerialize(method, writer);
            }

            if (node is UADataType dataType)
            {
                writer.WritePropertyName("dataType");
                TrySerialize(dataType, writer);
            }

            if (node is UAValue value)
            {
                writer.WritePropertyName("value");
                TrySerialize(value, writer);
            }

            writer.WritePropertyName("children");
            writer.WriteStartArray(); // children
            foreach (var child in node.Children)
            {
                GetAllChildren(child, writer, ref depth);
            }

            writer.WriteEndArray(); // children
        }
        writer.WriteEndObject(); // node
        depth--;
    }

    //private PeriodicTask? _writeCacheTask;

    public override void Start()
    {
        _startupTask = new DelayedTask(StartUpTask, 25000, LogicObject);
        _instance = this;
        _startupTask.Start();
    }

    private void StartUpTask()
    {
        var cgpOptions = new StartFunctions();
        cgpOptions.InitialzeConcerto();
        FTOptixConcerto.Instance.NetLogicObject = (NetLogicObject)LogicObject;
        //FTOptixConcerto.Instance.ConcertoMain();
        _synchronizer = new RemoteVariableSynchronizer();

        var parent = LogicObject.Owner;
        var rootNode = parent.Children.GetObject("EA_DesignTime").Children.GetVariable("RootNode");
        var buildModel = new BuildModel();
        var root = buildModel.GetModel(CommonConfig.Options["name"].ToString(), rootNode, true);
        try
        {
            if (root is { })
            {
                Log.Info(nameof(EA_Runtime), "StartUpTask - sending model");
                FTOptixConcerto.Instance.WriteModel(root[0], root[1]);
                GatewayModels.Clear();
                GatewayModels.CacheNode(root[0]);
                var models = JsonConvert.SerializeObject(root, Formatting.Indented);
            }
        }
        catch (System.Exception ex)
        {
            Log.Error(nameof(EA_Runtime), $"StartUpTask: {ex.Message}");
        }
    }

    public override void Stop()
    {
        _startupTask?.Dispose();
        //_writeCacheTask?.Cancel();
        //_writeCacheTask?.Dispose();
        try
        {
            FTOptixConcerto.Instance?.Dispose();
        }
        catch (System.Exception ex)
        {
            Log.Error(nameof(EA_Runtime), ex.Message);
        }
        // Insert code to be executed when the user-defined logic is stopped
    }

    #endregion Public Methods

    #region Private Methods

    private static void AddProperty(IUAVariable variable, ref Node parent)
    {
        parent.AddOrUpdateProperty(variable.BrowseName, variable.Value.Value, "Optix", false);
    }

    private void GetChildren(IUANode node, int depth, ref StringBuilder sb)
    {
        foreach (var child in node.Children)
        {
            sb.AppendLine(new string('\t', depth) + child.BrowseName);
            if (child.Children.Count > 0)
            {
                GetChildren(child, depth + 1, ref sb);
            }
        }
    }

    private void GetChildren(IUANode node, Node parent)
    {
        if (node is { })
        {
            try
            {
                var path = System.IO.Path.Combine("C:\\temp", $"{node.BrowseName}-export.json");
                var json = JsonConvert.SerializeObject(node, _jsonSettings);
                Writer.WriteText(path, json, true);
            }
            catch (System.Exception ex)
            {
                Log.Error($"{node.BrowseName} export: {ex.Message}");
            }

            if (node.BrowseName == "Retentivity")
            {
                return;
            }
            if (node.NodeClass == NodeClass.Variable)
            {
                var nodeVar = node as IUAVariable;
                if (nodeVar is { })
                {
                    AddProperty(nodeVar, ref parent);
                }
            }
            else if (node is PasswordPolicy pwdPolicy)
            {
                return;
            }
            else if (node is RetentivityStorage)
            {
                return;
            }
            else if (node.NodeClass == NodeClass.Object)
            {
                //Log.Error($"Creating: {parent.FullyQualifiedName}.{node.BrowseName}");
                //INode thisNode = GatewayModels.GetOrCreateNodeFromFQN($"{parent.FullyQualifiedName}.{node.BrowseName}");
                var thisNode = new Node
                {
                    Type = "x-ra/fteg/node",
                    Name = node.BrowseName,
                    FullyQualifiedName = $"{parent.FullyQualifiedName}.{node.BrowseName}",
                    OriginalName = $"{parent.FullyQualifiedName}.{node.BrowseName}",
                    ValType = (int)ValueTypes.Node,
                    Parent = new Parent { Id = parent.Id, Name = parent.Name, FullyQualifiedName = parent.FullyQualifiedName },
                    DataCollection = true,
                    Id = node.BrowseName,
                    OverWrite = false,
                    Path = $"{parent.FullyQualifiedName}.{node.BrowseName}",
                    ReadOnly = true,
                    UpdateRate = 0,
                    UpdateType = Cca.Cgp.Core.Base.Ia.UpdateType.OnChange
                };
                if (thisNode is { })
                {
                    //Log.Error($"Created: {parent.FullyQualifiedName}.{node.BrowseName}");
                    try
                    {
                        //thisNode.AddOrUpdateProperty("ftoNode", json, "FTOptix", false);
                        //if (node is FTOptix.CommunicationDriver.Tag ftoTag)
                        //{
                        //    Log.Error($"tag: {parent.FullyQualifiedName}.{node.BrowseName}");
                        //    thisNode.Type = "x-ra/fteg/property";
                        //    Cca.Cgp.Common.Model.Tag tag = new Cca.Cgp.Common.Model.Tag
                        //    {
                        //        Id = ftoTag.DisplayName.Text,
                        //        Name = ftoTag.DisplayName.Text,
                        //        OverWrite = false,
                        //        ReadOnly = true,
                        //        Path = ftoTag.BrowseName,
                        //        Type = "x-ra/cip/real",
                        //        ValType = (int)ValueTypes.Float
                        //    };
                        //    thisNode.Tag = tag;
                        //    thisNode.AddOrUpdateProperty("tagDef", JToken.Parse(JsonConvert.SerializeObject(tag)).ToString(Formatting.Indented), "Optix", false);
                        //}
                        parent.Children.Add(thisNode);
                        foreach (var child in node.Children)
                        {
                            if (child.Children?.Count > 0)
                            {
                                GetChildren(child, thisNode);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error(ex.Message);
                    }
                }
                else
                {
                    Log.Error("Node not created");
                }
            }
        }
    }

    private static void TrySerialize(object node, JsonTextWriter writer)
    {
        //writer.WriteStartObject();
        try
        {
            writer.WriteValue(JsonConvert.SerializeObject(node, _jsonSettingsValue));
        }
        catch
        {
            writer.WriteValue("error");
        }
        //writer.WriteEndObject();
    }

    private static void TrySerialize(UAValue value, JsonTextWriter writer)
    {
        //writer.WriteStartObject();
        try
        {
            if (value is { } && value.Value is { })
            {
                writer.WriteValue(value.Value);
            }
            else
            {
                writer.WriteNull();
            }
        }
        catch
        {
            //writer.WritePropertyName("value");
            writer.WriteNull();
        }
        //writer.WriteEndObject();
    }

    private void TrySerialize(DataValue value, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        try
        {
            writer.WritePropertyName("serverTimestamp");
            writer.WriteValue(value.ServerTimestamp);

            writer.WritePropertyName("sourceTimestamp");
            writer.WriteValue(value.SourceTimestamp);

            writer.WritePropertyName("value");
            TrySerialize(value.Value, writer);

            writer.WritePropertyName("statusCode");
            writer.WriteValue(value.StatusCode);
        }
        catch
        {
            writer.WritePropertyName("error");
            writer.WriteValue("error");
        }
        writer.WriteEndObject();
    }

    private void TrySerialize(IUAVariable variable, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        try
        {
            writer.WritePropertyName("actualDataType");
            TrySerialize(variable.ActualDataType, writer);

            writer.WritePropertyName("dataType");
            TrySerialize(variable.DataType, writer);

            writer.WritePropertyName("dataValue");
            TrySerialize(variable.DataValue, writer);

            writer.WritePropertyName("valueRank");
            TrySerialize(variable.ValueRank, writer);

            writer.WritePropertyName("value");
            TrySerialize(variable.Value, writer);
        }
        catch
        {
            writer.WritePropertyName("error");
            writer.WriteValue("error");
        }
        writer.WriteEndObject();
    }

    private static void TrySerialize(IUAMethod method, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        try
        {
            writer.WritePropertyName("methodName");
            writer.WriteValue(method.BrowseName);
        }
        catch
        {
            writer.WritePropertyName("error");
            writer.WriteValue("error");
        }
        writer.WriteEndObject();
    }

    private static void TrySerialize(IUADataType dataType, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        try
        {
            writer.WritePropertyName("isValid");
            writer.WriteValue(dataType.IsValid);
            writer.WritePropertyName("isAbstract");
            writer.WriteValue(dataType.IsAbstract);
            writer.WritePropertyName("isValid");
            writer.WriteValue(dataType.StructDefinition);
            writer.WritePropertyName("isValid");
            writer.WriteValue(dataType.IsValid);
        }
        catch
        {
            writer.WritePropertyName("error");
            writer.WriteValue("error");
        }
        writer.WriteEndObject();
    }

    private static void TrySerialize(NodeId nodeId, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        try
        {
            writer.WritePropertyName("id");
            writer.WriteValue(nodeId.Id);

            writer.WritePropertyName("namespaceIndex");
            writer.WriteValue(nodeId.NamespaceIndex);

            writer.WritePropertyName("IsEmpty");
            writer.WriteValue(nodeId.IsEmpty);

            writer.WritePropertyName("idType");
            writer.WriteValue(nodeId.IdType);

            writer.WritePropertyName("idTypeShortString");
            writer.WriteValue(nodeId.IdTypeShortString);
        }
        catch
        {
            writer.WritePropertyName("error");
            writer.WriteValue("error");
        }
        writer.WriteEndObject();
    }

    private static void TrySerialize(QualifiedName qualifiedName, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        try
        {
            writer.WritePropertyName("name");
            writer.WriteValue(qualifiedName.Name);

            writer.WritePropertyName("namespaceIndex");
            writer.WriteValue(qualifiedName.NamespaceIndex);
        }
        catch
        {
            writer.WritePropertyName("error");
            writer.WriteValue("error");
        }
        writer.WriteEndObject();
    }

    private void TrySerialize(FTOptix.CommunicationDriver.Tag tag, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        try
        {
            writer.WritePropertyName("name");
            writer.WriteValue(tag.BrowseName);
            writer.WritePropertyName("dotnetType");
            writer.WriteValue($"{tag.GetType().FullName}");

            writer.WritePropertyName("isValid");
            writer.WriteValue(tag.IsValid);
            writer.WritePropertyName("actualDataType");
            TrySerialize(tag.ActualDataType, writer);
            writer.WritePropertyName("dataType");
            TrySerialize(tag.DataType, writer);
            writer.WritePropertyName("dataValue");
            TrySerialize(tag.DataValue, writer);

            writer.WritePropertyName("nodeId");
            TrySerialize(tag.NodeId, writer);
            writer.WritePropertyName("nodeClass");
            writer.WriteValue(tag.NodeClass);
            //writer.WritePropertyName("prototype");
            //TrySerialize(tag.Prototype, writer);
            writer.WritePropertyName("QualifiedBrowseName");
            TrySerialize(tag.QualifiedBrowseName, writer);
        }
        catch
        {
            writer.WritePropertyName("error");
            writer.WriteValue("error");
        }
        writer.WriteEndObject();
    }

    #endregion Private Methods
}
