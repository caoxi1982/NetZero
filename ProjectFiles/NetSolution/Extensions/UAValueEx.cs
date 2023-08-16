using System;

using UAManagedCore;

namespace NetZero.Extensions
{
    public static class UAValueEx
    {
        public static DataValue ToUAValue(this DateTime now, NodeId nodeId)
        {
            var boolVal = now.Millisecond < 500;
            return nodeId.Id switch
            {
                1 => new DataValue(boolVal, 1, now),
                6 => new DataValue(Convert.ToInt32(now.Millisecond), 1, now),
                29 => new DataValue(Convert.ToInt32(now.Millisecond), 1, now),
                7 => new DataValue(Convert.ToUInt32(now.Millisecond), 1, now),
                10 => new DataValue(Convert.ToDouble(now.Millisecond), 1, now),
                _ => new DataValue(now.Millisecond.ToString(), 1, now)
            };
        }
    }
}
