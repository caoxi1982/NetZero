#region Using directives

using Cca.Extensions.Common;
using FTOptix.NetLogic;
using NetZero.Internal;
using System;
using System.Diagnostics;
using System.IO;
using UAManagedCore;
using FTOptix.NativeUI;
using FTOptix.UI;
using FTOptix.OPCUAServer;
using NetZero.Internal;
using FTOptix.WebUI;
using FTOptix.S7TCP;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;

#endregion

public class SO_Offline : BaseNetLogic
{
    [ExportMethod]
    public void ReadModels()
    {
        // read in the COA JSON
        // for each model found, add a NodePointer variable under the
        // Owner.Models property with the name of the model.
        // if any are found, set the Owner.Models property to true;
        var path = Owner.GetVariable("ControllerFilePath").Value.Value.ToString().ToLower();

        if (path.EndsWith(".acd") || path.EndsWith(".l5x"))
        {
            try
            {
                var extractor = new Process();
                var coaJson = "";
                var workingFolder = Owner.Owner.GetVariable("UtilityFolder").Value.Value.ToString().ToLower();
                var file1 = Path.Join(workingFolder, "1");
                var file2 = Path.Join(workingFolder, "2");
                var out1 = Path.Join(workingFolder, $"{Owner.BrowseName}1");
                var out2 = Path.Join(workingFolder, $"{Owner.BrowseName}2");

                if (File.Exists(file1)) { File.Delete(file1); }
                if (File.Exists(file2)) { File.Delete(file2); }

                path = path.Substring(path.IndexOf(":///") + 4);

                extractor.StartInfo.CreateNoWindow = true;
                extractor.StartInfo.UseShellExecute = true;
                extractor.StartInfo.Arguments = $"-f \"{path}\" {workingFolder}";
                extractor.StartInfo.FileName = Path.Join(workingFolder, "extractor.exe");

                extractor.Start();

                extractor.WaitForExit();

                if (File.Exists(file1))
                {
                    var modelRead = new StreamReader(file1);
                    coaJson = modelRead.ReadToEnd();
                    modelRead.Dispose();

                    coaJson = coaJson[16..^2];

                    File.Copy(file1, out1, true);
                    File.Delete(file1);

                    File.Copy(file2, out2, true);
                    File.Delete(file2);

                    if (coaJson.IsValidJson())
                    {
                        ControllerReader.PopulateModelsList(coaJson, Owner);
                        Log.Info($"Smart Objects", $"Models read. From {Owner.BrowseName}");
                    }
                    else
                    {
                        Log.Error("Smart Objects", $"Failed to process file. From {Owner.BrowseName}");
                    }
                }
                else
                {
                    Log.Error("Smart Objects", $"Failed to process file. From {Owner.BrowseName}");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Smart Objects", $"Failed to process file. From {Owner.BrowseName} {ex.Message}");
            }
        }
        else
        {
            Log.Error("Smart Objects", $"Controller file must be a ACD or L5X. From {Owner.BrowseName}");
        }
    }

    [ExportMethod]
    public void ImportSmartObjects()
    {
        try
        {
            var workingFolder = Owner.Owner.GetVariable("UtilityFolder").Value.Value.ToString().ToLower();

            ControllerReader.PopulateModelsOptix(Owner, workingFolder);
        }
        catch (Exception ex)
        {
            Log.Error("Smart Objects", $"ImportSmartObjects {Owner.BrowseName} {ex.Message}");
        }
    }
}
