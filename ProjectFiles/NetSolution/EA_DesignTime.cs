#region Using directives

using Cca.Cgp.Common.Model;
using Cca.Cgp.Common.Model.Extensions;
using Cca.Cgp.Core.Base.Ia;
using Cca.Extensions.Common;
using Cca.Extensions.Common.Util;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Retentivity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NetZero.EA;
using NetZero.Extensions;
using System.Collections.Generic;
using System.Linq;
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

#endregion

public class EA_DesignTime : BaseNetLogic
{
    #region Private Fields

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

    private static readonly System.Type TAG_TYPE = typeof(FTOptix.CommunicationDriver.Tag);
    private static readonly System.Type VAR_TYPE = typeof(IUAVariable);

    #endregion Private Fields

    [ExportMethod]
    public void TransferModel()
    {
        try
        {
            var cgpOptions = new StartFunctions();
            var rootNode = LogicObject.Children.GetVariable("RootNode");
            var buildModel = new BuildModel();
            var root = buildModel.GetModel(CommonConfig.Options["name"].ToString(), rootNode, false);

            if (root is { })
            {
                cgpOptions.SendModelOnce(root[0], root[1]);
            }
            else
            {
                Log.Error(nameof(EA_DesignTime), "Failed to find model");
            }
        }
        catch
        {
            Log.Error(nameof(EA_DesignTime), "Error sending model to Edge App");
        }
    }

