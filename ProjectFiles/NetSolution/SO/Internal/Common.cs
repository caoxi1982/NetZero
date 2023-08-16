using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UAManagedCore;

namespace NetZero.Internal
{
    public static class Common
    {
        public const string ModelNamePrefix = "Taget for ";

        #region Public Fields

        public const string ACTIVE_VALUES_PROPERTY_NAME = "ActiveValues";
        public const string AVAILABLE_STATES = "AvailableStates";
        public const string AVAILABLE_TRANSITIONS = "AvailableTransitions";
        public const string COMMAND_SOURCE_MODEL_PROPERTY_NAME = "CommandSourceModel";
        public const string COMMAND_TRIGGER = "CommandTrigger";
        public const string CURRENT_STATE = "CurrentState";
        public const string EXECUTABLE = "Executable";
        public const string FROM_STATE = "FromState";
        public const string ID = "Id";
        public const string LAST_TRANSITION = "LastTransition";
        public const string MDSM = "MDSM";
        public const string MDSM_CLASS_DECLARATION = "MDSM Class Declaration";
        public const string MDSM_COMMAND = "raC_Opr_MDSM_Command";
        public const string MDSM_COMMAND_SOURCE_MODEL = "raC_Opr_MDSM_CmdSrcModel";
        public const string MDSM_COMPONENT_NUMBER = "MDSM Component Number";
        public const string MDSM_MODE_MODEL = "raC_Opr_MDSM_ModeModel";
        public const string MDSM_PARAM_ACTIVE = "raC_Opr_MDSM_ParamActive";
        public const string MDSM_PARAM_REQUEST = "raC_Opr_MDSM_ParamRequest";
        public const string MDSM_PARAM_SOURCE = "raC_Opr_MDSM_ParamSource";
        public const string MDSM_PARAMETERS = "raC_Opr_MDSM_Parameters";
        public const string MDSM_SET_VALUE = "raC_Opr_MDSM_SetValue";
        public const string MDSM_STATE = "raC_Opr_MDSM_State";
        public const string MDSM_STATE_INITIAL = "raC_Opr_MDSM_State_Initial";
        public const string MDSM_STATE_MACHINE_CORE = "raC_Opr_MDSM_SM_Core";
        public const string MDSM_TRANSITION = "raC_Opr_MDSM_Transition";
        public const string MDSM_TYPE = "raC_Opr_MDSM";
        public const string MDSMCLASSDECLARATION_Node = "MDSMClassDeclarations";
        public const string MODE_MODEL_PROPERTY_NAME = "ModeModel";
        public const string OPCUA_HAS_CAUSE = "OPCUA_Ref_HasCause";
        public const string OPCUA_METHOD_TYPE = "OPCUA Method Type";
        public const string OPCUA_REF_HAS_CAUSE = "OPCUA_Ref_HasCause";
        public const string OPCUA_REF_TO_STATE = "OPCUA_Ref_ToState";
        public const string OPCUA_TO_STATE = "OPCUA_Ref_ToState";
        public const string OPCUA_TYPE = "OPCUA Type";
        public const string PARAMETERS_PROPERTY_NAME = "Parameters";
        public const string REQUESTED_VALUES_PROPERTY_NAME = "RequestedValues";
        public const string SM_CORE_PROPERTY_NAME = "StateMachineCore";
        public const string SM_STATE = "raC_Opr_SM_State";
        public const string SM_TRANSITION = "raC_Opr_SM_Transition";
        public const string STATE_NUMBER = "StateNumber";
        public const string TO_STATE = "ToState";
        public const string TRANSITION_NUMBER = "TransitionNumber";
        public const string PATH_BASE_OBJECT_TYPE = "BaseObjectType";
        public const string PATH_FOLDER_TYPE = $"{PATH_BASE_OBJECT_TYPE}/FolderType";
        public const string PATH_MDSM_PARAMETERS_TYPE = $"{PATH_FOLDER_TYPE}/raC_Opr_MDSM_Parameters";
        public const string PATH_MDSM_PARAM_ACTIVE_TYPE = $"{PATH_FOLDER_TYPE}/raC_Opr_MDSM_ParamActive";
        public const string PATH_MDSM_PARAM_REQUEST_TYPE = $"{PATH_FOLDER_TYPE}/raC_Opr_MDSM_ParamRequest";
        public const string PATH_MDSM_PARAM_SOURCE_TYPE = $"{PATH_FOLDER_TYPE}/raC_Opr_MDSM_ParamSource";
        public const string PATH_STATE_MACHINE_TYPE = $"{PATH_BASE_OBJECT_TYPE}/StateMachineType";
        public const string PATH_MDSM_BASE_TYPE = $"{PATH_BASE_OBJECT_TYPE}/raC_Opr_MDSM";
        public const string PATH_FINITE_STATE_MACHINE_TYPE = $"{PATH_STATE_MACHINE_TYPE}/FiniteStateMachineType";
        public const string PATH_MDSM_CMD_SRC_TYPE = $"{PATH_FINITE_STATE_MACHINE_TYPE}/raC_Opr_MDSM_CmdSrcModel";
        public const string PATH_MDSM_MODE_MODEL_TYPE = $"{PATH_FINITE_STATE_MACHINE_TYPE}/raC_Opr_MDSM_ModeModel";
        public const string PATH_MDSM_SM_CORE_TYPE = $"{PATH_FINITE_STATE_MACHINE_TYPE}/raC_Opr_MDSM_SM_Core";
        public const string PATH_STATE_TYPE = "BaseObjectType/StateType";
        public const string PATH_TRANSITION_TYPE = "BaseObjectType/TransitionType";

        public static readonly List<string> ATTRIBUTES_TO_SKIP = new()
        {
            "nodeType",
            "TestVar",
            "type",
            CURRENT_STATE,
            LAST_TRANSITION,
            OPCUA_TYPE,
            OPCUA_METHOD_TYPE,
            OPCUA_TO_STATE,
            MDSM_CLASS_DECLARATION,
            MDSM_COMPONENT_NUMBER
        };

        public static readonly UAObjectType BASE_OBJECT_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get(PATH_BASE_OBJECT_TYPE);
        public static readonly UAObjectType FINITE_STATE_MACHINE_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get(PATH_FINITE_STATE_MACHINE_TYPE);
        public static readonly UAObjectType FOLDER_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get(PATH_FOLDER_TYPE);
        public static readonly UAObjectType STATE_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get(PATH_STATE_TYPE);
        public static readonly UAObjectType TRANSITION_TYPE = (UAObjectType)OptixHelpers.UaObjectTypesFolder().Get(PATH_TRANSITION_TYPE);

        #endregion Public Fields

        public static Guid CreateGuidFromText(string text)
        {
            var hashBytes = Encoding.UTF8.GetBytes(text);
            var hasher = MD5.Create();
            var hashValue = hasher.ComputeHash(hashBytes);
            var guid = new Guid(hashValue);

            return guid;
        }

        public static Guid CreateGuidFromModelElement(string parentGUID, string itemName)
        {
            return CreateGuidFromText($"{parentGUID}.{itemName}");
        }
    }
}
