using Cca.Cgp.Core.Base.Ia;
using Cca.Extensions.Common;
using Cca.Extensions.Common.DateTimeUtil;
using System;
using UAManagedCore;

namespace NetZero.Extensions
{
    public static class UAVariableEx
    {
        public static Vqt GetVqt(this IUAVariable variable, string simpleType)
        {
            //Log.Info($"GetVqt: {variable.BrowseName} : {simpleType}");
            try
            {
                var vqt = new Vqt();
                var value = variable.DataValue.Value.Value;
                if (value is { })
                {
                    vqt.v = simpleType switch
                    {
                        "bool" => Convert.ToBoolean(value),
                        "integer" => Convert.ToInt32(value),
                        "long" => Convert.ToInt64(value),
                        "float" => Convert.ToSingle(value),
                        "double" => Convert.ToDouble(value),
                        _ => variable.DataValue.Value.Value.ToString()
                    };
                    vqt.q = variable.Quality.ToOpcQuality();
                    vqt.t = variable.DataValue.SourceTimestamp.ToRFC3339();
                }
                else
                {
                    var timestamp = DateTime.UtcNow;
                    vqt.v = simpleType switch
                    {
                        "bool" => true,
                        "integer" => timestamp.Millisecond,
                        "long" => timestamp.Millisecond,
                        "float" => (float)timestamp.Millisecond,
                        "double" => (double)timestamp.Millisecond,
                        _ => timestamp.Millisecond.ToString()
                    };
                    vqt.q = Cca.Cgp.Core.Base.Ia.Quality.Uncertain.ToInt();
                    vqt.t = timestamp.ToRFC3339();
                }
                return vqt;
            }
            catch (Exception ex)
            {
                // just return null
                Log.Error($"GetVqt: {variable.BrowseName}:\n{ex.Message}");
            }
            return null;
        }
    }
}
