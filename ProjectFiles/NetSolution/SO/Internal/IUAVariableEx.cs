using Cca.Extensions.Common;
using FTOptix.HMIProject;
using System;
using UAManagedCore;

namespace NetZero.Internal
{
    public static class IUAVariableEx
    {
        public static IUAVariable Clone(this IUAVariable variable)
        {
            if (variable is not { })
            {
                return null;
            }

            var variableDataType = variable.GetOpcUaDataType();

            Log.Info(nameof(IUAVariableEx), $"{nameof(Clone)}: cloning {Log.Node(variable)}");

            return InformationModel.MakeVariable(variable.BrowseName, variableDataType, variable.Prototype.NodeId, variable.ArrayDimensions);

            //var clone = variableDataType switch
            //{
            //    _ when variableDataType == UAManagedCore.OpcUa.DataTypes.Int16 => OptixHelpers.CreateShortVariable(variable.BrowseName),
            //    _ when variableDataType == UAManagedCore.OpcUa.DataTypes.Int32 => OptixHelpers.CreateIntVariable(variable.BrowseName),
            //    _ when variableDataType == UAManagedCore.OpcUa.DataTypes.Int64 => OptixHelpers.CreateLongVariable(variable.BrowseName),
            //    _ when variableDataType == UAManagedCore.OpcUa.DataTypes.Boolean => OptixHelpers.CreateBooleanVariable(variable.BrowseName),
            //    _ when variableDataType == UAManagedCore.OpcUa.DataTypes.Float => OptixHelpers.CreateFloatVariable(variable.BrowseName),
            //    _ when variableDataType == UAManagedCore.OpcUa.DataTypes.Double => OptixHelpers.CreateDoubleVariable(variable.BrowseName),
            //    _ when variableDataType == UAManagedCore.OpcUa.DataTypes.DateTime => OptixHelpers.CreateDateTimeVariable(variable.BrowseName),
            //    _ => OptixHelpers.CreateStringVariable(variable.BrowseName),
            //};

            //return clone;
        }

        public static Type GetNetDataTypeFromType(this int type)
        {
            return type switch
            {
                0 => null,
                1 => typeof(double),
                2 => typeof(int),
                3 => typeof(string),
                4 => typeof(double),
                5 => typeof(int),
                _ => typeof(string)
            };
        }

        //public static Type GetNetTypeFromNodeId(this NodeId nodeId)
        //{
        //    return nodeId switch
        //    {
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.Int32 => typeof(int),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.Float => typeof(float),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.Double => typeof(double),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.Boolean => typeof(bool),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.String => typeof(string),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.Int16 => typeof(short),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.Int64 => typeof(long),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.DateTime => typeof(DateTime),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.Byte => typeof(byte),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.Guid=> typeof(Guid),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.LocalizedText=> typeof(LocalizedText),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.NodeId => typeof(NodeId),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.NodeClass => typeof(NodeClass),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.QualifiedName => typeof(QualifiedName),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.UInt32 => typeof(uint),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.UInt16 => typeof(ushort),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.UInt64 => typeof(ulong),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.Integer => typeof(int),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.Number => typeof(double),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.UInteger => typeof(uint),
        //        _ when nodeId == UAManagedCore.OpcUa.DataTypes.UtcTime => typeof(DateTime),
        //        _ => typeof(string)

        //    };
        //}

        public static NodeId GetOpcUaDataTypeFromNetType(this Type type)
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

        public static NodeId GetOpcUaDataTypeFromType(this int type)
        {
            return type switch
            {
                0 => null,
                1 => UAManagedCore.OpcUa.DataTypes.Double,
                2 => UAManagedCore.OpcUa.DataTypes.Int32,
                3 => UAManagedCore.OpcUa.DataTypes.String,
                4 => UAManagedCore.OpcUa.DataTypes.Int32,
                5 => UAManagedCore.OpcUa.DataTypes.Double,
                _ => UAManagedCore.OpcUa.DataTypes.String
            };
        }

        public static Type GetSystemType(this IUAVariable variable)
        {
            return variable.Value.Value.GetType();
        }

        public static bool IsHasCause(this IUAVariable attribute)
        {
            return attribute.IsNamed(Common.OPCUA_HAS_CAUSE);
        }

        public static bool IsMdsmComponentNumber(this IUAVariable attribute)
        {
            return attribute.IsNamed(Common.MDSM_COMPONENT_NUMBER);
        }

        public static bool IsNamed(this IUAVariable attribute, string name)
        {
            return attribute.BrowseName.Equals(name);
        }

        public static bool IsOpcUaMethodType(this IUAVariable attribute)
        {
            return attribute.IsNamed(Common.OPCUA_METHOD_TYPE);
        }

        public static bool IsOpcUaType(this IUAVariable attribute)
        {
            return attribute.IsNamed(Common.OPCUA_TYPE);
        }

        public static bool IsProperty(this IUAVariable attribute)
        {
            return !(attribute.IsOpcUaMethodType() || attribute.IsOpcUaType());
        }

        public static bool IsToState(this IUAVariable attribute)
        {
            return attribute.IsNamed(Common.OPCUA_TO_STATE);
        }

        public static bool ToBool(this IUAVariable attribute)
        {
            return attribute.Value.Value.ToBool();
        }

        public static decimal ToDecimal(this IUAVariable attribute)
        {
            return attribute.Value.Value.ToDecimal();
        }

        public static double ToDouble(this IUAVariable attribute)
        {
            return attribute.Value.Value.ToDouble();
        }

        public static float ToFloat(this IUAVariable attribute)
        {
            return attribute.Value.Value.ToSingle();
        }

        public static int ToInt(this IUAVariable attribute)
        {
            return attribute.Value.Value.ToInt();
        }

        public static string ToString(this IUAVariable attribute)
        {
            return attribute.Value.Value.ToString();
        }
    }
}