    public void GetAllChildren(IUANode node, JsonTextWriter writer, ref int depth)
    {
        depth++;

        if (node is IUAMethod)
        {
            return;
        }

        writer.WriteStartObject();
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

    public static Node GetModel(IUAObject logicObject)
    {
        GatewayModels.Clear();
        var rootPath = $"@FTOptix:{Project.Current.BrowseName}/";
        Node root = null;
        try
        {
            var modelFolder = Project.Current.Get("Model/BrokerModel/Root");
            if (modelFolder is { })
            {
                var model = InformationModel.Get(modelFolder.NodeId);
                //Log.Error("after model");
                if (model is { })
                {
                    root = new Node
                    {
                        Type = MimeTypeExtensions.FTEG_ROOT_TYPE,
                        Name = model.BrowseName,
                        FullyQualifiedName = $"{model.BrowseName}",
                        OriginalName = model.BrowseName,
                        ValType = (int)ValueTypes.Model,
                        Parent = null,
                        Id = model.NodeId.Id.ToString(),
                        Tag = null,
                        Path = $"{rootPath}{model.BrowseName}",
                        UpdateRate = 1000,
                        UpdateType = UpdateType.OnChange,
                        ReadOnly = false,
                        DataCollection = true
                    };

                    foreach (var childNode in model.GetNodesByType<IUAObject>())
                    {
                        AddNode(childNode, ref root);
                    }
                }
                else
                {
                    Log.Error("model is null");
                }
            }
            else
            {
                Log.Error("model folder is null");
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"{Project.Current.BrowseName}: {ex.Message}");
        }
        if (root is { })
        {
            GatewayModels.AddOrUpdateNode(root.Name, root);
            var path = System.IO.Path.Combine("C:\\temp", $"{Project.Current.BrowseName}-model.json");
            Writer.WriteText(path, JToken.Parse(GatewayModels.ToJson()).ToString(Formatting.Indented), true);
        }
        return root;
    }

    #region Private Methods

    private static void AddNode(IUANode node, ref Node parent)
    {
        Node child = new()
        {
            Name = node.BrowseName,
            FullyQualifiedName = $"{parent.FullyQualifiedName}.{node.BrowseName}",
            OriginalName = node.BrowseName,
            ValType = (int)ValueTypes.Node,
            Parent = new Parent { Id = parent.Id, Name = parent.Name, FullyQualifiedName = parent.FullyQualifiedName },
            Type = MimeTypeExtensions.FTEG_NODE_TYPE,
            Id = node.NodeId.Id.ToString(),
            Tag = null,
            Path = $"{parent.Path}.{node.BrowseName}",
            UpdateRate = 1000,
            UpdateType = UpdateType.OnChange,
            ReadOnly = false,
            DataCollection = true
        };

        //foreach (var tag in GetChildTags(node))
        //{
        //    AddTag(tag, ref child);
        //}

        foreach (var variable in GetChildVariables(node))
        {
            AddTag(variable, ref child);
        }

        foreach (var childNode in GetChildNodes(node))
        {
            AddNode(childNode, ref child);
        }

        parent.Children.Add(child);
    }

    private static void AddProperty(IUAVariable variable, ref Node parent)
    {
        parent.AddOrUpdateProperty(variable.BrowseName, variable.Value.Value, "Optix", false);
    }

    private static void AddTag(IUAVariable variable, ref Node parent)
    {
        var mimeType = MimeTypeExtensions.GetMimeType(variable.DataValue.Value.Value.GetType());
        Node child = new()
        {
            Type = MimeTypeExtensions.FTEG_PROPERTY_TYPE,
            Name = variable.BrowseName,
            FullyQualifiedName = $"{parent.FullyQualifiedName}.{variable.BrowseName}",
            OriginalName = variable.BrowseName,
            ValType = (int)ValueTypesEx.FromMimeType(mimeType),
            Parent = new Parent { Id = parent.Id, Name = parent.Name, FullyQualifiedName = parent.FullyQualifiedName },
            Id = variable.NodeId.Id.ToString(),
            Tag = null,
            Path = $"{parent.Path}.{variable.BrowseName}",
            UpdateRate = 1000,
            UpdateType = UpdateType.OnChange,
            ReadOnly = false,
            DataCollection = true
        };

        FTOptixConcerto.Instance.AddNodeId(child.FullyQualifiedName, variable.NodeId);

        //child.AddOrUpdateProperty("DataTypeIdTypeShortString", variable.DataType.IdTypeShortString, "Optix");
        //child.AddOrUpdateProperty("DataValueValueType", variable.DataValue.Value.Value.GetType().Name, "Optix");

        child.Tag = new Cca.Cgp.Common.Model.Tag()
        {
            Id = IdGenerator.GenerateId(),
            Name = variable.BrowseName,
            OverWrite = true,
            ReadOnly = false,
            Path = $"{parent.Path}.{variable.BrowseName}",
            Type = mimeType,
            ValType = (int)ValueTypesEx.FromMimeType(variable.DataType.Id.ToString())
        };

        //var childTags = variable.GetNodesByType<FTOptix.CommunicationDriver.Tag>();

        foreach (var v1 in GetChildVariables(variable))
        {
            //child.AddOrUpdateProperty(v1.BrowseName, v1.Value.Value, "Optix");
            AddTag(v1, ref child);
        }

        //var variables = tag.GetNodesByType<IUAVariable>().Where(v => !v.GetType().IsAssignableFrom(TAG_TYPE));

        //foreach (var variable in GetChildVariables(tag))
        //{
        //    child.AddOrUpdateProperty(variable.BrowseName, variable.Value.Value, "Optix");
        //}

        //var childNodes = tag.GetNodesByType<IUAObject>().Where(n => !n.GetType().IsAssignableFrom(TAG_TYPE) && !n.GetType().IsAssignableFrom(VAR_TYPE));

        foreach (var n1 in GetChildNodes(variable))
        {
            AddNode(n1, ref child);
        }

        parent.Children.Add(child);
    }

    private static IEnumerable<IUANode> GetChildNodes(IUANode node)
    {
        return node.GetNodesByType<IUAObject>().Where(n => !n.GetType().IsAssignableFrom(TAG_TYPE) && !n.GetType().IsAssignableFrom(VAR_TYPE));
    }

    private static IEnumerable<FTOptix.CommunicationDriver.Tag> GetChildTags(IUANode node)
    {
        return node.GetNodesByType<FTOptix.CommunicationDriver.Tag>();
    }

    private static IEnumerable<IUAVariable> GetChildVariables(IUANode node)
    {
        return node.GetNodesByType<IUAVariable>();
    }

    private static void OptixNodeToGatewayNode(IUANode node, ref Node thisNode)
    {
        try
        {
            Log.Error($"populating node: {node.NodeClass.ToString()}: {thisNode.FullyQualifiedName}.{node.BrowseName}");
            if (node.NodeId is { })
            {
                thisNode.AddOrUpdateProperty("NodeId", JsonConvert.SerializeObject(node.NodeId), "Optix", false);
            }
            if (node.NodeClass is { })
            {
                thisNode.AddOrUpdateProperty("NodeClass", JsonConvert.SerializeObject(node.NodeClass), "Optix", false);
            }

            thisNode.AddOrUpdateProperty("DisplayName", JsonConvert.SerializeObject(node.DisplayName), "Optix", false);

            thisNode.AddOrUpdateProperty("QualifiedBrowseName", JsonConvert.SerializeObject(node.QualifiedBrowseName), "Optix", false);

            thisNode.AddOrUpdateProperty("IsValid", node.IsValid, "Optix", false);

            if (node.Description is { })
            {
                thisNode.AddOrUpdateProperty("Description", JsonConvert.SerializeObject(node.Description.Text), "Optix", false);
            }
        }
        catch
        {
        }
    }

    private static void OptixNodeToGatewayNode(ProjectFolder folder, ref Node thisNode)
    {
        try
        {
            Log.Error($"populating folder: {folder.NodeClass.ToString()}: {thisNode.FullyQualifiedName}.{folder.BrowseName}");
            if (folder.NodeId is { })
            {
                thisNode.AddOrUpdateProperty("NodeId", JsonConvert.SerializeObject(folder.NodeId), "Optix", false);
            }
            if (folder.NodeClass is { })
            {
                thisNode.AddOrUpdateProperty("NodeClass", JsonConvert.SerializeObject(folder.NodeClass), "Optix", false);
            }

            thisNode.AddOrUpdateProperty("DisplayName", JsonConvert.SerializeObject(folder.DisplayName), "Optix", false);

            thisNode.AddOrUpdateProperty("QualifiedBrowseName", JsonConvert.SerializeObject(folder.QualifiedBrowseName), "Optix", false);

            thisNode.AddOrUpdateProperty("IsValid", folder.IsValid, "Optix", false);

            if (folder.Description is { })
            {
                thisNode.AddOrUpdateProperty("Description", JsonConvert.SerializeObject(folder.Description.Text), "Optix", false);
            }
        }
        catch
        {
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
                    try
                    {
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

    private void TrySerialize(StructDefinition definition, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        try
        {
            writer.WritePropertyName("name");
            writer.WriteValue(definition.Name);

            writer.WritePropertyName("dataTypeId");
            TrySerialize(definition.DataTypeId as NodeId, writer);

            writer.WritePropertyName("isEmpty");
            writer.WriteValue(definition.IsEmpty);

            writer.WritePropertyName("fields");
            TrySerialize(definition.Fields, writer);
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

    private static void TrySerialize(LocalizedText text, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        try
        {
            writer.WritePropertyName("localeId");
            writer.WriteValue(text.LocaleId);

            writer.WritePropertyName("namespaceIndex");
            writer.WriteValue(text.NamespaceIndex);

            writer.WritePropertyName("hasTranslation");
            writer.WriteValue(text.HasTranslation);

            writer.WritePropertyName("text");
            writer.WriteValue(text.Text);
            writer.WritePropertyName("textId");
            writer.WriteValue(text.TextId);
        }
        catch
        {
            writer.WritePropertyName("error");
            writer.WriteValue("error");
        }
        writer.WriteEndObject();
    }

    private static void TrySerialize(UAReference reference, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        try
        {
            writer.WritePropertyName("reference");
            writer.WriteValue(reference.ToString());
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

    private void TrySerialize(FTOptix.CommunicationDriver.TagStructure tagStructure, JsonTextWriter writer)
    {
        writer.WritePropertyName("tagStructure");
        writer.WriteStartObject();
        try
        {
            writer.WritePropertyName("isValid");
            writer.WriteValue(tagStructure.IsValid);
            writer.WritePropertyName("nodeId");
            TrySerialize(tagStructure.NodeId, writer);
            writer.WritePropertyName("nodeClass");
            TrySerialize(tagStructure.NodeClass, writer);

            writer.WritePropertyName("prototype");
            TrySerialize(tagStructure.Prototype, writer);

            writer.WritePropertyName("QualifiedBrowseName");
            TrySerialize(tagStructure.QualifiedBrowseName, writer);
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
