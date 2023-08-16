using FTOptix.HMIProject;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using UAManagedCore;

namespace NetZero.Internal
{
    internal static class ControllerReader
    {
        internal static void PopulateModel(COAModel model, IUANode targetNode, string dataSource)
        {
            try
            {
                IUANode modelRoot;
                if (targetNode.HasChildNamed(model.name))
                {
                    modelRoot = targetNode.Children.First(c => c.BrowseName.Equals(model.name));
                }
                else
                {
                    modelRoot = model.CreateSoNode(targetNode, dataSource);
                }
                if (modelRoot is { })
                {
                    foreach (var childNode in model.items)
                    {
                        PopulateModelRecursive(childNode, modelRoot, dataSource);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(nameof(ControllerReader), $"PopulateModel: {model.name}: {ex.Message}");
            }
        }

        internal static void PopulateModelRecursive(COAModel model, IUANode parent, string dataSource)
        {
            var thisNode = model.CreateSoNode(parent, dataSource);

            if (model.items is { } && model.items.Any())
            {
                foreach (var childNode in model.items)
                {
                    if (childNode.mimeType == "x-ra/clx/models" || childNode.mimeType == "x-ra/clx/raC_UDT_SO_Node")
                    {
                        try
                        {
                            PopulateModelRecursive(childNode, thisNode, dataSource);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(nameof(ControllerReader), $"PopulateModelRecursive: NODE: {childNode.name}: {ex.Message}");
                        }
                    }
                    else
                    {
                        try
                        {
                            childNode.MakeSmartObjectTag(thisNode, dataSource);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(nameof(ControllerReader), $"PopulateModelRecursive: TAG: {childNode.name}: {ex.Message}");
                        }
                    }
                }
            }
        }

        internal static void PopulateModelsList(string json, IUANode owner)
        {
            var models = owner.GetVariable("Models");
            var modelsCount = 0;
            var controllerCoa = JsonSerializer.Deserialize<ControllerCOA>(json);
            if (controllerCoa is { })
            {
                foreach (var model in controllerCoa.items)
                {
                    var fullName = $"{Common.ModelNamePrefix}{model.name}";

                    if (!owner.HasChildNamed(fullName))
                    {
                        var modelPointer = InformationModel.MakeNodePointer(fullName);
                        owner.Add(modelPointer);

                        modelsCount++;
                    }
                }

                var vars = owner.Children.ToArray();
                var len = vars.Length - 1;

                for (int i = len; i > -1; i--)
                {
                    var tagetName = vars[i].BrowseName;
                    if (tagetName.StartsWith(Common.ModelNamePrefix))
                    {
                        tagetName = vars[i].BrowseName.Substring(10);
                        var inModels = controllerCoa.items.FirstOrDefault(checkModel => checkModel.name == tagetName);

                        if (inModels.name == null)
                        {
                            owner.Children.Remove(vars[i]);
                        }
                    }
                }

                models.Value = new UAValue(modelsCount);
            }
            else
            {
                Log.Info(nameof(ControllerReader), "PopulateModelsList - Deserialize failed");
            }
        }

        internal static void PopulateModelsOptix(IUANode owner, string workingFolder)
        {
            var modelfilePath = Path.Join(workingFolder, $"{owner.BrowseName}1");

            if (File.Exists(modelfilePath))
            {
                var modelRead = new StreamReader(modelfilePath);
                var json = modelRead.ReadToEnd();

                modelRead.Dispose();
                json = json[16..^2];

                var controllerCoa = JsonSerializer.Deserialize<ControllerCOA>(json);

                foreach (var model in controllerCoa.items)
                {
                    var fullName = $"{Common.ModelNamePrefix}{model.name}";
                    var modelPointer = owner.GetVariable(fullName);

                    if (modelPointer != null)
                    {
                        var targetNodePointer = modelPointer.Value.Value;

                        if (targetNodePointer != null)
                        {
                            var targetNode = InformationModel.Get((NodeId)targetNodePointer);

                            PopulateModel(model, targetNode, owner.BrowseName);
                        }
                    }
                }
            }
            else
            {
                Log.Error("Smart Objects", $"PopulateModelsOptix: Could not find models files {modelfilePath}");
            }
        }
    }
}
