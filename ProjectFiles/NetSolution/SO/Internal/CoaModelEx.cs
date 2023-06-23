using Cca.Extensions.Common;
using FTOptix.HMIProject;
using System;
using System.Linq;
using UAManagedCore;

namespace NetZero.Internal
{
    internal static class CoaModelEx
    {
        public static IUAVariable CreateSoTagArrayVariable(string name, NodeId dataType, int length, bool writeable = false)
        {
            var dimensions = new uint[1];
            dimensions[0] = (uint)length;
            if (writeable)
            {
                return InformationModel.MakeVariable(name, dataType, VariableTypes.SmartObjectWritableTag, dimensions);
            }
            else
            {
                return InformationModel.MakeVariable(name, dataType, VariableTypes.SmartObjectTag, dimensions);
            }
        }

        public static IUAVariable CreateSoTagVariable(IUANode parent, string name, NodeId dataType, bool writeable = false, int length = 0)
        {
            IUAVariable variable = null;
            NodeId variableType = writeable ? VariableTypes.SmartObjectWritableTag : VariableTypes.SmartObjectTag;

            try
            {
                variable = OptixHelpers.CreateScalarVariable(parent, name, dataType, variableType, "", length);
            }
            catch (Exception ex)
            {
                Log.Error(nameof(CoaModelEx), $"CreateSoTagVariable: {name}, {ex.Message}");
            }

            return variable;
        }

        public static int GetArrayTagLength(this COAModel coaNode, string tagName)
        {
            return coaNode.items.Count(c => c.name.StartsWith(tagName));
        }

        public static void MakeSmartObjectTag(this COAModel tagNode, IUANode parent, string dataSource, int length = 0)
        {
            if (tagNode.mimeType == "x-ra/clx/raC_UDT_SO_Node") { return; }

            var opcuaTagType = tagNode.GetOpcUaDataType();
            IUAVariable tagVariable = CreateSoTagVariable(parent, tagNode.name, opcuaTagType, tagNode.IsWriteable(), length);
            if (tagVariable is { })
            {
                var defaultValues = GetDefaultValue(tagNode.mimeType, length);
                tagVariable.Value = defaultValues;
                var backingTag = OptixHelpers.CreateStringVariable(tagVariable, "Symbol name");
                backingTag.Value = new UAValue($"{dataSource}/{tagNode.backingTag}");
                PopulateInfoAttributes(tagNode, tagVariable);
            }
            else
            {
                Log.Error(nameof(CoaModelEx), $"MakeSmartObjectTag: {tagNode.name}: tag variable is null");
            }
        }

        internal static IUAObject CreateSoNode(this COAModel coaModel, IUANode parent, string dataSource)
        {
            var soNode = OptixHelpers.CreateObject(parent, coaModel.name, ObjectTypes.SmartObjectNode);

            if (soNode is { })
            {
                var backingTag = OptixHelpers.CreateStringVariable(soNode, "Symbol name");
                backingTag.Value = new UAValue($"{dataSource}/{coaModel.backingTag}");
                PopulateInfoAttributes(coaModel, soNode);
            }
            else
            {
                Log.Warning(nameof(CoaModelEx), $"CreateSoNode: soNode is null");
            }
            return soNode;
        }

        internal static void PopulateInfoAttributes(COAModel coaModel, IUANode soNode)
        {
            var infoAttributes = OptixHelpers.CreateBooleanVariable(soNode, "Info Attributes");
            UAValue hasInfoAttributes = new(coaModel.infoAttributes is { } && coaModel.infoAttributes.Any(ia => (!string.IsNullOrEmpty(ia.name.Trim()))));
            infoAttributes.Value = hasInfoAttributes;

            if (hasInfoAttributes.Value.ToBool())
            {
                foreach (var coaAttribute in coaModel.infoAttributes)
                {
                    // make sure we have a valid name
                    if (!string.IsNullOrEmpty(coaAttribute.name))
                    {
                        var varName = coaAttribute.name.Trim();
                        var infoAttVar = OptixHelpers.CreateStringVariable(soNode, varName);
                        if (!string.IsNullOrEmpty(coaAttribute.value))
                        {
                            infoAttVar.Value = new UAValue(coaAttribute.value);
                        }
                        else
                        {
                            infoAttVar.Value = new UAValue(string.Empty);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Given a SmartObject node type, return the default array value
        /// </summary>
        /// <param name="tagTypeCode"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static UAValue GetArrayDefaultsFromType(string tagTypeCode, int length)
        {
            return tagTypeCode switch
            {
                "x-ra/clx/raC_Opr_SO_Real" => new UAValue(new float[length]),
                "x-ra/clx/raC_Opr_SO_RealW" => new UAValue(new float[length]),
                "x-ra/clx/raC_Opr_SO_DINT" => new UAValue(new int[length]),
                "x-ra/clx/raC_Opr_SO_DINTW" => new UAValue(new int[length]),
                "x-ra/clx/raC_Opr_SO_LINT" => new UAValue(new long[length]),
                "x-ra/clx/raC_Opr_SO_LINTW" => new UAValue(new long[length]),
                _ => new UAValue(new string[length])
            };
        }

        private static UAValue GetDefaultValue(string tagTypeCode, int length = 0)
        {
            if (length > 0)
            {
                return GetArrayDefaultsFromType(tagTypeCode, length);
            }
            else
            {
                return GetScalarDefaultsFromType(tagTypeCode);
            }
        }

        private static NodeId GetOpcUaDataType(this COAModel node)
        {
            return node.mimeType switch
            {
                "x-ra/clx/raC_Opr_SO_Node" => ObjectTypes.SmartObjectNode,
                "x-ra/clx/raC_Opr_SO_Real" => UAManagedCore.OpcUa.DataTypes.Float,
                "x-ra/clx/raC_Opr_SO_RealW" => UAManagedCore.OpcUa.DataTypes.Float,
                "x-ra/clx/raC_Opr_SO_DINT" => UAManagedCore.OpcUa.DataTypes.Int32,
                "x-ra/clx/raC_Opr_SO_DINTW" => UAManagedCore.OpcUa.DataTypes.Int32,
                "x-ra/clx/raC_Opr_SO_LINT" => UAManagedCore.OpcUa.DataTypes.Int64,
                "x-ra/clx/raC_Opr_SO_LINTW" => UAManagedCore.OpcUa.DataTypes.Int64,
                _ => UAManagedCore.OpcUa.DataTypes.String
            };
        }

        /// <summary>
        /// Given a SmartObject node type, return the default scalar value
        /// </summary>
        /// <param name="tagTypeCode"></param>
        /// <returns></returns>
        private static UAValue GetScalarDefaultsFromType(string tagTypeCode)
        {
            return tagTypeCode switch
            {
                "x-ra/clx/raC_Opr_SO_Node" => null,
                "x-ra/clx/raC_Opr_SO_Real" => new UAValue(0.0D),
                "x-ra/clx/raC_Opr_SO_RealW" => new UAValue(0.0D),
                "x-ra/clx/raC_Opr_SO_DINT" => new UAValue(0),
                "x-ra/clx/raC_Opr_SO_DINTW" => new UAValue(0),
                "x-ra/clx/raC_Opr_SO_LINT" => new UAValue(0),
                "x-ra/clx/raC_Opr_SO_LINTW" => new UAValue(0),
                _ => new UAValue(string.Empty)
            };
        }

        private static bool IsWriteable(this COAModel tagNode)
        {
            return tagNode.mimeType.EndsWith('W');
        }
    }
}