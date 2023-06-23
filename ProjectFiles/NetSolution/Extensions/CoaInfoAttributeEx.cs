namespace NetZero.Extensions
{
    public static class CoaInfoAttributeEx
    {
        public const string OPCUA_TYPE = "OPCUA Type";
        public const string OPCUA_METHOD_TYPE = "OPCUA Method Type";
        public const string MDSM_COMPONENT_NUMBER = "MDSM Component Number";
        public const string OPCUA_TO_STATE = "OPCUA_Ref_ToState";
        public const string OPCUA_HAS_CAUSE = "OPCUA_Ref_HasCause";

        public static bool IsMdsmComponentNumber(this CoaInfoAttribute attribute)
        {
            return attribute.IsNamed(MDSM_COMPONENT_NUMBER);
        }

        public static bool IsHasCause(this CoaInfoAttribute attribute)
        {
            return attribute.IsNamed(OPCUA_HAS_CAUSE);
        }

        public static bool IsToState(this CoaInfoAttribute attribute)
        {
            return attribute.IsNamed(OPCUA_TO_STATE);
        }

        public static bool IsOpcUaType(this CoaInfoAttribute attribute)
        {
            return attribute.IsNamed(OPCUA_TYPE);
        }

        public static bool IsOpcUaMethodType(this CoaInfoAttribute attribute)
        {
            return attribute.IsNamed(OPCUA_METHOD_TYPE);
        }

        public static bool IsProperty(this CoaInfoAttribute attribute)
        {
            return !(attribute.IsOpcUaMethodType() || attribute.IsOpcUaType());
        }

        public static bool IsNamed(this CoaInfoAttribute attribute, string name)
        {
            return attribute.Name.Equals(name);
        }
    }
}