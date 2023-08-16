using Cca.Cgp.Common.Model;
using Cca.Extensions.Common;
using Cca.Extensions.Common.DateTimeUtil;
using FTOptix.Core;
using FTOptix.HMIProject;
using System;
using System.Collections.Generic;
using System.Linq;
using UAManagedCore;

namespace NetZero.Extensions
{
    using DataTypes = UAManagedCore.OpcUa.DataTypes;
    using VariableTypes = UAManagedCore.OpcUa.VariableTypes;

    public static class OptixHelpers
    {
        #region Public Fields

        public const string CcaPlugAndProduceGuid = "{1038E98B-9835-4025-9EEA-1BA8F91261DD}";

        public static readonly UAObjectType BASE_OBJECT_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType");
        public static readonly UAObjectType FINITE_STATE_MACHINE_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType/StateMachineType/FiniteStateMachineType");
        public static readonly UAObjectType FINITE_STATE_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType/StateType");
        public static readonly UAObjectType FINITE_TRANSITION_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType/TransitionType");

        #endregion Public Fields

        #region Private Fields

        private static uint _lastId = 100;
        private static readonly Guid _methodFactoryGuid;
        private static readonly int _namespaceIndex = -1;

        private static readonly List<NodeId> _registeredMethodHandlers = new();

        #endregion Private Fields

        #region Public Properties

        public static IContext Context => Project.Current.Context;

        #endregion Public Properties

        #region Public Methods

