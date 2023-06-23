using Cca.Cgp.Common.Model;
using Cca.Extensions.Common;
using FTOptix.HMIProject;
using System.Collections.Generic;
using System.Linq;
using UAManagedCore;

namespace NetZero.Extensions
{
    public static class CoaNodeEx
    {
        #region Public Fields

        public const string MDSM_COMPONENT_NUBER = "MDSM Component Number";
        public const string MDSM_STATE = "raC_Opr_MDSM_State";
        public const string MDSM_STATE_INITIAL = "raC_Opr_MDSM_State_Initial";
        public const string MDSM_TRANSITION = "raC_Opr_MDSM_Transition";
        public const string SM_STRANSITION = "raC_Opr_SM_Transition";

        #endregion Public Fields

        #region Private Fields

        private const string AVAILABLE_STATES = "AvailableStates";
        private const string AVAILABLE_TRANSITIONS = "AvailableTransitions";
        private const string MDSM_COMMAND = "raC_Opr_MDSM_Command";
        private const string MDSM_MODE_MODEL = "raC_opr_MDSM_ModeModel";
        private const string MDSM_PARAM_ACTIVE = "raC_opr_MDSM_ParamActive";
        private const string MDSM_PARAM_REQUEST = "raC_Opr_MDSM_ParamRequest";
        private const string MDSM_PARAM_SOURCE = "raC_Opr_MDSM_ParamSource";
        private const string MDSM_PARAMETERS = "raC_opr_MDSM_Parameters";
        private const string MDSM_SET_VALUE = "raC_Opr_MDSM_SetValue";
        private const string MDSM_STATE_MACHINE_CORE = "raC_Opr_MDSM_SM_Core";
        private static readonly IUANode _finiteStateMachineType = OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType/StateMachineType/FiniteStateMachineType");
        private static readonly IUANode _stateType = OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType/StateType");
        private static readonly IUANode _stateVariableType = OptixHelpers.UaVariableTypesFolder().Get("BaseVariableType/BaseDataVariableType/StateVariableType");
        private static readonly IUANode _transitionType = OptixHelpers.UaObjectTypesFolder().Get("BaseObjectType/TransitionType");
        private static readonly IUANode _transitionVariableType = OptixHelpers.UaObjectTypesFolder().Get("BaseVariableType/BaseDataVariableType/TransitionVariableType");

        private static readonly List<string> _opcuaTypesToSkip = new()
        {
            MDSM_STATE,
            MDSM_STATE_INITIAL,
            MDSM_TRANSITION
        };

        #endregion Private Fields

        #region Public Methods

        public static void AddStatesToInstance(this CoaNode node, IUAObject stateMachine)
        {
            var states = node.GetStates();
            foreach (var stateNode in states)
            {
                stateMachine.Add(stateNode.ToStateType());
            }
        }

        public static void AddTransitionsToInstance(this CoaNode node, IUAObject stateMachine)
        {
            var transitions = node.GetTransitions();
            foreach (var transitionNode in transitions)
            {
                stateMachine.Add(transitionNode.ToTransitionType());
            }
        }

        public static void GetAllChildren(this CoaNode node, ref List<CoaNode> nodes)
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

        public static object GetAttributeValue(this CoaNode node, string attribute)
        {
            if (node.HasAttribute(attribute))
            {
                return node.InfoAttributes.First(a => a.Name.Equals(attribute)).Value;
            }
            return null;
        }

        public static int GetCauseNumber(this CoaNode node)
        {
            var value = node.GetAttributeValue(CoaInfoAttributeEx.OPCUA_HAS_CAUSE);
            if (value is { })
            {
                return value.ToInt();
            }
            return -1;
        }

        public static IEnumerable<CoaNode> GetCauses(this CoaNode node)
        {
            return node.HasChildren() ?
                node.Children.Where(c => c.IsMdsmCommandMethod()) :
                new List<CoaNode>();
        }

