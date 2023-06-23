using Cca.Cgp.Common.Model;
using Cca.Cgp.Common.Model.Interfaces;
using Cca.Extensions.Common;
using Cca.Extensions.Common.DateTimeUtil;
using FTOptix.Core;
using FTOptix.HMIProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UAManagedCore;
using DataTypes = UAManagedCore.OpcUa.DataTypes;

namespace NetZero.Internal
{
    public static class OptixHelpers
    {
        public const string CcaPlugAndProduceGuid = "{1038E98B-9835-4025-9EEA-1BA8F91261DD}";

        public static readonly UAObjectType BASE_OBJECT_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType");
        public static readonly UAObjectType FINITE_STATE_MACHINE_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType/StateMachineType/FiniteStateMachineType");
        public static readonly UAObjectType FINITE_STATE_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType/StateType");
        public static readonly UAObjectType FINITE_TRANSITION_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType/TransitionType");

        private static uint _lastId = 100;
        private static readonly Guid _methodFactoryGuid;
        private static readonly int _namespaceIndex = -1;

        private static readonly List<NodeId> _registeredMethodHandlers = new();

        static OptixHelpers()
        {
            _namespaceIndex = Context.AssignNamespaceIndex("http://opcFoundation.org/UA/SmartObjects",
                                                           namespaceType: NamespaceType.Static);
            _methodFactoryGuid = new Guid(CcaPlugAndProduceGuid);
        }

        public static IContext Context => Project.Current.Context;

        //public static IUAVariable CreateAnalogVariable(string name, string displayName = "")
        //{
        //    return CreateScalarVariable(name, UAManagedCore.OpcUa.VariableTypes.AnalogItemType, displayName);
        //}

        public static void AddIfNotExist(this IUANode uaObject, IUANode property, string propertyName)
        {
            if (property is not { })
            {
                return;
            }
            var srcProp = uaObject.Get(propertyName);
            if (srcProp is not { })
            {
                uaObject.Add(property);
            }
        }

        public static void AddReference(this IUANode node, NodeId referenceTypeNodeId, IUANode targetNode)
        {
            node.Refs.AddReference(referenceTypeNodeId, targetNode);
        }

        public static void AddReference(this IUANode node, NodeId referenceTypeNodeId, IUAObject targetNode)
        {
            node.Refs.AddReference(referenceTypeNodeId, targetNode);
        }

        public static void AddReference(this IUANode node, NodeId referenceTypeNodeId, NodeId targetNodeId)
        {
            node.Refs.AddReference(referenceTypeNodeId, targetNodeId);
        }

        public static void AddReference(this IUANode node, NodeId referenceTypeNodeId, IUAMethod targetMethod)
        {
            node.Refs.AddReference(referenceTypeNodeId, targetMethod);
        }

