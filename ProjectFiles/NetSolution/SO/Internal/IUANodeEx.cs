using Cca.Extensions.Common;
using FTOptix.HMIProject;
using System;
using System.Collections.Generic;
using System.Linq;
using UAManagedCore;

namespace NetZero.Internal
{
    public static class IUANodeEx
    {
        #region Private Fields

        private static readonly IUANode _finiteStateMachineType = OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType/StateMachineType/FiniteStateMachineType");

        private static readonly List<string> _opcuaTypesToSkip = new()
        {
            Common.MDSM_STATE,
            Common.MDSM_STATE_INITIAL,
            Common.MDSM_TRANSITION
        };

        private static readonly IUANode _stateType = OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType/StateType");
        private static readonly IUANode _stateVariableType = OptixHelpers.UaVariableTypesFolder().Get("BaseVariableType/BaseDataVariableType/StateVariableType");
        private static readonly IUANode _transitionType = OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType/TransitionType");
        private static readonly IUANode _transitionVariableType = OptixHelpers.UaObjectTypesFolder().Get("BaseVariableType/BaseDataVariableType/TransitionVariableType");

        #endregion Private Fields

        #region Public Methods

        public static void AddDynamicLinkToVariable(this IUANode node, IUAVariable variable)
        {
            var nodeVariable = node.GetVariable(variable.BrowseName);
            if (nodeVariable is not { })
            {
                Log.Info(nameof(IUANodeEx), $"{nameof(AddDynamicLinkToVariable)}: {Log.Node(node)}: Adding new variable {variable.BrowseName}");
                try
                {
                    nodeVariable = variable.Clone();
                    node.Add(nodeVariable);
                    Log.Info(nameof(IUANodeEx), $"{nameof(AddDynamicLinkToVariable)}: {Log.Node(node)}: new cloned variable {Log.Node(nodeVariable)}");
                    nodeVariable = node.GetVariable(variable.BrowseName);
                }
                catch (Exception ex)
                {
                    Log.Error(nameof(IUANodeEx), $"{nameof(AddDynamicLinkToVariable)}: {Log.Node(node)}: {ex.Message}");
                    var nodeVars = node.GetChildVariableNodes();
                    Log.Error(nameof(IUANodeEx), $"{nameof(AddDynamicLinkToVariable)}: {Log.Node(node)}:");
                    foreach (var nodeVar in nodeVars)
                    {
                        Log.Error(nameof(IUANodeEx), $"{nameof(AddDynamicLinkToVariable)}: existing var {nodeVar.BrowseName}");
                    }
                }
            }
            else
            {
                Log.Info(nameof(IUANodeEx), $"{nameof(AddDynamicLinkToVariable)}: using existing variable {Log.Node(nodeVariable)}");
            }
            Log.Info(nameof(IUANodeEx), $"{nameof(AddDynamicLinkToVariable)}: target: [{Log.Node(nodeVariable)}] source: [{Log.Node(variable)}] type: {variable.GetSystemType().Name}");
            nodeVariable.SetDynamicLink(variable, FTOptix.CoreBase.DynamicLinkMode.ReadWrite);
            Log.Info(nameof(IUANodeEx), $"{nameof(AddDynamicLinkToVariable)}: Done");
        }

        public static void AddStatesToInstance(this IUANode node, IUAObject stateMachine)
        {
            var states = node.GetStates();
            foreach (var stateNode in states)
            {
                stateMachine.Add(stateNode.ToStateType());
            }
        }

        public static void AddTransitionsToInstance(this IUANode node, IUAObject stateMachine)
        {
            var transitions = node.GetTransitions();
            foreach (var transitionNode in transitions)
            {
                stateMachine.Add(transitionNode.ToTransitionType());
            }
        }

        public static bool IsInModel(this IUANode node)
        {
            return Log.Node(node).Contains('\\');
        }

