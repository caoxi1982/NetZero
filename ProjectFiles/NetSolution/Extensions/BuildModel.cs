using Cca.Cgp.Common.Model;
using Cca.Cgp.Common.Model.Extensions;
using Cca.Cgp.Core.Base.Ia;
using Cca.Extensions.Common;
using Cca.Extensions.Common.Util;
using FTOptix.HMIProject;
using NetZero.EA;
using System.Collections.Generic;
using System.Linq;
using UAManagedCore;

namespace NetZero.Extensions
{
    internal class BuildModel
    {
        public Node[] GetModel(string sourceName, IUAVariable rootNode, bool monitorValues)
        {
            _monitorValues = monitorValues;
            GatewayModels.Clear();
            Node models = null;
            Node types = null;
            try
            {
                var model = InformationModel.Get(rootNode.Value);
                if (model is { })
                {
                    models = new Node
                    {
                        Type = MimeTypeExtensions.FTEG_ROOT_TYPE,
                        Name = model.BrowseName,
                        FullyQualifiedName = $"{model.BrowseName}",
                        OriginalName = model.BrowseName,
                        ValType = (int)ValueTypes.Model,
                        Parent = null,
                        Id = model.NodeId.ToString(),
                        Tag = null,
                        Path = $"{sourceName}/{model.BrowseName}",
                        UpdateRate = 1000,
                        UpdateType = UpdateType.OnChange,
                        ReadOnly = false,
                        DataCollection = true
                    };

                    types = new Node
                    {
                        Type = MimeTypeExtensions.FTEG_ROOT_TYPE,
                        Name = "Types",
                        FullyQualifiedName = "Types",
                        OriginalName = "Types",
                        ValType = (int)ValueTypes.Model,
                        Parent = null,
                        Id = "0",
                        Tag = null,
                        Path = "NA",
                        UpdateRate = 1000,
                        UpdateType = UpdateType.OnChange,
                        ReadOnly = true,
                        DataCollection = false
                    };

                    foreach (var childNode in model.GetNodesByType<IUAObject>())
                    {
                        AddNode(childNode, ref models, ref types);
                    }
                }
                else
                {
                    Log.Error("model is null");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"{Project.Current.BrowseName}: {ex.Message}");
            }
            if (models is { })
            {
                GatewayModels.AddOrUpdateNode(models.Name, models);
            }
            return new Node[] { models, types };
        }

        private void AddNode(IUANode node, ref Node parent, ref Node types)
        {
            Node child = new()
            {
                Name = node.BrowseName,
                FullyQualifiedName = $"{parent.FullyQualifiedName}.{node.BrowseName}",
                OriginalName = node.BrowseName,
                ValType = (int)ValueTypes.Node,
                Parent = new Parent { Id = parent.Id, Name = parent.Name, FullyQualifiedName = parent.FullyQualifiedName },
                Type = MimeTypeExtensions.FTEG_NODE_TYPE,
                Id = node.NodeId.ToString(),
                Tag = null,
                Path = $"{parent.Path}.{node.BrowseName}",
                UpdateRate = 1000,
                UpdateType = UpdateType.OnChange,
                ReadOnly = false,
                DataCollection = true
            };

            var typeName = ((UAObject)node).ObjectType.BrowseName;

            switch (typeName)
            {
                case "FolderType":
                case "EventHandler":
                case "InputArguments":
                case "OutputArguments":
                case "MethodInvocation":
                    {
                        break;
                    }
                default:
                    {
                        child.Properties.Add(new Property { Bin = "infoAttributes", Name = "TypeName", Value = typeName, Type = "STRING" });

                        if (types.Children.FindIndex(x => x.Name == typeName) == -1)
                        {
                            var typeDefineintion = ((UAObject)node).ObjectType;

                            Node type = new()
                            {
                                Name = typeName,
                                FullyQualifiedName = $"{parent.FullyQualifiedName}.{typeName}",
                                OriginalName = typeName,
                                ValType = (int)ValueTypes.Node,
                                Parent = new Parent { Id = parent.Id, Name = parent.Name, FullyQualifiedName = parent.FullyQualifiedName },
                                Type = MimeTypeExtensions.FTEG_NODE_TYPE,
                                Id = typeDefineintion.NodeId.ToString(),
                                Tag = null,
                                Path = $"{parent.Path}.{typeName}",
                                UpdateRate = 1000,
                                UpdateType = UpdateType.OnChange,
                                ReadOnly = false,
                                DataCollection = true
                            };

                            foreach (var variable in GetChildVariables(typeDefineintion))
                            {
                                AddTag(variable, ref type);
                            }

                            foreach (var childNode in GetChildNodes(typeDefineintion))
                            {
                                AddNode(childNode, ref type, ref types);
                            }

                            types.Children.Add(type);
                            types.ChildCount = types.Children.Count;
                        }

                        break;
                    }
            }

            foreach (var variable in GetChildVariables(node))
            {
                if (_monitorValues)
                {
                    if (EA_Runtime.Instance._synchronizer is { })
                    {
                        EA_Runtime.Instance._synchronizer.Add(variable);
                       // variable.VariableChange += EA_Runtime.Instance.Variable_VariableChange;
                    }
                }
                AddTag(variable, ref child);
            }

            foreach (var childNode in GetChildNodes(node))
            {
                AddNode(childNode, ref child, ref types);
            }

            parent.Children.Add(child);
        }

        private static readonly System.Type TAG_TYPE = typeof(FTOptix.CommunicationDriver.Tag);
        private static readonly System.Type VAR_TYPE = typeof(IUAVariable);

        private static IEnumerable<IUAVariable> GetChildVariables(IUANode node)
        {
            return node.GetNodesByType<IUAVariable>();
        }

        private static IEnumerable<IUANode> GetChildNodes(IUANode node)
        {
            return node.GetNodesByType<IUAObject>().Where(n => !n.GetType().IsAssignableFrom(TAG_TYPE) && !n.GetType().IsAssignableFrom(VAR_TYPE));
        }

        private static void AddTag(IUAVariable variable, ref Node parent)
        {
            switch (parent.Name)
            {
                case "InputArguments":
                case "EventArguments":
                    {
                        break;
                    }
                default:
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
                            Id = variable.NodeId.ToString(),
                            Tag = null,
                            Path = $"{parent.Path}.{variable.BrowseName}",
                            UpdateRate = 1000,
                            UpdateType = UpdateType.OnChange,
                            ReadOnly = false,
                            DataCollection = true
                        };

                        FTOptixConcerto.Instance.AddNodeId(child.FullyQualifiedName, variable.NodeId);

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

                        parent.Children.Add(child);
                        break;
                    }
            }
        }

        private bool _monitorValues = true;
    }
}