        public static Struct CreateArgumentsStructScalar(string name, NodeId dataType, string description = "")
        {
            return dataType switch
            {
                _ when dataType == DataTypes.Boolean => new Struct(DataTypes.Argument,
                                                                   name,
                                                                   dataType,
                                                                   ValueRank.OneDimension,
                                                                   Array.Empty<bool>(),
                                                                   description.ToLocalizedText()),
                _ when dataType == DataTypes.Byte => new Struct(DataTypes.Argument,
                                                                name,
                                                                dataType,
                                                                ValueRank.OneDimension,
                                                                Array.Empty<byte>(),
                                                                description.ToLocalizedText()),
                _ when dataType == DataTypes.DateTime => new Struct(DataTypes.Argument,
                                                                    name,
                                                                    dataType,
                                                                    ValueRank.OneDimension,
                                                                    Array.Empty<DateTime>(),
                                                                    description.ToLocalizedText()),
                _ when dataType == DataTypes.Double => new Struct(DataTypes.Argument,
                                                                  name,
                                                                  dataType,
                                                                  ValueRank.OneDimension,
                                                                  Array.Empty<double>(),
                                                                  description.ToLocalizedText()),
                _ when dataType == DataTypes.Float => new Struct(DataTypes.Argument,
                                                                 name,
                                                                 dataType,
                                                                 ValueRank.OneDimension,
                                                                 Array.Empty<float>(),
                                                                 description.ToLocalizedText()),
                _ when dataType == DataTypes.Int16 => new Struct(DataTypes.Argument,
                                                                 name,
                                                                 dataType,
                                                                 ValueRank.OneDimension,
                                                                 Array.Empty<short>(),
                                                                 description.ToLocalizedText()),
                _ when dataType == DataTypes.Int32 => new Struct(DataTypes.Argument,
                                                                 name,
                                                                 dataType,
                                                                 ValueRank.OneDimension,
                                                                 Array.Empty<int>(),
                                                                 description.ToLocalizedText()),
                _ when dataType == DataTypes.Int64 => new Struct(DataTypes.Argument,
                                                                 name,
                                                                 dataType,
                                                                 ValueRank.OneDimension,
                                                                 Array.Empty<long>(),
                                                                 description.ToLocalizedText()),
                _ when dataType == DataTypes.UInt16 => new Struct(DataTypes.Argument,
                                                                  name,
                                                                  dataType,
                                                                  ValueRank.OneDimension,
                                                                  Array.Empty<ushort>(),
                                                                  description.ToLocalizedText()),
                _ when dataType == DataTypes.UInt32 => new Struct(DataTypes.Argument,
                                                                  name,
                                                                  dataType,
                                                                  ValueRank.OneDimension,
                                                                  Array.Empty<uint>(),
                                                                  description.ToLocalizedText()),
                _ when dataType == DataTypes.UInt64 => new Struct(DataTypes.Argument,
                                                                  name,
                                                                  dataType,
                                                                  ValueRank.OneDimension,
                                                                  Array.Empty<ulong>(),
                                                                  description.ToLocalizedText()),
                _ when dataType == DataTypes.Number => new Struct(DataTypes.Argument,
                                                                  name,
                                                                  dataType,
                                                                  ValueRank.OneDimension,
                                                                  Array.Empty<double>(),
                                                                  description.ToLocalizedText()),
                _ when dataType == DataTypes.String => new Struct(DataTypes.Argument,
                                                                  name,
                                                                  dataType,
                                                                  ValueRank.OneDimension,
                                                                  Array.Empty<string>(),
                                                                  description.ToLocalizedText()),
                _ => new Struct(DataTypes.Argument,
                                name,
                                dataType,
                                ValueRank.OneDimension,
                                Array.Empty<string>(),
                                description.ToLocalizedText()),
            };
        }