        public static IUAVariable CreateAnalogDoubleVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.Double, VariableTypes.AnalogItemType, displayName);
        }

        public static Struct CreateArgumentsStructScalar(string name, NodeId dataType, string description = "")
        {
            return dataType switch
            {
                _ when dataType == DataTypes.Boolean => new Struct(DataTypes.Argument,
                                                                   name,
                                                                   dataType,
                                                                   ValueRank.Scalar,
                                                                   Array.Empty<bool>(),
                                                                   description.ToLocalizedText()),
                _ when dataType == DataTypes.Byte => new Struct(DataTypes.Argument,
                                                                name,
                                                                dataType,
                                                                ValueRank.Scalar,
                                                                Array.Empty<byte>(),
                                                                description.ToLocalizedText()),
                _ when dataType == DataTypes.DateTime => new Struct(DataTypes.Argument,
                                                                    name,
                                                                    dataType,
                                                                    ValueRank.Scalar,
                                                                    Array.Empty<DateTime>(),
                                                                    description.ToLocalizedText()),
                _ when dataType == DataTypes.Double => new Struct(DataTypes.Argument,
                                                                  name,
                                                                  dataType,
                                                                  ValueRank.Scalar,
                                                                  Array.Empty<double>(),
                                                                  description.ToLocalizedText()),
                _ when dataType == DataTypes.Float => new Struct(DataTypes.Argument,
                                                                 name,
                                                                 dataType,
                                                                 ValueRank.Scalar,
                                                                 Array.Empty<float>(),
                                                                 description.ToLocalizedText()),
                _ when dataType == DataTypes.Int16 => new Struct(DataTypes.Argument,
                                                                 name,
                                                                 dataType,
                                                                 ValueRank.Scalar,
                                                                 Array.Empty<short>(),
                                                                 description.ToLocalizedText()),
                _ when dataType == DataTypes.Int32 => new Struct(DataTypes.Argument,
                                                                 name,
                                                                 dataType,
                                                                 ValueRank.Scalar,
                                                                 Array.Empty<int>(),
                                                                 description.ToLocalizedText()),
                _ when dataType == DataTypes.Int64 => new Struct(DataTypes.Argument,
                                                                 name,
                                                                 dataType,
                                                                 ValueRank.Scalar,
                                                                 Array.Empty<long>(),
                                                                 description.ToLocalizedText()),
                _ when dataType == DataTypes.UInt16 => new Struct(DataTypes.Argument,
                                                                  name,
                                                                  dataType,
                                                                  ValueRank.Scalar,
                                                                  Array.Empty<ushort>(),
                                                                  description.ToLocalizedText()),
                _ when dataType == DataTypes.UInt32 => new Struct(DataTypes.Argument,
                                                                  name,
                                                                  dataType,
                                                                  ValueRank.Scalar,
                                                                  Array.Empty<uint>(),
                                                                  description.ToLocalizedText()),
                _ when dataType == DataTypes.UInt64 => new Struct(DataTypes.Argument,
                                                                  name,
                                                                  dataType,
                                                                  ValueRank.Scalar,
                                                                  Array.Empty<ulong>(),
                                                                  description.ToLocalizedText()),
                _ when dataType == DataTypes.Number => new Struct(DataTypes.Argument,
                                                                  name,
                                                                  dataType,
                                                                  ValueRank.Scalar,
                                                                  Array.Empty<double>(),
                                                                  description.ToLocalizedText()),
                _ when dataType == DataTypes.String => new Struct(DataTypes.Argument,
                                                                  name,
                                                                  dataType,
                                                                  ValueRank.Scalar,
                                                                  Array.Empty<string>(),
                                                                  description.ToLocalizedText()),
                _ => new Struct(DataTypes.Argument,
                                name,
                                dataType,
                                ValueRank.Scalar,
                                Array.Empty<string>(),
                                description.ToLocalizedText()),
            };
        }

        public static IUAVariable CreateArgumentVariable(string name, IEnumerable<Struct> arguments, string displayName = "")
        {
            uint argumentCount = (uint)(arguments is { } ? arguments.Count() : 0);

            var variable = Context.NodeFactory.MakeVariable(GetNextNodeId(),
                                                            name.ToQualifiedName(),
                                                            DataTypes.Argument,
                                                            ValueRank.OneDimension,
                                                            new uint[] { argumentCount },
                                                            UAManagedCore.OpcUa.VariableTypes.PropertyType,
                                                            true,
                                                            null,
                                                            0);
            variable.DisplayName = displayName.ToLocalizedText();

            if (arguments is { })
            {
                variable.SetValue(arguments.ToArray());
            }
            return variable;
        }

        public static IUAVariable CreateArrayVariable(string name, NodeId dataType, NodeId baseVariableType, ValueRank valueRank = ValueRank.OneDimension, string displayName = "")
        {
            var variable = Context.NodeFactory
                .MakeVariable(GetNextNodeId(),
                              name.ToQualifiedName(),
                              dataType,
                              valueRank,
                              new uint[] { 0 },
                              baseVariableType,
                              true,
                              null,
                              0);
            variable.DisplayName = displayName.ToLocalizedText();
            return variable;
        }

        public static IUAVariable CreateBaseDataVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.Number, VariableTypes.BaseDataVariableType, displayName);
        }

        public static IUAVariable CreateBooleanVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.Boolean, VariableTypes.BaseDataVariableType, displayName);
        }

        public static IUADataType CreateDataType(string name, NodeId baseDataType)
        {
            return Context.NodeFactory.MakeDataType(GetNextNodeId(),
                                                    name.ToQualifiedName(),
                                                    baseDataType);
        }

        public static IUAVariable CreateDataVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.Number, UAManagedCore.OpcUa.VariableTypes.DataItemType, displayName);
        }

        public static IUAVariable CreateDateTimeVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.DateTime, VariableTypes.BaseDataVariableType, displayName);
        }

        public static IUAVariable CreateDoubleVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.Double, VariableTypes.BaseDataVariableType, displayName);
        }

        public static void CreateEnumeration(string name, List<EnumStruct> enums)
        {
            var enumDataType = CreateDataType(name, DataTypes.Enumeration);
            enumDataType.DisplayName = name.ToLocalizedText();

            var enumValuesVariable = Context.NodeFactory.MakeVariable(GetNextNodeId(),
                                                                      "EnumValues".ToQualifiedName(),
                                                                      DataTypes.EnumValueType,
                                                                      ValueRank.OneDimension,
                                                                      new uint[] { (uint)enums.Count },
                                                                      UAManagedCore.OpcUa.VariableTypes.BaseDataVariableType,
                                                                      true);
            var enumValueArray = new Struct[enums.Count];
            foreach (var enumDef in enums)
            {
                enumValueArray[enumDef.Ordinal] = new Struct(DataTypes.EnumValueType,
                    enumDef.Ordinal,
                    enumDef.Name.ToLocalizedText(),
                    enumDef.Description.ToLocalizedText());
            }

            enumValuesVariable.Value = enumValueArray;
            enumDataType.Add(enumValuesVariable);
            UaDataTypesFolder().Add(enumDataType);
        }

        public static IUAVariable CreateFiniteStateVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, UAManagedCore.OpcUa.VariableTypes.FiniteStateVariableType, VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateFiniteTransitionVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, UAManagedCore.OpcUa.VariableTypes.FiniteTransitionVariableType, VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateFloatVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.Float, VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateIntVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.Int32, VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateLongVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.Int64, VariableTypes.BaseVariableType, displayName);
        }

        public static Type GetDotNetType(IUANode node)
        {
            return GetDotNetType(node.NodeId);
        }

        public static Type GetDotNetType(NodeId nodeId)
        {
            return Context.NodeFactory.GetNetTypeFromNodeId(nodeId);
        }

        public static void AddReference(this IUANode node, NodeId referenceTypeNodeId, UANode targetNode)
        {
            node.Refs.AddReference(referenceTypeNodeId, targetNode);
        }

        public static IUAReferenceType MakeReferenceType(NodeId nodeId, QualifiedName browseName, NodeId superTypeNodeId)
        {
            return Context.NodeFactory.MakeReferenceType(nodeId, browseName, superTypeNodeId);
        }

        public static IUAMethod CreateMethod(string name, IEnumerable<IUAVariable> variables, NamingRuleType namingRuleType = NamingRuleType.None)
        {
            Log.Warning($"OptixHelpers.CreateMethod: {name}");
            IUAMethod methodNode = null;
            try
            {
                var newNodeId = GetNextNodeId();

                methodNode = Context.NodeFactory.MakeMethod(newNodeId,
                                                     name.ToQualifiedName(),
                                                     namingRuleType, WriteMask.Executable);

                if (variables is { })
                {
                    foreach (var variable in variables)
                    {
                        Log.Warning($"OptixHelpers.CreateMethod: {name} - adding variable {variable.BrowseName}");
                        methodNode.Refs.AddReference(UAManagedCore.OpcUa.ReferenceTypes.HasProperty, variable);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"OptixHelpers.CreateMethod: {name}: {ex.Message}");
            }

            return methodNode;
        }

        public static IUAObject CreateObject(string name, NodeId objectTypeId = null, NamingRuleType namingRuleType = NamingRuleType.Mandatory)
        {
            var nodeId = GetNextNodeId();

            Log.Info($"CreateObject: {name}: new NodeId {nodeId}");

            if (objectTypeId == null)
            {
                return InformationModel.MakeObject(name);
            }

            return InformationModel.MakeObject(name, objectTypeId);
        }

        public static IUAObjectType CreateObjectType(string name, IUANode baseObjectType)
        {
            NodeId baseObjectTypeNodeId = baseObjectType is not { } ? UAManagedCore.OpcUa.ObjectTypes.BaseObjectType : baseObjectType.NodeId;
            return CreateObjectType(name, baseObjectType.NodeId);
            //var nodeId = GetNextNodeId();

            //Log.Info($"CreateObjectType: {name}: new NodeId {nodeId}");

            //var objectType = InformationModel.MakeObjectType(name, baseObjectTypeNodeId);

            ////UaObjectTypesFolder().Add(objectType);
            //return objectType;
        }

        public static IUAObjectType CreateObjectType(string name, NodeId baseNodeId = null)
        {
            if (baseNodeId == null)
            {
                baseNodeId = UAManagedCore.OpcUa.ObjectTypes.BaseObjectType;
            }

            var nodeId = GetNextNodeId();

            Log.Info($"CreateObjectType: {name}: new NodeId {nodeId}");

            var objectType = InformationModel.MakeObjectType(name, baseNodeId);

            //UaObjectTypesFolder().Add(objectType);
            return objectType;
        }

        public static IUAObjectType CreateObjectType(string name, NodeId baseNodeId, IEnumerable<IUAVariable> variables)
        {
            if (baseNodeId is not { })
            {
                baseNodeId = OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType").NodeId;
            }

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

        public static IUAVariable CreateProperty(string name, string displayName = "")
        {
            return CreateScalarVariable(name, UAManagedCore.OpcUa.DataTypes.Integer, VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateScalarVariable(string name, NodeId dataType, NodeId variableType, string displayName = "")
        {
            var variable = InformationModel.MakeVariable(name, dataType, variableType);
            variable.DisplayName = string.IsNullOrEmpty(displayName) ? name.ToLocalizedText() : displayName.ToLocalizedText();

            return variable;
        }

        public static IUAVariable CreateShortVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.Int16, VariableTypes.BaseDataVariableType, displayName);
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

        public static IUAObjectType CreateTransitionType(string name)
        {
            var transitionType = (IUAObjectType)UaObjectTypesFolder().Find(name);
            if (transitionType is not { })
            {
                transitionType = CreateObjectType(name, FINITE_TRANSITION_TYPE);
            }
            return transitionType;
        }

        public static IUAObjectType CreateFiniteStateMachineType(string name)
        {
            var finiteStateMachineType = (IUAObjectType)UaObjectTypesFolder().Find(name);
            if (finiteStateMachineType is not { })
            {
                finiteStateMachineType = CreateObjectType(name, FINITE_STATE_MACHINE_TYPE);
            }
            return finiteStateMachineType;
        }

        public static IUAVariable CreateStateVariable(string name, int stateNumber, string displayName = "")
        {
            var variable = InformationModel.MakeVariable(name, DataTypes.LocalizedText, VariableTypes.FiniteStateVariableType);
            //var id = variable.GetVariable("Id");
            //id.Value = new UAValue(stateNumber);
            return variable;
        }

        public static IUAVariable CreateStringVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.String, VariableTypes.BaseDataVariableType, displayName);
        }

        public static IUAVariable CreateTransitionVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.LocalizedText, VariableTypes.FiniteTransitionVariableType, displayName);
        }

        public static IUAVariable CreateTwoStateVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, UAManagedCore.OpcUa.VariableTypes.TwoStateVariableType, VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateUIntVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.UInt32, VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateULongVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.UInt64, VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateUShortVariable(string name, string displayName = "")
        {
            return CreateScalarVariable(name, DataTypes.UInt16, VariableTypes.BaseVariableType, displayName);
        }

        public static IUAVariable CreateVariable(string name, NodeId baseVariableType, string displayName = "")
        {
            var variable = Context.NodeFactory
                .MakeVariable(GetNextNodeId(),
                              name.ToQualifiedName(),
                              DataTypes.NodeId,
                              ValueRank.OneDimension,
                              new uint[] { 0 },
                              baseVariableType,
                              true,
                              null,
                              0);
            variable.DisplayName = displayName.ToLocalizedText();
            return variable;
        }

        public static IUAVariableType CreateVariableType(string name, NodeId baseVariableType, string displayName = "")
        {
            var variableType = Context.NodeFactory
                .MakeVariableType(GetNextNodeId(),
                              name.ToQualifiedName(),
                              DataTypes.NodeId,
                              ValueRank.OneDimension,
                              new uint[] { 0 },
                              baseVariableType,
                              true,
                              true,
                              0);
            variableType.DisplayName = displayName.ToLocalizedText();
            return variableType;
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
                Log.Warning("PushAgent", "Parse Exception: " + e.Message);
                throw;
            }

            return null;
        }

        public static bool HasChildNamed(this IUANode node, string name)
        {
            return node.Children.Any(c => c.BrowseName.Equals(name));
        }

        //public static void RegisterMethod(this IUANode objectType, IUAMethod method)
        //{
        //    if (!objectType.Children.Any(c => c.BrowseName.Equals(method.BrowseName)))
        //    {
        //        objectType.Add(method);
        //    }

        //    // only register once
        //    if (!_registeredMethodHandlers.Contains(objectType.NodeId))
        //    {
        //        Context.NodeFactory.RegisterBehaviourFactoryReference(objectType.NodeId, _methodFactoryGuid, (byte) BehaviourModulePriorities.Default);
        //        _registeredMethodHandlers.Add(objectType.NodeId);
        //    }
        //}

        public static void RegisterMethod(this IUAObjectType objectType, IUAMethod method)
        {
            Log.Warning($"OptixHelpers.RegisterMethod: {objectType.BrowseName}::{method.BrowseName}()");
            if (!objectType.SuperType.Children.Any(c => c.BrowseName.Equals(method.BrowseName)) && !objectType.Children.Any(c => c.BrowseName.Equals(method.BrowseName)))
            {
                objectType.Add(method);
            }

            // only register once
            if (!_registeredMethodHandlers.Contains(objectType.NodeId))
            {
                Context.NodeFactory.RegisterBehaviourFactoryReference(objectType.NodeId, _methodFactoryGuid, (byte)BehaviourModulePriorities.Default);
                _registeredMethodHandlers.Add(objectType.NodeId);
            }
        }

        public static void RegisterMethods(this IUAObjectType objectType, IEnumerable<IUAMethod> methods)
        {
            foreach (var method in methods)
            {
                RegisterMethod(objectType, method);
            }
        }

        public static LocalizedText ToLocalizedText(this string text)
        {
            return new LocalizedText(text, "en-us");
        }

        public static QualifiedName ToQualifiedName(this string name)
        {
            return new QualifiedName(_namespaceIndex, name);
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

        #endregion Public Methods

        #region Private Methods

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

        //private static uint GetNextId()
        //{
        //    _lastId++;
        //    return _lastId++;
        //}

        private static NodeId GetNextNodeId()
        {
            return new NodeId(_namespaceIndex, _lastId++);
        }

        private static DateTime? GetTimestamp(object value)
        {
            if (value is { })
            {
                if (Type.GetTypeCode(value.GetType()) == TypeCode.DateTime)
                {
                    return ((DateTime)value);
                }
                else
                {
                    var date = value.ToDateTime();
                    return DateTime.SpecifyKind(date, DateTimeKind.Utc);
                }
            }
            return null;
        }

        #endregion Private Methods
    }
}
