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
#endregion

public class HistogramLogic1 : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
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
            if (ResultSet.GetLength(0) > 1) {
                Log.Error("Histogram", "Input query returned more than one line");
                return;
            }
            // Delete all children from Object
            foreach (var children in myModelObject.Children) {
                children.Delete();
            }
            // For each column create an Object children
            for (int i = 0; i < Header.Length; i++) {
                String columnName = Header[i];
                if (columnName != "Timestamp" && columnName != "LocalTimestamp" && columnName != "Id") {
                    var myObj = InformationModel.MakeVariable(columnName, OpcUa.DataTypes.Double);
                    myObj.Value = Convert.ToDouble(ResultSet[0, i]);
                    myModelObject.Add(myObj);
                }
            }
            myChart.Refresh();
        } catch (Exception ex) {
            Log.Error("Histogram", ex.Message);
            return;
        }
    }
}
