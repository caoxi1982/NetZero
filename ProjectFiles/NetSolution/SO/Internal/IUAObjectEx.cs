using System.Collections.Generic;

using UAManagedCore;

namespace NetZero.Internal
{
    public static class IUAObjectEx
    {
        public static void AddOrSet(this IUAObject node, IUANode child)
        {
            if (!node.HasChildNamed(child.BrowseName))
            {
                node.Add(child);
                return;
            }
            if (child is UAObject childObject)
            {
                node.SetObjectProperty(childObject, child.BrowseName);
                return;
            }
            if (child is IUAVariable childVariable)
            {
                node.AddVariables(new List<IUAVariable> { childVariable });
                return;
            }
        }
    }
}
