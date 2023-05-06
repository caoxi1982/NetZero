#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.Retentivity;
using FTOptix.NativeUI;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.Modbus;
#endregion

public class HistogramLogic : BaseNetLogic
{
    public override void Start()
    {
        Log.Info("His start");
        DrawChart();
    }

    public override void Stop()
    {
        Log.Info("His stop");
    }

    [ExportMethod]
    public void DrawChart() {
        // Get the different pieces we need to build the graph
        string sqlQuery = Owner.GetVariable("MyQuery").Value;
        Store myDbStore = InformationModel.Get<Store>(Owner.GetVariable("MyDataBase").Value);
        IUANode myModelObject = Owner.Get("HistogramModel");
        HistogramChart myChart = (HistogramChart)Owner;
        // Prepare SQL Query
        Object[,] ResultSet;
        String[] Header;
        // Execute query and check result
        try {
            myDbStore.Query(sqlQuery, out Header, out ResultSet);
            if (ResultSet.GetLength(0) < 1) {
                Log.Error("Histogram", "Input query returned more than one line");
                return;
            }
            // Delete all children from Object
            foreach (var children in myModelObject.Children) {
                children.Delete();
            }
            // For each column create an Object children
            for (int i = 0; i < ResultSet.GetLength(0); i++) {
                var myObj = InformationModel.MakeVariable(Convert.ToString(ResultSet[i,0]), OpcUa.DataTypes.String);
                myObj.Value = Convert.ToDouble(ResultSet[i, 1]);
                myModelObject.Add(myObj);
            }
            myChart.Refresh();
        } catch (Exception ex) {
            Log.Error("Histogram", ex.Message);
            return;
        }
    }
}