        public static IUAVariable CreateBooleanVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, DataTypes.Boolean, UAManagedCore.OpcUa.VariableTypes.BaseDataVariableType, displayName);
        }

        public static IUAVariable CreateDataVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, DataTypes.Number, UAManagedCore.OpcUa.VariableTypes.DataItemType, displayName);
        }

        public static IUAVariable CreateDoubleVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, DataTypes.Double, UAManagedCore.OpcUa.VariableTypes.BaseDataVariableType, displayName);
        }

        public static IUAVariable CreateFiniteStateVariable(IUANode parent, string name, string displayName, IUAObject stateNode)
        {
            var variable = InformationModel.MakeVariable(name, UAManagedCore.OpcUa.DataTypes.LocalizedText, UAManagedCore.OpcUa.VariableTypes.FiniteStateVariableType);
            variable.DisplayName = string.IsNullOrEmpty(displayName) ? name.ToLocalizedText() : displayName.ToLocalizedText();
            variable.Value = new UAValue(stateNode.BrowseName.ToLocalizedText());
            var idVar = variable.Get<IUAVariable>("Id");
            variable.Remove(idVar);
            idVar = InformationModel.MakeVariable("Id", UAManagedCore.OpcUa.DataTypes.NodeId, UAManagedCore.OpcUa.VariableTypes.PropertyType);
            idVar.Value = new UAValue(stateNode.NodeId);
            variable.Add(idVar);
            var nameProperty = InformationModel.MakeVariable("Name", UAManagedCore.OpcUa.DataTypes.String, UAManagedCore.OpcUa.VariableTypes.PropertyType);
            nameProperty.Value = new UAValue(stateNode.BrowseName);
            variable.Add(nameProperty);
            var numberProperty = InformationModel.MakeVariable("Number", UAManagedCore.OpcUa.DataTypes.Int32, UAManagedCore.OpcUa.VariableTypes.PropertyType);
            numberProperty.Value = new UAValue(stateNode.Get<IUAVariable>("StateNumber").Value.Value.ToInt());
            variable.Add(numberProperty);
            return variable;
        }

        public static IUAVariable CreateFiniteTransitionVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, UAManagedCore.OpcUa.VariableTypes.FiniteTransitionVariableType, UAManagedCore.OpcUa.VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateFloatVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, DataTypes.Float, UAManagedCore.OpcUa.VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateIntVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, DataTypes.Int32, UAManagedCore.OpcUa.VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateLongVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, DataTypes.Int64, UAManagedCore.OpcUa.VariableTypes.BaseVariableType, displayName);
        }

        public static IUAObject CreateObject(IUANode parent, string name, NodeId objectTypeId)
        {
            {
                var newObject = Context.NodeFactory.MakeObject(
                    new NodeId(parent.NodeId.NamespaceIndex, Common.CreateGuidFromModelElement(parent.NodeId.Id.ToString(), name)),
                    name,
                    objectTypeId);

                parent.Add(newObject);

                return newObject;
            }
        }

        public static IUAObjectType CreateObjectType(string name, IUANode baseObjectType)
        {
            NodeId baseObjectTypeNodeId = baseObjectType is not { } ? Common.BASE_OBJECT_TYPE.NodeId : baseObjectType.NodeId;
            return CreateObjectType(name, baseObjectTypeNodeId);
        }

        public static IUAObjectType CreateObjectType(string name, NodeId baseNodeId = null)
        {
            var objectType = GetObjectTypeByName(name);
            if (objectType is not { })
            {
                objectType = InformationModel.MakeObjectType(name, baseNodeId ?? Common.BASE_OBJECT_TYPE.NodeId);
            }
            return objectType;
        }

        public static IUAObjectType CreateObjectType(string name, NodeId baseNodeId, IEnumerable<IUAVariable> variables)
        {
            var objectType = CreateObjectType(name, baseNodeId);
            var superType = InformationModel.Get(baseNodeId);
            foreach (var variable in variables)
            {
                if (!superType.HasChildNamed(variable.BrowseName))
                {
                    objectType.Add(variable);
                }
                else
                {
                    objectType.GetVariable(variable.BrowseName).Value = variable.Value;
                }
            }

            return objectType;
        }

        public static IUAVariable CreateScalarVariable(IUANode parent, string browseName, NodeId dataTypeId, NodeId variableTypeId, string displayName = "", int length = 0)
        {
            IUAVariable variable = null;
            try
            {
                if (length == 0)
                {
                    variable = Context.NodeFactory.MakeVariable(
                        new NodeId(parent.NodeId.NamespaceIndex, Common.CreateGuidFromModelElement(parent.NodeId.Id.ToString(), browseName)),
                        browseName,
                        dataTypeId,
                        variableTypeId);
                }
                else
                {
                    ValueRank valueRank = ValueRank.OneDimension;
                    var arrayDimensions = new uint[1];
                    arrayDimensions[0] = (uint)length;
                    variable = Context.NodeFactory.MakeVariable(
                        new NodeId(parent.NodeId.NamespaceIndex, Common.CreateGuidFromModelElement(parent.NodeId.Id.ToString(), browseName)),
                        browseName,
                        dataTypeId,
                        valueRank,
                        arrayDimensions,
                        variableTypeId);
                }

                parent.Add(variable);
            }
            catch (Exception ex)
            {
                Log.Error(nameof(OptixHelpers), $"{nameof(CreateScalarVariable)}: browseName: {browseName}: {ex.Message}");
            }

            return variable;
        }

        public static IUAObjectType CreateStateType(string name)
        {
            var stateType = (IUAObjectType)UaObjectTypesFolder().Find(name);
            if (stateType is not { })
            {
                stateType = CreateObjectType(name, FINITE_STATE_TYPE);
            }
            return stateType;
        }

        public static IUAVariable CreateStateVariable(string name, int stateNumber, string displayName = "")
        {
            var variable = InformationModel.MakeVariable(name, DataTypes.LocalizedText, UAManagedCore.OpcUa.VariableTypes.FiniteStateVariableType);
            //var id = variable.GetVariable("Id");
            //id.Value = new UAValue(stateNumber);
            return variable;
        }

        public static IUAVariable CreateStringVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, DataTypes.String, UAManagedCore.OpcUa.VariableTypes.BaseDataVariableType, displayName);
        }

        public static IUAObjectType CreateSubType(string name, IUAObjectType superType)
        {
            var objectType = GetObjectTypeByName(name);

            if (objectType is not { })
            {
                if (superType is not { })
                {
                    superType = Common.BASE_OBJECT_TYPE;
                }
                objectType = CreateObjectType(name, superType);
            }
            return objectType;
        }

        public static IUAObjectType CreateTransitionType(string name)
        {
            var transitionType = (IUAObjectType)UaObjectTypesFolder().Find(name);
            if (transitionType is not { })
            {
                transitionType = CreateObjectType(name, FINITE_TRANSITION_TYPE);
            }
            return transitionType;
        }

        public static IUAVariable CreateTransitionVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, DataTypes.LocalizedText, UAManagedCore.OpcUa.VariableTypes.FiniteTransitionVariableType, displayName);
        }

        public static IUAVariable CreateTwoStateVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, UAManagedCore.OpcUa.VariableTypes.TwoStateVariableType, UAManagedCore.OpcUa.VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateUIntVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, DataTypes.UInt32, UAManagedCore.OpcUa.VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateULongVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, DataTypes.UInt64, UAManagedCore.OpcUa.VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateUShortVariable(IUANode parent, string name, string displayName = "")
        {
            return CreateScalarVariable(parent, name, DataTypes.UInt16, UAManagedCore.OpcUa.VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateVariable(string name, Type dataType)
        {
            var opcUaType = dataType.ToOpcUaDataType();
            return InformationModel.MakeVariable(name, opcUaType);
        }

        public static void DeleteTypeByName(string name)
        {
            var oldType = OptixHelpers.GetObjectTypeByName(name);
            if (oldType is { })
            {
                oldType.Delete();
            }
        }

        public static NodeId GetDataTypeId(this Type dataType)
        {
            return dataType switch
            {
                _ when dataType == typeof(int) => DataTypes.Int32,
                _ when dataType == typeof(float) => DataTypes.Float,
                _ when dataType == typeof(string) => DataTypes.String,
                _ when dataType == typeof(double) => DataTypes.Double,
                _ when dataType == typeof(bool) => DataTypes.Boolean,
                _ when dataType == typeof(long) => DataTypes.Int64,
                _ when dataType == typeof(short) => DataTypes.Int16,
                _ => DataTypes.Number
            };
        }

        public static NodeId GetDataTypeId(this ValueTypes dataType)
        {
            return dataType switch
            {
                ValueTypes.Int32 => DataTypes.Int32,
                ValueTypes.Float => DataTypes.Float,
                ValueTypes.String => DataTypes.String,
                ValueTypes.Double => DataTypes.Double,
                ValueTypes.Boolean => DataTypes.Boolean,
                ValueTypes.Int64 => DataTypes.Int64,
                ValueTypes.Int16 => DataTypes.Int16,
                _ => DataTypes.Number
            };
        }

        public static Type GetDotNetType(IUANode node)
        {
            return node.NodeId.GetDotNetType();
        }

        public static Type GetDotNetType(this NodeId nodeId)
        {
            return Context.NodeFactory.GetNetTypeFromNodeId(nodeId);
        }

        /// <summary>
        /// Given a path to a project folder, obtains one list of objects of type T contained in the
        /// same folder (and relative suubfolders, recursively)
        /// </summary>
        public static ICollection<IUANode> GetNodesIntoFolder<T>(IUANode iuaNode)
        {
            var objectsInFolder = new List<IUANode>();
            foreach (var o in iuaNode.Children)
            {
                switch (o)
                {
                    case T _:
                        objectsInFolder.Add(o);
                        break;

                    case Folder _:
                    case UAObject _:
                        objectsInFolder.AddRange(GetNodesIntoFolder<T>(o));
                        break;

                    default:
                        break;
                }
            }
            return objectsInFolder;
        }

        public static IUAObjectType GetObjectTypeByName(string name)
        {
            return (IUAObjectType)UaObjectTypesFolder().FindObject(name);
        }

        public static IUAObjectType GetObjectTypeFromPath(string path)
        {
            var node = UaObjectTypesFolder().Get(path);
            if (node is { })
            {
                return node as IUAObjectType;
            }
            return null;
        }

        public static NodeId GetOpcUaDataType(this IUAVariable variable)
        {
            if (variable is not { })
            {
                return UAManagedCore.OpcUa.DataTypes.String;
            }
            var type = variable.GetSystemType();
            return type.ToOpcUaDataType();
        }

        public static IUAObjectType GetTypeFromBrowsePath(string path)
        {
            return (IUAObjectType)UaObjectTypesFolder().Get(path);
        }

        public static UAValue GetUAValue(object value, string type)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                var stringVal = value.ToString();
                if (string.IsNullOrEmpty(stringVal))
                {
                    return null;
                }
                switch (type)
                {
                    case ("Boolean"):
                        return new UAValue(Int32.Parse(GetBoolean(value)));

                    case ("Byte"):
                        return new UAValue(Byte.Parse(stringVal));

                    case ("SByte"):
                        return new UAValue(SByte.Parse(stringVal));

                    case ("Int16"):
                        return new UAValue(Int16.Parse(stringVal));

                    case ("Int32"):
                        return new UAValue(Int32.Parse(stringVal));

                    case ("Int64"):
                        return new UAValue(Int64.Parse(stringVal));

                    case ("UInt16"):
                        return new UAValue(UInt16.Parse(stringVal));

                    case ("UInt32"):
                        return new UAValue(UInt32.Parse(stringVal));

                    case ("UInt64"):
                        return new UAValue(UInt64.Parse(stringVal));

                    case ("Float"):
                        return new UAValue((float)((double)value));

                    case ("Double"):
                        return new UAValue((double)value);

                    case ("String"):
                        return new UAValue(stringVal);

                    case ("ByteString"):
                        return new UAValue((ByteString)value);

                    case ("UtcTime"):
                    case ("DateTime"):
                        {
                            var timestamp = GetTimestamp(value);
                            if (timestamp is { })
                            {
                                return new UAValue(GetTimestamp(value));
                            }
                            break;
                        }

                    default:
                        break;
                }
                //NodeId valueType = variableToLog.ActualDataType;
                //if (valueType == OpcUa.DataTypes.Boolean)
                //    return new UAValue(Int32.Parse(GetBoolean(value)));
                //else if (valueType == OpcUa.DataTypes.Integer)
                //    return new UAValue(Int64.Parse(value.ToString()));
                //else if (valueType == OpcUa.DataTypes.UInteger)
                //    return new UAValue(UInt64.Parse(value.ToString()));
                //else if (valueType == OpcUa.DataTypes.Byte)
                //    return new UAValue(Byte.Parse(value.ToString()));
                //else if (valueType == OpcUa.DataTypes.SByte)
                //    return new UAValue(SByte.Parse(value.ToString()));
                //else if (valueType == OpcUa.DataTypes.Int16)
                //    return new UAValue(Int16.Parse(value.ToString()));
                //else if (valueType == OpcUa.DataTypes.UInt16)
                //    return new UAValue(UInt16.Parse(value.ToString()));
                //else if (valueType == OpcUa.DataTypes.Int32)
                //    return new UAValue(Int32.Parse(value.ToString()));
                //else if (valueType == OpcUa.DataTypes.UInt32)
                //    return new UAValue(UInt32.Parse(value.ToString()));
                //else if (valueType == OpcUa.DataTypes.Int64)
                //    return new UAValue(Int64.Parse(value.ToString()));
                //else if (valueType == OpcUa.DataTypes.UInt64)
                //    return new UAValue(UInt64.Parse(value.ToString()));
                //else if (valueType == OpcUa.DataTypes.Float)
                //    return new UAValue((float)((double)value));
                //else if (valueType == OpcUa.DataTypes.Double)
                //    return new UAValue((double)value);
                //else if (valueType == OpcUa.DataTypes.DateTime)
                //    return new UAValue(GetTimestamp(value));
                //else if (valueType == OpcUa.DataTypes.String)
                //    return new UAValue(value.ToString());
                //else if (valueType == OpcUa.DataTypes.ByteString)
                //    return new UAValue((ByteString)value);
                //else if (valueType == OpcUa.DataTypes.NodeId)
                //    return new UAValue((NodeId)value);
            }
            catch (Exception e)
            {
                Log.Info(nameof(OptixHelpers), $"GetUAValue: Parse Exception: {e.Message}");
                throw;
            }

            return null;
        }

        public static bool HasChildNamed(this IUANode node, string name)
        {
            return node.Children.Any(c => c.BrowseName.Equals(name));
        }

        public static bool IsRuntime()
        {
            var isRuntime = Cca.Extensions.Common.Util.OperatingSystem.BaseProcessName.Contains("Runtime");
            Log.Info(nameof(OptixHelpers), $"IsRuntime : {isRuntime}");
            return isRuntime;
        }

        public static IUAReferenceType MakeReferenceType(NodeId nodeId, QualifiedName browseName, NodeId superTypeNodeId)
        {
            return Context.NodeFactory.MakeReferenceType(nodeId, browseName, superTypeNodeId);
        }

        public static void RegisterMethod(this IUANode objectType, IUAMethod method)
        {
            if (objectType is not { })
            {
                Log.Error(nameof(OptixHelpers), $"{nameof(RegisterMethod)}: {nameof(objectType)} is null");
                return;
            }

            if (method is not { })
            {
                Log.Error(nameof(OptixHelpers), $"{nameof(RegisterMethod)}: {nameof(method)} is null");
                return;
            }

            Log.Info(nameof(OptixHelpers), $"{nameof(RegisterMethod)}: type: {objectType.BrowseName}  registering {method.DisplayName}");
            if (!objectType.HasChildren() || !objectType.Children.Any(c => c.BrowseName.Equals(method.BrowseName)))
            {
                objectType.Add(method);
            }

            // only register once
            try
            {
                if (!_registeredMethodHandlers.Contains(objectType.NodeId))
                {
                    Context.NodeFactory.RegisterBehaviourFactoryReference(objectType.NodeId, _methodFactoryGuid, (byte)BehaviourModulePriorities.Default);
                    _registeredMethodHandlers.Add(objectType.NodeId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(nameof(OptixHelpers), $"{nameof(RegisterMethod)}: type: {objectType.BrowseName}  registering {method.DisplayName}: {ex.Message}");
            }
        }

        public static void RegisterMethods(this IUANode objectType, IEnumerable<IUAMethod> methods)
        {
            foreach (var method in methods)
            {
                objectType.RegisterMethod(method);
            }
        }

        public static void SetObjectProperty(this IUANode uaObject, IUANode property, string propertyName)
        {
            if (uaObject is not { })
            {
                Log.Error(nameof(OptixHelpers), $"{nameof(SetObjectProperty)}: {nameof(uaObject)} parameter is null");
                return;
            }
            if (property is not { })
            {
                Log.Error(nameof(OptixHelpers), $"{nameof(SetObjectProperty)}: {nameof(property)} parameter is null");
                return;
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                Log.Error(nameof(OptixHelpers), $"{nameof(SetObjectProperty)}: {nameof(propertyName)} parameter is null or empty");
                return;
            }

            var srcProp = uaObject.Get(propertyName);
            if (srcProp is not { })
            {
                uaObject.Add(property);
            }
            else
            {
                if (uaObject.TryRemove(srcProp))
                {
                    uaObject.Add(property);
                }
            }
        }

        public static void SetObjectProperty(this IUANode uaObject, IUANode property)
        {
            if (uaObject is not { })
            {
                Log.Error(nameof(OptixHelpers), $"{nameof(SetObjectProperty)}: {nameof(uaObject)} parameter is null");
                return;
            }
            if (property is not { })
            {
                Log.Error(nameof(OptixHelpers), $"{nameof(SetObjectProperty)}: {nameof(property)} parameter is null");
                return;
            }

            var propertyName = property.BrowseName;
            uaObject.SetObjectProperty(property, propertyName);
        }

        public static LocalizedText ToLocalizedText(this string text, string localeId = "en-us")
        {
            if (!string.IsNullOrEmpty(text))
            {
                return new LocalizedText(text, localeId);
            }
            else
            {
                Log.Error(nameof(OptixHelpers), $"{nameof(ToLocalizedText)}: {nameof(text)} parameter is null or empty");
            }
            return null;
        }

        public static NodeId ToOpcUaDataType(this Type type)
        {
            return type switch
            {
                _ when type == typeof(int) => UAManagedCore.OpcUa.DataTypes.Int32,
                _ when type == typeof(float) => UAManagedCore.OpcUa.DataTypes.Float,
                _ when type == typeof(double) => UAManagedCore.OpcUa.DataTypes.Double,
                _ when type == typeof(bool) => UAManagedCore.OpcUa.DataTypes.Boolean,
                _ when type == typeof(short) => UAManagedCore.OpcUa.DataTypes.Int16,
                _ when type == typeof(long) => UAManagedCore.OpcUa.DataTypes.Int64,
                _ when type == typeof(DateTime) => UAManagedCore.OpcUa.DataTypes.DateTime,
                _ when type == typeof(byte) => UAManagedCore.OpcUa.DataTypes.Byte,
                _ when type == typeof(Guid) => UAManagedCore.OpcUa.DataTypes.Guid,
                _ when type == typeof(LocalizedText) => UAManagedCore.OpcUa.DataTypes.LocalizedText,
                _ when type == typeof(NodeId) => UAManagedCore.OpcUa.DataTypes.NodeId,
                _ when type == typeof(NodeClass) => UAManagedCore.OpcUa.DataTypes.NodeClass,
                _ when type == typeof(QualifiedName) => UAManagedCore.OpcUa.DataTypes.QualifiedName,
                _ when type == typeof(UInt16) => UAManagedCore.OpcUa.DataTypes.UInt16,
                _ when type == typeof(UInt32) => UAManagedCore.OpcUa.DataTypes.UInt32,
                _ when type == typeof(UInt64) => UAManagedCore.OpcUa.DataTypes.UInt64,
                _ => UAManagedCore.OpcUa.DataTypes.String
            };
        }

        public static QualifiedName ToQualifiedName(this string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                return new QualifiedName(_namespaceIndex, name);
            }
            return null;
        }

        public static bool TryRemove(this IUANode node, IUANode property)
        {
            try
            {
                node.Remove(property);
                return true;
            }
            catch
            {
            }
            return false;
        }

        public static IUAObject UaDataTypesFolder()
        {
            return Context.GetObject(UAManagedCore.OpcUa.Objects.DataTypesFolder);
        }

        public static IUAObject UaEventTypesFolder()
        {
            return Context.GetObject(UAManagedCore.OpcUa.Objects.EventTypesFolder);
        }

        public static IUAObject UaObjectsFolder()
        {
            return Context.GetObject(UAManagedCore.OpcUa.Objects.ObjectsFolder);
        }

        public static IUAObject UaObjectTypesFolder()
        {
            return Context.GetObject(UAManagedCore.OpcUa.Objects.ObjectTypesFolder);
        }

        public static IUAObject UaReferenceTypesFolder()
        {
            return Context.GetObject(UAManagedCore.OpcUa.Objects.ReferenceTypesFolder);
        }

        public static IUAObject UaRootFolder()
        {
            return Context.GetObject(UAManagedCore.OpcUa.Objects.RootFolder);
        }

        public static IUAObject UaTypesFolder()
        {
            return Context.GetObject(UAManagedCore.OpcUa.Objects.TypesFolder);
        }

        public static IUAObject UaVariableTypesFolder()
        {
            return Context.GetObject(UAManagedCore.OpcUa.Objects.VariableTypesFolder);
        }

        public static IUAObject UaViewsFolder()
        {
            return Context.GetObject(UAManagedCore.OpcUa.Objects.ViewsFolder);
        }

        private static string GetBoolean(object value)
        {
            return value.ToBool() ? "1" : "0";

            //var valueString = value.ToString();
            //if (valueString == "0" || valueString == "1")
            //    return valueString;

            //if (valueString.Equals("false", StringComparison.OrdinalIgnoreCase) || valueString.Equals("f", StringComparison.OrdinalIgnoreCase))
            //    return "0";
            //else
            //    return "1";
        }

        private static DateTime GetTimestamp(object value)
        {
            if (Type.GetTypeCode(value.GetType()) == TypeCode.DateTime)
            {
                return (DateTime)value;
            }
            else
            {
                var date = value.ToDateTime();
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);
            }
        }

        public struct EnumStruct
        {
            public string Description;
            public string Name;
            public int Ordinal;
        }
    }
}