        public static void AddVariables(this IUANode node, IEnumerable<IUAVariable> variables)
        {
            foreach (var variable in variables)
            {
                try
                {
                    if (variable is { } && !Common.ATTRIBUTES_TO_SKIP.Contains(variable.BrowseName))
                    {
                        Log.Info(nameof(IUANodeEx), $"AddVariables: {node.BrowseName}: Adding {variable.BrowseName}");
                        node.AddDynamicLinkToVariable(variable);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(nameof(IUANodeEx), $"AddVariables: {node.BrowseName}: {ex.Message}");
                }
            }
        }

        public static void GetAllChildren(this IUANode node, ref List<IUANode> nodes)
        {
            if (node.HasChildren())
            {
                foreach (var child in node.Children)
                {
                    nodes.Add(child);
                    child.GetAllChildren(ref nodes);
                }
            }
        }

        public static object GetAttributeValue(this IUANode node, string attribute)
        {
            var variable = node.GetVariableByName(attribute);

            if (variable is { } && variable.Value is { })
            {
                return variable.Value.Value;
            }

            return null;
        }

        public static IUANode GetCauseByNumber(this IUANode node, int number)
        {
            return node.GetCausesByNumber().TryGetValue(number, out IUANode cause) ? cause : null;
        }

        public static int GetCauseNumber(this IUANode node)
        {
            var value = node.GetAttributeValue(Common.OPCUA_HAS_CAUSE);
            if (value is { })
            {
                return value.ToInt();
            }
            return -1;
        }

        public static IEnumerable<IUANode> GetCauses(this IUANode node)
        {
            IEnumerable<IUANode> causes = new List<IUANode>();
            try
            {
                causes = node.HasMethods() ?
                    node.Children.Where(c => c.IsMdsmCommandMethod()).ToList() :
                    new List<IUANode>();
            }
            catch (System.Exception ex)
            {
                Log.Error(nameof(IUANodeEx), $"{nameof(GetCauses)}: {ex.Message}");
            }
            return causes;
        }

        public static Dictionary<int, IUANode> GetCausesByNumber(this IUANode node)
        {
            var methods = node.GetCauses();
            var causes = methods.Where(m => m.IsMdsmCommandMethod());
            var causesByNumber = new Dictionary<int, IUANode>();
            foreach (var cause in causes)
            {
                var componentNumber = cause.GetComponentNumber();
                causesByNumber[componentNumber] = cause;
            }
            return causesByNumber;
        }

        public static IUANode GetChildByComponentNumber(this IUANode node, int componentNumber)
        {
            return node.HasChildren() ?
                node.Children.First(c => c.GetComponentNumber() == componentNumber) :
                null;
        }

        public static IEnumerable<NodeId> GetChildrenByType(this IUANode node, NodeId typeToFind)
        {
            Log.Info(nameof(IUANodeEx), $"{nameof(GetChildrenByType)}: {Log.Node(node)} finding type: {Log.Node(InformationModel.Get(typeToFind))}");
            IEnumerable<NodeId> nodes = new List<NodeId>();
            if (node.HasChildren())
            {
                var objectNodes = node.Children.Where(c => c is not null && c is IUAObject).Select(c => c as IUAObject);
                nodes = objectNodes.Where(o => o is not null && o.ObjectType.NodeId == typeToFind).Select(o => o.NodeId);
            }
            return nodes;
        }

        public static IEnumerable<IUAObject> GetChildObjectNodes(this IUANode node)
        {
            return node.Children.Where(c => c is { } && c is IUAObject).Cast<IUAObject>();
        }

        public static IEnumerable<IUAVariable> GetChildVariableNodes(this IUANode node)
        {
            return node.Children.Where(c => c is { } && c is IUAVariable).Cast<IUAVariable>();
        }

        public static IUANode GetCommandSourceModel(this IUANode node)
        {
            return node.HasChildren() ? node.Children.First(c => c.IsMdsmCommmandSourceModel()) : null;
        }

        public static int GetComponentNumber(this IUANode node)
        {
            var number = node.GetAttributeValue(Common.MDSM_COMPONENT_NUMBER);
            return number is { } ? number.ToInt() : 0;
        }

        public static NodeId GetDataType(this IUANode node) => node.GetNodeType() switch
        {
            1 => UAManagedCore.OpcUa.DataTypes.Double,
            4 => UAManagedCore.OpcUa.DataTypes.Double,
            2 => UAManagedCore.OpcUa.DataTypes.Int32,
            5 => UAManagedCore.OpcUa.DataTypes.Int32,
            _ => UAManagedCore.OpcUa.DataTypes.String
        };

        public static string GetMdsmClassDeclaration(this IUANode node)
        {
            return node.IsMdsmClassDeclaration() ? node.GetAttributeValue(Common.MDSM_CLASS_DECLARATION).ToString() : null;
        }

        public static IUANode GetModeModel(this IUANode node)
        {
            return node.HasChildren() ? node.Children.First(c => c.IsMdsmModeModel()) : null;
        }

        //public static ValueTypes GetMdsmSetValueDataType(this IUANode node)
        //{
        //    if (node.IsMdsmSetValueMethod())
        //    {
        //        return Cca.Extensions.Common.EnumExtensions.GetEnumValue<ValueTypes>(node.GetNodeType());
        //    }
        //    return ValueTypes.Unknown;
        //}
        public static int GetNodeType(this IUANode node) => node.Get<IUAVariable>("type").Value.Value.ToInt();

        public static int GetOpcUaComponentNumber(this IUANode node)
        {
            return node.HasComponentNumber() ? node.InfoAttributes().First(a => a.IsMdsmComponentNumber()).Value.ToInt() : -1;
        }

        public static List<string> GetOpcUaMethodNames(this IUANode node)
        {
            return node.HasChildren() ? node.Children.Where(c => c.IsMethod()).Select(c => c.BrowseName).ToList() : new List<string>();
        }

        public static IEnumerable<IUANode> GetOpcUaMethods(this IUANode node)
        {
            return node.HasMethods() ?
                node.Children.Where(c => c.IsMethod()) :
                new List<IUANode>();
        }

        public static string GetOpcUaMethodType(this IUANode node)
        {
            return node.IsMethod() ?
                node.InfoAttributes().First(a => a.IsOpcUaMethodType()).Value : string.Empty;
        }

        public static string GetOpcUaType(this IUANode node)
        {
            return node.IsOpcUaType() ?
                node.InfoAttributes().First(a => a.IsOpcUaType()).Value : string.Empty;
        }

        public static IUANode GetOrCreateNode(this IUANode root, string browseName, NodeId typeToCreate)
        {
            var node = root.Get(browseName);
            if (node is not { })
            {
                node = InformationModel.MakeObject(browseName, typeToCreate);
                root.Add(node);
            }
            return node;
        }

        public static IEnumerable<IUANode> GetParameters(this IUANode node)
        {
            return node.HasChildren() ?
                node.Children.Where(c => (c.IsMdsmParameterSource() || c.IsMdsmActiveParameter())) :
                new List<IUANode>();
        }

        public static IUANode GetParametersNode(this IUANode node)
        {
            return node.HasChildren() ?
                System.Array.Find(node.Children.ToArray(), c => c.IsMdsmParameters()) :
                null;
        }

        public static IEnumerable<IUANode> GetProperties(this IUANode node)
        {
            if (node.HasChildren())
            {
                return node.Children.Where(c => !c.IsOpcUaType() && !c.IsMethod());
            }
            return new List<IUANode>();
        }

        public static IEnumerable<IUAVariable> GetPropertiesAsVariables(this IUANode node)
        {
            var variables = new List<IUAVariable>();
            if (node.HasChildren())
            {
                var properties = node.Children.Where(c => !c.IsOpcUaType() && !c.IsMethod());
                foreach (var property in properties)
                {
                    var variable = property.ToVariable();
                    if (variable is { })
                    {
                        variables.Add(variable);
                    }
                }
            }
            return variables;
        }

        public static NodeId GetStateByNumber(this IUANode stateMachine, int number)
        {
            Log.Info(nameof(IUANodeEx), $"{nameof(GetStateByNumber)}: {Log.Node(stateMachine)}: {number}");
            NodeId nodeId = null;

            var stateIds = stateMachine.GetChildrenByType(Common.STATE_TYPE.NodeId);

            if (stateIds.Any())
            {
                foreach (var stateId in stateIds)
                {
                    if (stateId is not { })
                    {
                        continue;
                    }
                    var state = InformationModel.Get(stateId);
                    if (state is not { })
                    {
                        continue;
                    }
                    var stateNumberVar = state.GetVariable(Common.STATE_NUMBER);
                    if (stateNumberVar is not { })
                    {
                        continue;
                    }
                    var stateNumber = stateNumberVar.Value.Value.ToInt();
                    //Log.Info(nameof(IUANodeEx), $"{nameof(GetStateByNumber)}: {Log.Node(stateMachine)}: {number} testing {state.BrowseName} : {stateNumber}");
                    if (stateNumber == number)
                    {
                        nodeId = state.NodeId;
                        Log.Info(nameof(IUANodeEx), $"{nameof(GetStateByNumber)}: {Log.Node(stateMachine)}: {number} = {state.BrowseName}");
                        break;
                    }
                }
                if (nodeId is not { })
                {
                    nodeId = stateIds.First();
                }
            }
            else
            {
                Log.Error(nameof(IUANodeEx), $"{nameof(GetStateByNumber)}: {Log.Node(stateMachine)}: {number} no states found");
            }

            if (nodeId is not { })
            {
                Log.Error(nameof(IUANodeEx), $"{nameof(GetStateByNumber)}: {Log.Node(stateMachine)}: {number} not found");
            }

            return nodeId;
        }

        //public static ValueTypes GetPropertyType(this IUANode node)
        //{
        //    return Cca.Extensions.Common.EnumExtensions.GetEnumValue<ValueTypes>(node.GetNodeType());
        //}
        //public static IUANode? GetStateByNumber(this IUANode node, int number)
        //{
        //    return node.GetStatesByNumber().TryGetValue(number, out IUANode state) ? state : null;
        //}

        public static IUANode GetStateMachineCoreModel(this IUANode node)
        {
            return node.HasChildren() ? node.Children.First(c => c.IsMdsmStateMachineCore()) : null;
        }

        public static IEnumerable<IUANode> GetStates(this IUANode node)
        {
            return node.HasChildren() ?
                node.Children.Where(c => c.IsOpcUaState()) :
                new List<IUANode>();
        }

        public static Dictionary<int, IUANode> GetStatesByNumber(this IUANode node)
        {
            var states = node.GetStates();
            var statesByNumber = new Dictionary<int, IUANode>();
            foreach (var state in states)
            {
                var componentNumber = state.GetComponentNumber();
                statesByNumber[componentNumber] = state;
            }
            return statesByNumber;
        }

        public static int GetToState(this IUANode node)
        {
            var value = node.GetAttributeValue(Common.OPCUA_TO_STATE);
            if (value is { })
            {
                return value.ToInt();
            }
            return -1;
        }

        //public static NodeId? GetTransitionByNumber(this IUAObjectType stateMachineType, int number)
        //{
        //    NodeId transitionId = null;

        //    return transitionId;
        //}

        public static NodeId GetTransitionByNumber(this IUANode stateMachine, int number)
        {
            Log.Info(nameof(IUANodeEx), $"{nameof(GetTransitionByNumber)}: {Log.Node(stateMachine)}: {number}");
            NodeId nodeId = null;

            var transitionIds = stateMachine.GetChildrenByType(Common.TRANSITION_TYPE.NodeId);

            if (transitionIds.Any())
            {
                foreach (var transitionId in transitionIds)
                {
                    if (transitionId is not { })
                    {
                        continue;
                    }
                    var transition = InformationModel.Get(transitionId);
                    if (transition is not { })
                    {
                        continue;
                    }
                    var transitionNumberVar = transition.GetVariable(Common.TRANSITION_NUMBER);
                    if (transitionNumberVar is not { })
                    {
                        continue;
                    }
                    var transitionNumber = transitionNumberVar.Value.Value.ToInt();
                    //Log.Info(nameof(IUANodeEx), $"{nameof(GetTransitionByNumber)}: {Log.Node(stateMachine)}: {number} testing {transition.BrowseName} : {transitionNumber}");
                    if (transitionNumber == number)
                    {
                        nodeId = transition.NodeId;
                        Log.Info(nameof(IUANodeEx), $"{nameof(GetTransitionByNumber)}: {Log.Node(stateMachine)}: {number} = {transition.BrowseName}");
                        break;
                    }
                }
                if (nodeId is not { })
                {
                    nodeId = transitionIds.First();
                }
            }
            else
            {
                Log.Error(nameof(IUANodeEx), $"{nameof(GetTransitionByNumber)}: {Log.Node(stateMachine)}: {number}: no transitions found");
            }

            if (nodeId is not { })
            {
                Log.Error(nameof(IUANodeEx), $"{nameof(GetTransitionByNumber)}: {Log.Node(stateMachine)}: {number} not found");
            }

            return nodeId;
        }

        public static IEnumerable<IUANode> GetTransitions(this IUANode node)
        {
            return node.HasChildren() ?
                node.Children.Where(c => c.IsOpcUaTransition()) :
                new List<IUANode>();
        }

        public static Dictionary<int, IUANode> GetTransitionsByNumber(this IUANode node)
        {
            var transitions = node.GetTransitions();
            var transitionsByNumber = new Dictionary<int, IUANode>();
            foreach (var transition in transitions)
            {
                var componentNumber = transition.GetComponentNumber();
                transitionsByNumber.Add(componentNumber, transition);
            }
            return transitionsByNumber;
        }

        public static IUAVariable GetVariableByName(this IUANode node, string variableName)
        {
            if (node.Children.Any(c => c is IUAVariable && c.BrowseName.Equals(variableName)))
            {
                return node.Get<IUAVariable>(variableName);
            }
            return null;
        }

        public static bool HasAttribute(this IUANode node, string attribute)
        {
            return node.HasAttributes() && node.InfoAttributes().Any(a => a.BrowseName.Equals(attribute));
        }

        public static bool HasAttributes(this IUANode node)
        {
            return node.Children is { } && node.Children.Any(c => c is IUAVariable);
            //return node.InfoAttributes() is { } && node.InfoAttributes().Any();
        }

        public static bool HasCause(this IUANode node)
        {
            return node.HasAttributes() && node.InfoAttributes().Any(a => a.IsMdsmCommandMethod());
        }

        public static bool HasChildren(this IUANode node)
        {
            return node.Children.Any(c => c is not IUAVariable);
            //return node.Children is { } && node.Children.Count - node.InfoAttributes().Count() > 0;
        }

        public static bool HasComponentNumber(this IUANode node)
        {
            return node.HasAttributes() &&
                node.InfoAttributes().Any(a => a.IsMdsmComponentNumber());
        }

        public static bool HasMethods(this IUANode node)
        {
            var hasMethods = node.HasChildren() && node.Children.Any(c => c.IsMethod());
            return hasMethods;
        }

        public static bool HasProperties(this IUANode node)
        {
            return node.HasChildren() && node.Children.Any(c => !c.IsOpcUaType() && !c.IsMethod());
        }

        public static IEnumerable<IUAVariable> InfoAttributes(this IUANode node)
        {
            if (node.Children.Any(c => c is IUAVariable))
            {
                return (IEnumerable<IUAVariable>)(node.Children.Where(c => c is IUAVariable).Select(v => v as IUAVariable));
            }
            return new List<IUAVariable>();
        }

        public static bool IsFiniteStateMachine(this IUANode node)
        {
            return node.IsMdsmCommmandSourceModel() || node.IsMdsmModeModel() || node.IsMdsmStateMachineCore();
        }

        public static bool IsMdsm(this IUANode node)
        {
            //raC_Opr_MDSM
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(Common.MDSM_TYPE);
        }

        public static bool IsMdsmActiveParameter(this IUANode node)
        {
            //raC_opr_MDSM_Parameters
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(Common.MDSM_PARAM_ACTIVE);
        }

        public static bool IsMdsmClassDeclaration(this IUANode node)
        {
            return node.HasAttribute(Common.MDSM_CLASS_DECLARATION);
        }

        public static bool IsMdsmCommandMethod(this IUANode node)
        {
            var isCommmand = node.IsMethod() && node.GetOpcUaMethodType().Equals(Common.MDSM_COMMAND);
            return isCommmand;
        }

        public static bool IsMdsmCommmandSourceModel(this IUANode node)
        {
            //raC_Opr_MDSM_CmdSrcModel
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(Common.MDSM_COMMAND_SOURCE_MODEL);
        }

        public static bool IsMdsmModeModel(this IUANode node)
        {
            //raC_opr_MDSM_ModeModel
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(Common.MDSM_MODE_MODEL);
        }

        public static bool IsMdsmParameters(this IUANode node)
        {
            //raC_opr_MDSM_Parameters
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(Common.MDSM_PARAMETERS);
        }

        public static bool IsMdsmParameterSource(this IUANode node)
        {
            //raC_opr_MDSM_Parameters
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(Common.MDSM_PARAM_SOURCE);
        }

        public static bool IsMdsmRequestParameter(this IUANode node)
        {
            //raC_opr_MDSM_Parameters
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(Common.MDSM_PARAM_REQUEST);
        }

        public static bool IsMdsmSetValueMethod(this IUANode node)
        {
            return node.IsMethod() && node.GetOpcUaMethodType().Equals(Common.MDSM_SET_VALUE);
        }

        public static bool IsMdsmStateMachineCore(this IUANode node)
        {
            //raC_Opr_MDSM_SM_Core
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(Common.MDSM_STATE_MACHINE_CORE);
        }

        public static bool IsMethod(this IUANode node)
        {
            return node.HasAttributes() && node.InfoAttributes().Any(a => a.IsOpcUaMethodType());
        }

        public static bool IsObject(this IUANode node)
        {
            return node is IUAObject;
        }

        public static bool IsObjectType(this IUANode node)
        {
            return node is IUAObjectType;
        }

        public static bool IsOpcUaState(this IUANode node)
        {
            return node.IsOpcUaType() &&
                (node.GetOpcUaType().Equals(Common.MDSM_STATE_INITIAL) ||
                node.GetOpcUaType().Equals(Common.SM_STATE) ||
                node.GetOpcUaType().Equals(Common.MDSM_STATE));
        }

        public static bool IsOpcUaStateInitial(this IUANode node)
        {
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(Common.MDSM_STATE_INITIAL);
        }

        public static bool IsOpcUaTransition(this IUANode node)
        {
            return node.IsOpcUaType() &&
                (node.GetOpcUaType().Equals(Common.MDSM_TRANSITION) ||
                node.GetOpcUaType().Equals(Common.SM_TRANSITION)
                );
        }

        public static bool IsOpcUaType(this IUANode node)
        {
            return node.HasAttributes() && node.InfoAttributes().Any(a => a.IsOpcUaType());
        }

        public static IUAObjectType MakeSubType(this IUANode node, IUAObjectType superType)
        {
            var nodeTypeName = node.GetOpcUaType();
            if (_opcuaTypesToSkip.Contains(nodeTypeName))
            {
                return null;
            }

            IUAObjectType nodeType = OptixHelpers.GetObjectTypeByName(nodeTypeName);
            if (nodeType is { })
            {
                return nodeType;
            }
            if (!superType.Children.Any(c => c.BrowseName.Equals(nodeTypeName)))
            {
                Log.Info(nameof(IUANodeEx), $"MakeSubType: creating new type {nodeTypeName}");

                // create the subtype
                nodeType = OptixHelpers.CreateObjectType(nodeTypeName, superType);

                var properties = node.GetProperties();
                foreach (var propertyNode in properties)
                {
                    if (!nodeType.SuperType.Children.Any(c => c.BrowseName.Equals(propertyNode.BrowseName)) &&
                        !nodeType.Children.Any(c => c.BrowseName.Equals(propertyNode.BrowseName)))
                    {
                        Log.Info(nameof(IUANodeEx), $"MakeSubType: adding property {propertyNode.BrowseName}");
                        var variable = propertyNode.ToVariable();
                        nodeType.Add(variable);
                    }
                }
                if (superType.BrowseName.Equals("FiniteStateMachineType"))
                {
                    Log.Info(nameof(IUANodeEx), $"MakeSubType: {nodeTypeName} is a subtype of {superType.BrowseName}");
                    var states = node.GetStates();
                    if (states is { } && states.Any())
                    {
                        Log.Info(nameof(IUANodeEx), $"MakeSubType: {nodeTypeName} has {states.Count()} states");
                        var availableStates = nodeType.SuperType.GetVariable(Common.AVAILABLE_STATES);
                        if (availableStates is { })
                        {
                            var statesList = new List<NodeId>();

                            foreach (var stateNode in states)
                            {
                                // we only get here if the nodeType is a type of finite state machine
                                if (!nodeType.SuperType.HasChildNamed(stateNode.BrowseName) && !nodeType.HasChildNamed(stateNode.BrowseName))
                                {
                                    Log.Info(nameof(IUANodeEx), $"MakeSubType: adding state variable {stateNode.BrowseName}");
                                    var stateNumber = stateNode.GetComponentNumber();
                                    var stateVar = OptixHelpers.CreateStateVariable(stateNode.BrowseName, stateNumber);
                                    statesList.Add(stateVar.NodeId);
                                    nodeType.Add(stateVar);
                                    availableStates.Add(stateVar);
                                }
                                else
                                {
                                    Log.Info(nameof(IUANodeEx), $"MakeSubType: {nodeTypeName} already has a child named {stateNode.BrowseName}");
                                }
                            }

                            availableStates.Value = statesList.ToArray();
                        }
                    }
                    else
                    {
                        Log.Info(nameof(IUANodeEx), $"MakeSubType: {nodeTypeName} has no states");
                    }
                }
            }

            return nodeType is { } ? (IUAObjectType)nodeType : null;
        }

        public static IUAObject ToStateType(this IUANode node)
        {
            if (node.IsOpcUaState())
            {
                IUAObject state;
                if (node.IsOpcUaStateInitial())
                {
                    state = InformationModel.MakeObject(node.BrowseName);
                }
                else
                {
                    state = InformationModel.MakeObject(node.BrowseName);
                }
                return state;
            }

            return null;
        }

        public static IUAObject ToTransitionType(this IUANode node)
        {
            if (node.IsOpcUaTransition())
            {
                return InformationModel.MakeObject(node.BrowseName);
            }
            return null;
        }

        #endregion Public Methods

        #region Private Methods

        private static IUAVariable ToVariable(this IUANode property) => property.GetNodeType() switch
        {
            1 | 4 => InformationModel.MakeVariable(property.BrowseName, UAManagedCore.OpcUa.DataTypes.Double),
            2 | 5 => InformationModel.MakeVariable(property.BrowseName, UAManagedCore.OpcUa.DataTypes.Int32),
            3 | 10 => InformationModel.MakeVariable(property.BrowseName, UAManagedCore.OpcUa.DataTypes.String),
            _ => InformationModel.MakeVariable(property.BrowseName, UAManagedCore.OpcUa.DataTypes.String)
        };

        #endregion Private Methods
    }
}