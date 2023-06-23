using System.Collections.Generic;

namespace NetZero.Extensions
{
    public static class ControllerCoaEx
    {
        #region Private Methods

        private static void FindAllAttributes(this CoaNode node, string attribute, ref List<string> values)
        {
            //Log.Info($"FindAllAttributes: testing {node.Name} for {attribute}");
            if (node.HasAttributes() && node.HasAttribute(attribute))
            {
                //Log.Info($"FindAllAttributes: {node.Name} has {attribute}");
                var value = node.GetAttributeValue(attribute);
                if (value is { })
                {
                    var attributeValue = value.ToString();
                    if (!string.IsNullOrEmpty(attributeValue) && !values.Contains(attributeValue))
                    {
                        //Log.Info($"FindAllAttributes: trying to add {attribute} : {attributeValue}");
                        values.Add(attributeValue);
                        //Log.Info($"FindAllAttributes: added {attribute} : {attributeValue}");
                    }
                }
            }

            if (node.Children is { })
            {
                foreach (var child in node.Children)
                {
                    child.FindAllAttributes(attribute, ref values);
                }
            }
        }

        #endregion Private Methods
    }
}