        public static int GetComponentNumber(this CoaNode node)
        {
            if (node.HasAttributes() && node.HasAttribute(MDSM_COMPONENT_NUBER))
            {
                node.GetAttributeValue(MDSM_COMPONENT_NUBER);
            }
            return 0;
        }

        public static NodeId GetDataType(this CoaNode node) => node.Type switch
        {
            1 | 5 => UAManagedCore.OpcUa.DataTypes.Double,
            2 | 4 => UAManagedCore.OpcUa.DataTypes.Int32,
            3 | 10 => UAManagedCore.OpcUa.DataTypes.String,
            _ => UAManagedCore.OpcUa.DataTypes.String
        };

        public static ValueTypes GetMdsmSetValueDataType(this CoaNode node)
        {
            if (node.IsMdsmSetValueMethod())
            {
                return Cca.Extensions.Common.EnumExtensions.GetEnumValue<ValueTypes>(node.Type);
            }
            return ValueTypes.Unknown;
        }

        public static int GetOpcUaComponentNumber(this CoaNode node)
        {
            return node.HasComponentNumber() ? node.InfoAttributes.First(a => a.IsMdsmComponentNumber()).Value.ToInt() : -1;
        }

        public static List<string> GetOpcUaMethodNames(this CoaNode node)
        {
            return node.HasChildren() ? node.Children.Where(c => c.IsMethod()).Select(c => c.Name).ToList() : new List<string>();
        }

        public static CoaNode? GetChildByComponentNumber(this CoaNode node, int componentNumber)
        {
            return node.HasChildren() ?
                node.Children.First(c => c.GetComponentNumber() == componentNumber) :
                null;
        }

        public static IEnumerable<CoaNode> GetOpcUaMethods(this CoaNode node)
        {
            return node.HasChildren() ?
                node.Children.Where(c => c.IsMethod()) :
                new List<CoaNode>();
        }

        public static string GetOpcUaMethodType(this CoaNode node)
        {
            return node.IsMethod() ?
                node.InfoAttributes.First(a => a.IsOpcUaMethodType()).Value : string.Empty;
        }

        public static string GetOpcUaType(this CoaNode node)
        {
            return node.IsOpcUaType() ?
                node.InfoAttributes.First(a => a.IsOpcUaType()).Value : string.Empty;
        }

        public static IEnumerable<CoaNode> GetParameters(this CoaNode node)
        {
            return node.HasChildren() ?
                node.Children.Where(c => (c.IsMdsmParameterSource() || c.IsMdsmActiveParameter())) :
                new List<CoaNode>();
        }

        public static IEnumerable<CoaNode> GetProperties(this CoaNode node)
        {
            if (node.HasChildren())
            {
                return node.Children.Where(c => !c.IsOpcUaType() && !c.IsMethod());
            }
            return new List<CoaNode>();
        }

        public static IEnumerable<IUAVariable> GetPropertiesAsVariables(this CoaNode node)
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

        public static ValueTypes GetPropertyType(this CoaNode node)
        {
            return Cca.Extensions.Common.EnumExtensions.GetEnumValue<ValueTypes>(node.Type);
        }

        public static CoaNode? GetStateByNumber(this CoaNode node, int number)
        {
            return node.GetStatesByNumber().TryGetValue(number, out CoaNode state) ? state : null;
        }

        public static CoaNode? GetCauseByNumber(this CoaNode node, int number)
        {
            return node.GetCausesByNumber().TryGetValue(number, out CoaNode cause) ? cause : null;
        }

        public static Dictionary<int, CoaNode> GetTransitionsByNumber(this CoaNode node)
        {
            var transitions = node.GetTransitions();
            var transitionsByNumber = new Dictionary<int, CoaNode>();
            foreach (var transition in transitions)
            {
                var componentNumber = transition.GetComponentNumber();
                transitionsByNumber.Add(componentNumber, transition);
            }
            return transitionsByNumber;
        }

