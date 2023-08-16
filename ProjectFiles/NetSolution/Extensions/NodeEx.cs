using Cca.Cgp.Common.Model;
using Cca.Extensions.Common;
using NetZero.EA;

using UAManagedCore;

namespace NetZero.Extensions
{
    public static class NodeEx
    {
        #region Public Methods

        public static string GetSimpleDataType(this NodeId nodeId)
        {
            return nodeId.Id.ToInt() switch
            {
                1 => "bool",
                6 => "integer",
                29 => "integer",
                7 => "integer",
                10 => "double",
                _ => "string"
            };
        }

        public static Cca.Cgp.Core.Base.Ia.Vqt GetCurrentVqt(this Node node)
        {
            try
            {
                //var basePath = FTOptixConcerto.Instance.InformationModel.GetVariable(node.Id);
                var browsePath = node.FullyQualifiedName.Replace(".", "/");
                //Log.Info($"GetCurrentVqt: Finding {browsePath} : {node.Id}");
                //var variable = Project.Current.Get(browsePath) as IUAVariable; // FTOptixConcerto.Instance.InformationModel.GetVariable(browsePath);
                if (FTOptixConcerto.Instance.TryGetNodeId(node.FullyQualifiedName, out NodeId nodeId))
                {
                    if (FTOptixConcerto.Instance.NetLogicObject is { })
                    {
                        var variable = FTOptixConcerto.Instance.GetVariableFromNodeId(nodeId);
                        if (variable is { })
                        {
                            //Log.Info($"GetCurrentVqt: Found {browsePath}");
                            return variable.GetVqt(node.Type.SimpleTypeFromMimeType());
                        }
                        else
                        {
                            Log.Info($"GetCurrentVqt: {browsePath} not found");
                        }
                    }
                    else
                    {
                        Log.Info($"GetCurrentVqt: NetLogicObject is null");
                    }
                }
                Log.Info($"GetCurrentVqt: {browsePath} not found");
            }
            catch (System.Exception ex)
            {
                Log.Error($"GetCurrentVqt:\n{ex.Message}");
            }
            return null;
        }

        #endregion Public Methods
    }
}