        public static Dictionary<int, CoaNode> GetCausesByNumber(this CoaNode node)
        {
            var causes = node.GetOpcUaMethods().Where(m => m.Name.Equals(MDSM_COMMAND));
            var causesByNumber = new Dictionary<int, CoaNode>();
            foreach (var cause in causes)
            {
                var componentNumber = cause.GetComponentNumber();
                causesByNumber.Add(componentNumber, cause);
            }
            return causesByNumber;
        }

        public static Dictionary<int, CoaNode> GetStatesByNumber(this CoaNode node)
        {
            var states = node.GetStates();
            var statesByNumber = new Dictionary<int, CoaNode>();
            foreach (var state in states)
            {
                var componentNumber = state.GetComponentNumber();
                statesByNumber.Add(componentNumber, state);
            }
            return statesByNumber;
        }

        public static IEnumerable<CoaNode> GetStates(this CoaNode node)
        {
            return node.HasChildren() ?
                node.Children.Where(c => c.IsOpcUaState()) :
                new List<CoaNode>();
        }

        public static int GetToState(this CoaNode node)
        {
            var value = node.GetAttributeValue(CoaInfoAttributeEx.OPCUA_TO_STATE);
            if (value is { })
            {
                return value.ToInt();
            }
            return -1;
        }

        public static IEnumerable<CoaNode> GetTransitions(this CoaNode node)
        {
            return node.HasChildren() ?
                node.Children.Where(c => c.IsOpcUaTransition()) :
                new List<CoaNode>();
        }

        public static bool HasAttribute(this CoaNode node, string attribute)
        {
            return node.HasAttributes() && node.InfoAttributes.Any(a => a.Name.Equals(attribute));
        }

        public static bool HasAttributes(this CoaNode node)
        {
            return node.InfoAttributes is { } && node.InfoAttributes.Length > 0;
        }

        public static bool HasCause(this CoaNode node)
        {
            return node.HasAttributes() && node.InfoAttributes.Any(a => a.IsOpcUaType());
        }

        public static bool HasChildren(this CoaNode node)
        {
            return node.Children is { } && node.Children.Length > 0;
        }

        public static bool HasComponentNumber(this CoaNode node)
        {
            return node.HasAttributes() &&
                node.InfoAttributes.Any(a => a.IsMdsmComponentNumber());
        }

        public static bool HasMethods(this CoaNode node)
        {
            return node.HasChildren() && node.Children.Any(c => c.IsMethod());
        }

        public static bool HasProperties(this CoaNode node)
        {
            return node.HasChildren() && node.Children.Any(c => !c.IsOpcUaType() && !c.IsMethod());
        }

        public static bool IsCommmandSourceModel(this CoaNode node)
        {
            //raC_Opr_MDSM_CmdSrcModel
            return node.IsOpcUaType() && node.GetOpcUaType().Equals("raC_Opr_MDSM_CmdSrcModel");
        }

        public static bool IsFiniteStateMachine(this CoaNode node)
        {
            return node.IsCommmandSourceModel() || node.IsMdsmModeModel() || node.IsMdsmStateMachineCore();
        }

        public static bool IsMdsm(this CoaNode node)
        {
            //raC_Opr_MDSM
            return node.IsOpcUaType() && node.GetOpcUaType().Equals("raC_Opr_MDSM");
        }

        public static bool IsMdsmActiveParameter(this CoaNode node)
        {
            //raC_opr_MDSM_Parameters
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(MDSM_PARAM_ACTIVE);
        }

        public static bool IsMdsmCommandMethod(this CoaNode node)
        {
            return node.IsMethod() && node.GetOpcUaMethodType().Equals(MDSM_COMMAND);
        }

        public static bool IsMdsmModeModel(this CoaNode node)
        {
            //raC_opr_MDSM_ModeModel
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(MDSM_MODE_MODEL);
        }

        public static bool IsMdsmParameters(this CoaNode node)
        {
            //raC_opr_MDSM_Parameters
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(MDSM_PARAMETERS);
        }

        public static bool IsMdsmParameterSource(this CoaNode node)
        {
            //raC_opr_MDSM_Parameters
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(MDSM_PARAM_SOURCE);
        }

        public static bool IsMdsmRequestParameter(this CoaNode node)
        {
            //raC_opr_MDSM_Parameters
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(MDSM_PARAM_REQUEST);
        }

        public static bool IsMdsmSetValueMethod(this CoaNode node)
        {
            return node.IsMethod() && node.GetOpcUaMethodType().Equals(MDSM_SET_VALUE);
        }

        public static bool IsMdsmStateMachineCore(this CoaNode node)
        {
            //raC_Opr_MDSM_SM_Core
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(MDSM_STATE_MACHINE_CORE);
        }

        public static bool IsMethod(this CoaNode node)
        {
            return node.HasAttributes() && node.InfoAttributes.Any(a => a.IsOpcUaMethodType());
        }

        public static bool IsOpcUaState(this CoaNode node)
        {
            return node.IsOpcUaType() &&
                (node.GetOpcUaType().Equals(MDSM_STATE_INITIAL) ||
                node.GetOpcUaType().Equals(MDSM_STATE));
        }

        public static bool IsOpcUaStateInitial(this CoaNode node)
        {
            return node.IsOpcUaType() && node.GetOpcUaType().Equals(MDSM_STATE_INITIAL);
        }

        public static bool IsOpcUaTransition(this CoaNode node)
        {
            return node.IsOpcUaType() &&
                (node.GetOpcUaType().Equals(SM_STRANSITION) ||
                node.GetOpcUaType().Equals(MDSM_TRANSITION));
        }

        public static bool IsOpcUaType(this CoaNode node)
        {
            return node.HasAttributes() && node.InfoAttributes.Any(a => a.IsOpcUaType());
        }

        public static IUAObjectType MakeSubType(this CoaNode node, IUAObjectType superType)
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
                Log.Warning($"MakeSubType: creating new type {nodeTypeName}");

                // create the subtype
                nodeType = OptixHelpers.CreateObjectType(nodeTypeName, superType);

                var properties = node.GetProperties();
                foreach (var propertyNode in properties)
                {
                    if (!nodeType.SuperType.Children.Any(c => c.BrowseName.Equals(propertyNode.Name)) &&
                        !nodeType.Children.Any(c => c.BrowseName.Equals(propertyNode.Name)))
                    {
                        Log.Warning($"MakeSubType: adding property {propertyNode.Name}");
                        var variable = propertyNode.ToVariable();
                        nodeType.Add(variable);
                    }
                }
                if (superType.BrowseName.Equals("FiniteStateMachineType"))
                {
                    Log.Warning($"MakeSubType: {nodeTypeName} is a subtype of {superType.BrowseName}");
                    var states = node.GetStates();
                    if (states is { } && states.Any())
                    {
                        Log.Warning($"MakeSubType: {nodeTypeName} has {states.Count()} states");
                        var availableStates = nodeType.SuperType.GetVariable(AVAILABLE_STATES);
                        if (availableStates is { })
                        {
                            var statesList = new List<NodeId>();

                            foreach (var stateNode in states)
                            {
                                // we only get here if the nodeType is a type of finite state machine
                                if (!nodeType.SuperType.HasChildNamed(stateNode.Name) && !nodeType.HasChildNamed(stateNode.Name))
                                {
                                    Log.Warning($"MakeSubType: adding state variable {stateNode.Name}");
                                    var stateNumber = stateNode.GetComponentNumber();
                                    var stateVar = OptixHelpers.CreateStateVariable(stateNode.Name, stateNumber);
                                    statesList.Add(stateVar.NodeId);
                                    nodeType.Add(stateVar);
                                    availableStates.Add(stateVar);
                                }
                                else
                                {
                                    Log.Warning($"MakeSubType: {nodeTypeName} already has a child named {stateNode.Name}");
                                }
                            }

                            availableStates.Value = statesList.ToArray();
                        }
                    }
                    else
                    {
                        Log.Warning($"MakeSubType: {nodeTypeName} has no states");
                    }

                    var transitions = node.GetTransitions();
                    if (transitions is { } && transitions.Any())
                    {
                        Log.Warning($"MakeSubType: {nodeTypeName} has {transitions.Count()} transitions");
                        var availableTransitions = nodeType.SuperType.GetVariable(AVAILABLE_TRANSITIONS);

                        if (availableTransitions is { })
                        {
                            var transitionsList = new List<NodeId>();

                            foreach (var transitionNode in transitions)
                            {
                                if (!nodeType.SuperType.HasChildNamed(transitionNode.Name) && !nodeType.HasChildNamed(transitionNode.Name))
                                {
                                    Log.Warning($"MakeSubType: adding transition variable {transitionNode.Name}");
                                    var transitionVar = OptixHelpers.CreateTransitionVariable(transitionNode.Name);
                                    transitionsList.Add(transitionVar.NodeId);
                                    nodeType.Add(transitionVar);
                                }
                                else
                                {
                                    Log.Warning($"MakeSubType: {nodeTypeName} already has a child named {transitionNode.Name}");
                                }
                            }
                            availableTransitions.Value = transitionsList.ToArray();
                        }
                    }
                    else
                    {
                        Log.Warning($"MakeSubType: {nodeTypeName} has no transitions");
                    }

                    var methods = node.GetOpcUaMethods();
                    foreach (var childMethod in methods)
                    {
                        Log.Warning($"MakeSubType: {nodeTypeName} adding method {childMethod.Name}");
                        AddMethodToType(nodeType, childMethod);
                    }
                }
            }

            return nodeType is { } ? (IUAObjectType)nodeType : null;
        }

        public static IUAObject ToStateType(this CoaNode node)
        {
            if (node.IsOpcUaState())
            {
                IUAObject state;
                if (node.IsOpcUaStateInitial())
                {
                    state = InformationModel.MakeObject(node.Name);
                }
                else
                {
                    state = InformationModel.MakeObject(node.Name);
                }
                return state;
            }

            return null;
        }

        public static IUAObject ToTransitionType(this CoaNode node)
        {
            if (node.IsOpcUaTransition())
            {
                return InformationModel.MakeObject(node.Name);
            }
            return null;
        }

        #endregion Public Methods

        #region Private Methods

        private static void AddMethodToType(IUAObjectType nodeType, CoaNode childMethod)
        {
            if (!nodeType.SuperType.HasChildNamed(childMethod.Name) && !nodeType.HasChildNamed(childMethod.Name))
            {
                Log.Warning($"MakeSubType: {nodeType.BrowseName}: adding method {childMethod.Name}");
                var variables = childMethod.GetPropertiesAsVariables();
                var method = OptixHelpers.CreateMethod(childMethod.Name, variables);
                if (method is { })
                {
                    OptixHelpers.RegisterMethod(nodeType, method);
                }
            }
        }

        private static IUAVariable ToVariable(this CoaNode property) => property.Type switch
        {
            1 | 5 => InformationModel.MakeVariable(property.Name, UAManagedCore.OpcUa.DataTypes.Double),
            2 | 4 => InformationModel.MakeVariable(property.Name, UAManagedCore.OpcUa.DataTypes.Int32),
            3 | 10 => InformationModel.MakeVariable(property.Name, UAManagedCore.OpcUa.DataTypes.String),
            _ => InformationModel.MakeVariable(property.Name, UAManagedCore.OpcUa.DataTypes.String)
        };

        #endregion Private Methods
    }
}